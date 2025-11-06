using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable, ITemperatureAffectable
{
    public enum PatrolMode { Waypoints, RangeX }

    public static event Action<EnemyController> OnAnyEnemyDied;

    [Header("Mission")]
    [SerializeField] private string missionTag = "";
    public string MissionTag => missionTag;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    [Header("Patrol")]
    [SerializeField] private PatrolMode patrolMode = PatrolMode.RangeX;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitAtPoint = 0.4f;

    [Header("Range Patrol (X only)")]
    [SerializeField] private bool rangeIsOffsetFromStart = true;
    [SerializeField] private float rangeLeft = -2f;
    [SerializeField] private float rangeRight = 2f;
    [SerializeField] private bool startMovingRight = true;
    [SerializeField] private float boundLeeway = 0.02f;

    [Header("Chase")]
    [SerializeField] private bool chaseOnSight = true;
    [SerializeField] private float chaseSpeed = 3.2f;
    [SerializeField] private float stopChaseDistance = 0.25f;
    private Coroutine alarmSeekRoutine;

    [Header("Vision")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private Collider2D visionTrigger;

    [Header("Vision Follow Facing")]
    [SerializeField] private bool flipVisionWithFacing = true;
    [SerializeField] private float visionSideOffset = 1.0f;
    [SerializeField] private bool visionIsChildTransform = true;

    [Header("Contact Damage")]
    [SerializeField] private bool dealContactDamage = true;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float perTargetCooldown = 0.5f;
    [SerializeField] private bool knockbackOnHit = true;
    [SerializeField] private float knockbackForce = 5f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Pathing Options")]
    [SerializeField] private bool lockYToStart = true;

    [Header("UI Hook")]
    [SerializeField] private EnemyEmotionUI emotionUI;

    // --- Color State ---
    private Color originalColor = Color.white;
    private bool colorCached = false;

    private int currentHealth;
    private bool isDead;

    private float lockedY;
    private Rigidbody2D rb;
    private Transform player;
    private bool chasing;
    private Coroutine patrolRoutine;

    private readonly Dictionary<int, float> lastHitTimeByTarget = new();

    private Vector3 cachedVisionLocalPos;
    private Vector2 cachedColliderOffset;
    private int facingSign = 1;

    private float startX;
    private float leftBound;
    private float rightBound;
    private int rangeDir;

    private int smokeStack = 0;
    private int coldStack = 0;

    public bool IsInSmoke => smokeStack > 0;
    public bool IsInCold => coldStack > 0;

    public void EnterSmoke() { smokeStack++; OnPlayerLost(); }
    public void ExitSmoke() { smokeStack = Mathf.Max(0, smokeStack - 1); }

    public void EnterCold()
    {
        coldStack++;
        SetSpeedModifier("cold", 0.5f);
        OnPlayerLost();

        if (!colorCached && spriteRenderer)
        {
            originalColor = spriteRenderer.color;
            colorCached = true;
        }

        if (spriteRenderer)
            spriteRenderer.color = Color.cyan;
    }

    public void ExitCold()
    {
        coldStack = Mathf.Max(0, coldStack - 1);
        if (coldStack == 0)
        {
            RemoveSpeedModifier("cold");

            if (spriteRenderer)
                spriteRenderer.color = originalColor;
        }
    }

    // ---------- Temperature ----------
    public float CurrentTemperature => 0f;

    public void ApplyHeat(float amt)
    {
        if (isDead) return;

        int damage = Mathf.CeilToInt(amt);
        TakeDamage(damage);

        if (spriteRenderer)
            spriteRenderer.color = new Color(1f, 0.6f, 0.3f);
    }

    public void ApplyCold(float amt)
    {
        if (isDead) return;
        EnterCold();
    }

    public void CoolDown(float amt)
    {
        ExitCold();
    }

    // ---------- Speed Modifiers ----------
    private readonly Dictionary<object, float> speedModifiers = new();
    private float SpeedMult
    {
        get { float m = 1f; foreach (var kv in speedModifiers) m *= Mathf.Clamp(kv.Value, 0.01f, 10f); return m; }
    }
    public void SetSpeedModifier(object key, float mult) { speedModifiers[key] = mult; }
    public void RemoveSpeedModifier(object key) { if (speedModifiers.ContainsKey(key)) speedModifiers.Remove(key); }

    private float PatrolSpeedEff => patrolSpeed * SpeedMult;
    private float ChaseSpeedEff => chaseSpeed * SpeedMult;

    private Collider2D[] myColliders;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        myColliders = GetComponentsInChildren<Collider2D>(true);

        if (!emotionUI) emotionUI = GetComponentInChildren<EnemyEmotionUI>(true);

        if (visionTrigger != null)
        {
            visionTrigger.isTrigger = true;
            if (visionIsChildTransform)
            {
                cachedVisionLocalPos = visionTrigger.transform.localPosition;
                cachedVisionLocalPos.x = Mathf.Abs(cachedVisionLocalPos.x) > 0.0001f ? Mathf.Abs(cachedVisionLocalPos.x) : Mathf.Max(0.0001f, visionSideOffset);
            }
            else
            {
                cachedColliderOffset = GetColliderOffset(visionTrigger);
                if (Mathf.Abs(cachedColliderOffset.x) < 0.0001f) cachedColliderOffset.x = Mathf.Max(0.0001f, visionSideOffset);
                else cachedColliderOffset.x = Mathf.Abs(cachedColliderOffset.x);
            }
        }
    }

    private void Start()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        lockedY = rb.position.y;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        startX = rb.position.x;

        float l = rangeLeft;
        float r = rangeRight;

        if (rangeIsOffsetFromStart)
        {
            leftBound = startX + Mathf.Min(l, r);
            rightBound = startX + Mathf.Max(l, r);
        }
        else
        {
            leftBound = Mathf.Min(l, r);
            rightBound = Mathf.Max(l, r);
        }

        if (Mathf.Abs(rightBound - leftBound) < 0.001f)
            rightBound = leftBound + 0.5f;

        rangeDir = startMovingRight ? 1 : -1;
    }

    private void OnEnable()
    {
        if (patrolRoutine == null)
            patrolRoutine = StartCoroutine(PatrolLoop());
    }

    private void OnDisable()
    {
        if (patrolRoutine != null)
        {
            StopCoroutine(patrolRoutine);
            patrolRoutine = null;
        }
    }

    // ---------- Patrol ----------
    private IEnumerator PatrolLoop()
    {
        if (patrolMode == PatrolMode.Waypoints)
        {
            if (waypoints == null || waypoints.Length == 0) yield break;
            int idx = 0;
            while (!isDead)
            {
                if (chasing) { yield return null; continue; }
                Vector2 target = waypoints[idx].position;
                if (lockYToStart) target.y = lockedY;
                yield return MoveTo(target, PatrolSpeedEff);
                yield return new WaitForSeconds(waitAtPoint);
                idx = (idx + 1) % waypoints.Length;
            }
        }
        else
        {
            while (!isDead)
            {
                if (chasing) { yield return null; continue; }
                Vector2 pos = rb.position;
                if (lockYToStart) pos.y = lockedY;
                float targetX = (rangeDir > 0) ? rightBound : leftBound;
                float stepX = Mathf.MoveTowards(pos.x, targetX, PatrolSpeedEff * Time.fixedDeltaTime);
                Vector2 next = new Vector2(stepX, lockYToStart ? lockedY : pos.y);
                rb.MovePosition(next);
                UpdateFacing(stepX - pos.x);
                UpdateAnim(Mathf.Abs(stepX - pos.x) > 0.0001f);
                if (Mathf.Abs(stepX - targetX) <= boundLeeway)
                {
                    rangeDir *= -1;
                    UpdateAnim(false);
                    yield return new WaitForSeconds(waitAtPoint);
                }
                yield return new WaitForFixedUpdate();
            }
        }
    }

    private IEnumerator MoveTo(Vector2 target, float speed)
    {
        while (!chasing && !isDead && Vector2.Distance(rb.position, target) > 0.01f)
        {
            Vector2 current = rb.position;
            if (lockYToStart) current.y = lockedY;
            Vector2 next = Vector2.MoveTowards(current, target, speed * Time.fixedDeltaTime);
            if (lockYToStart) next.y = lockedY;
            rb.MovePosition(next);
            UpdateFacing(next.x - current.x);
            UpdateAnim(true);
            yield return new WaitForFixedUpdate();
        }
        UpdateAnim(false);
    }

    // ---------- Vision ----------
    public void Vision_OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || IsInSmoke || IsInCold) return;
        if (!IsPlayer(other)) return;
        if (!useLineOfSight || HasLineOfSight(other.transform))
        {
            OnPlayerSpotted(other.transform);
            emotionUI?.ForceAlert();
        }
    }

    public void Vision_OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        if (IsInSmoke || IsInCold) { OnPlayerLost(); emotionUI?.ForceHidden(); return; }
        if (!IsPlayer(other)) return;

        if (!useLineOfSight || HasLineOfSight(other.transform))
        {
            emotionUI?.ForceAlert();
        }
        else
        {
            OnPlayerLost();
            emotionUI?.ForceHidden();
        }
    }

    public void Vision_OnTriggerExit2D(Collider2D other)
    {
        if (isDead) return;
        if (!IsPlayer(other)) return;
        OnPlayerLost();
        emotionUI?.ForceHidden();
    }

    private bool IsPlayer(Collider2D col) =>
        (playerLayer.value & (1 << col.gameObject.layer)) != 0;

    private bool HasLineOfSight(Transform target)
    {
        Vector2 origin = rb.position;
        Vector2 toTgt = (Vector2)target.position - origin;
        float dist = toTgt.magnitude;
        if (dist <= 0.001f) return true;
        var hit = Physics2D.Raycast(origin, toTgt.normalized, dist, obstacleLayers);
        return hit.collider == null;
    }

    private void OnPlayerSpotted(Transform playerTf)
    {
        if (!chaseOnSight || IsInSmoke || IsInCold || isDead) return;
        player = playerTf;
        if (!chasing)
        {
            chasing = true;
            StopPatrol();
            StartCoroutine(ChaseLoop());
        }
    }

    private void OnPlayerLost()
    {
        player = null;
        if (chasing && !isDead)
        {
            chasing = false;
            ResumePatrol();
        }
    }

    private IEnumerator ChaseLoop()
    {
        while (chasing && player != null && !IsInSmoke && !IsInCold && !isDead)
        {
            Vector2 pos = rb.position;
            Vector2 goal = (Vector2)player.position;
            if (lockYToStart) goal.y = lockedY;

            FaceTowards(goal);

            if (Vector2.Distance(pos, goal) <= stopChaseDistance)
            {
                UpdateAnim(false);
                emotionUI?.ForceAlert();
                yield return null;
                continue;
            }

            Vector2 next = Vector2.MoveTowards(pos, goal, ChaseSpeedEff * Time.fixedDeltaTime);
            if (lockYToStart) next.y = lockedY;
            rb.MovePosition(next);
            UpdateFacing(next.x - pos.x);
            UpdateAnim(true);
            emotionUI?.ForceAlert();
            yield return new WaitForFixedUpdate();
        }
        UpdateAnim(false);
    }

    // ---------- Damage ----------
    public void Damage_OnTriggerEnter2D(Collider2D other) => TryDamagePlayerOnContact(other);
    public void Damage_OnTriggerStay2D(Collider2D other) => TryDamagePlayerOnContact(other);

    private void TryDamagePlayerOnContact(Collider2D other)
    {
        if (isDead || !dealContactDamage) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (IsInSmoke || IsInCold || (PlayerHiding.Instance && PlayerHiding.Instance.IsHiddenBySmoke)) return;

        emotionUI?.PulseAlert(0.6f);

        FaceTowards(other.bounds.center);

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;

        int id = other.transform.root.GetInstanceID();
        float now = Time.time;
        if (lastHitTimeByTarget.TryGetValue(id, out float last) && now - last < perTargetCooldown) return;

        damageable.TakeDamage(contactDamage);
        lastHitTimeByTarget[id] = now;

        if (knockbackOnHit)
        {
            var rb2 = other.attachedRigidbody;
            if (rb2 != null && knockbackForce > 0f)
            {
                Vector2 dir = ((Vector2)other.bounds.center - (Vector2)transform.position).normalized;
                rb2.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    // ---------- Helpers ----------
    private void StopPatrol()
    {
        if (patrolRoutine != null)
        {
            StopCoroutine(patrolRoutine);
            patrolRoutine = null;
        }
    }

    public void ResumePatrol()
    {
        if (isDead) return;
        if (patrolRoutine == null)
            patrolRoutine = StartCoroutine(PatrolLoop());
    }

    private void UpdateFacing(float xDelta)
    {
        if (Mathf.Abs(xDelta) >= 0.01f) facingSign = (xDelta < 0f) ? -1 : 1;
        if (spriteRenderer) spriteRenderer.flipX = (facingSign < 0);
        if (flipVisionWithFacing) UpdateVisionSide();
    }

    private void FaceTowards(Vector2 worldPos)
    {
        float dx = worldPos.x - rb.position.x;
        if (Mathf.Abs(dx) >= 0.001f) UpdateFacing(dx);
    }

    private void UpdateAnim(bool isMoving)
    {
        if (!animator || isDead) return;
        animator.SetBool("IsWalking", isMoving);
        animator.SetBool("IsIdle", !isMoving);
    }

    private void UpdateVisionSide()
    {
        if (visionTrigger == null) return;
        if (visionIsChildTransform)
        {
            Vector3 lp = cachedVisionLocalPos;
            lp.x = Mathf.Abs(lp.x) > 0.0001f ? Mathf.Abs(lp.x) : Mathf.Max(0.0001f, visionSideOffset);
            lp.x *= facingSign;
            visionTrigger.transform.localPosition = lp;
        }
        else
        {
            Vector2 off = cachedColliderOffset;
            off.x = Mathf.Abs(off.x) > 0.0001f ? Mathf.Abs(off.x) : Mathf.Max(0.0001f, visionSideOffset);
            off.x *= facingSign;
            SetColliderOffset(visionTrigger, off);
        }
    }

    private static Vector2 GetColliderOffset(Collider2D col)
    {
        if (col is BoxCollider2D box) return box.offset;
        if (col is CircleCollider2D circle) return circle.offset;
        if (col is CapsuleCollider2D cap) return cap.offset;
        return Vector2.zero;
    }

    private static void SetColliderOffset(Collider2D col, Vector2 offset)
    {
        if (col is BoxCollider2D box) { box.offset = offset; return; }
        if (col is CircleCollider2D circle) { circle.offset = offset; return; }
        if (col is CapsuleCollider2D cap) { cap.offset = offset; return; }
    }

    // ---------- Death ----------
    public void TakeDamage(int amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, amount));
        if (currentHealth <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        OnAnyEnemyDied?.Invoke(this);

        if (!string.IsNullOrEmpty(missionTag))
        {
            MissionManager.Instance?.MarkKillComplete(missionTag);
        }

        StopAllCoroutines();
        chasing = false;

        if (animator)
        {
            animator.ResetTrigger("Die");
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsIdle", false);
            animator.SetTrigger("Die");
        }

        if (visionTrigger) visionTrigger.enabled = false;
        dealContactDamage = false;

        foreach (var c in myColliders)
            if (c) c.enabled = false;

        if (rb) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
    }

    // ---------- Move To Target ----------
    public void GoToTarget(Transform target, float stopDistance, System.Action<EnemyController> onReach)
    {
        if (isDead) return;
        if (alarmSeekRoutine != null)
            StopCoroutine(alarmSeekRoutine);

        alarmSeekRoutine = StartCoroutine(GoToTargetRoutine(target, stopDistance, onReach));
    }

    private IEnumerator GoToTargetRoutine(Transform target, float stopDistance, System.Action<EnemyController> onReach)
    {
        chasing = false;
        StopPatrol();

        while (!isDead && target != null)
        {
            Vector2 pos = rb.position;
            Vector2 goal = (Vector2)target.position;
            if (lockYToStart) goal.y = lockedY;

            if (Vector2.Distance(pos, goal) <= stopDistance)
                break;

            Vector2 next = Vector2.MoveTowards(pos, goal, ChaseSpeedEff * Time.fixedDeltaTime);
            if (lockYToStart) next.y = lockedY;
            rb.MovePosition(next);
            UpdateFacing(next.x - pos.x);
            UpdateAnim(true);
            yield return new WaitForFixedUpdate();
        }

        UpdateAnim(false);
        alarmSeekRoutine = null;
        onReach?.Invoke(this);

        ResumePatrol();
    }
}
