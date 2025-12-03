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

    [Header("Range Patrol")]
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

    [Header("Vision Settings")]
    [SerializeField] private float visionRadius = 3f;
    [SerializeField] private float visionAngle = 70f;
    [SerializeField] private float visionYOffset = 0.4f;
    [SerializeField] private bool debugVisionGizmo = true;

    [Header("LOS Settings")]
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField] private bool useLineOfSight = true;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Contact Damage (No Hitbox)")]
    [SerializeField] private bool dealContactDamage = true;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float perTargetCooldown = 0.5f;
    [SerializeField] private float knockbackForce = 5f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Pathing")]
    [SerializeField] private bool lockYToStart = true;

    [Header("UI")]
    [SerializeField] private EnemyEmotionUI emotionUI;

    private Color originalColor = Color.white;
    private bool colorCached = false;

    private int currentHealth;
    private bool isDead;

    private float lockedY;
    private Rigidbody2D rb;
    private Transform player;
    private bool chasing;
    private Coroutine patrolRoutine;

    private readonly Dictionary<int, float> lastHit = new();

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
        SetSpeed("cold", 0.5f);
        OnPlayerLost();
        if (!colorCached && spriteRenderer)
        {
            originalColor = spriteRenderer.color;
            colorCached = true;
        }
        if (spriteRenderer) spriteRenderer.color = Color.cyan;
    }

    public void ExitCold()
    {
        coldStack = Mathf.Max(0, coldStack - 1);
        if (coldStack == 0)
        {
            RemoveSpeed("cold");
            if (spriteRenderer) spriteRenderer.color = originalColor;
        }
    }

    public float CurrentTemperature => 0f;

    public void ApplyHeat(float amt)
    {
        if (isDead) return;
        TakeDamage(Mathf.CeilToInt(amt));
        if (spriteRenderer) spriteRenderer.color = new Color(1f, 0.6f, 0.3f);
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

    private readonly Dictionary<object, float> speedMods = new();
    private float SpeedMult { get { float m = 1f; foreach (var kv in speedMods) m *= kv.Value; return m; } }
    private float PatrolSpeedEff => patrolSpeed * SpeedMult;
    private float ChaseSpeedEff => chaseSpeed * SpeedMult;
    public void SetSpeed(object key, float m) => speedMods[key] = m;
    public void RemoveSpeed(object key) { if (speedMods.ContainsKey(key)) speedMods.Remove(key); }

    private Collider2D[] myCols;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        myCols = GetComponentsInChildren<Collider2D>(true);
        if (!emotionUI) emotionUI = GetComponentInChildren<EnemyEmotionUI>(true);
    }

    private void Start()
    {
        currentHealth = Mathf.Max(1, maxHealth);
        lockedY = rb.position.y;
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        startX = rb.position.x;

        if (rangeIsOffsetFromStart)
        {
            leftBound = startX + Mathf.Min(rangeLeft, rangeRight);
            rightBound = startX + Mathf.Max(rangeLeft, rangeRight);
        }
        else
        {
            leftBound = Mathf.Min(rangeLeft, rangeRight);
            rightBound = Mathf.Max(rangeLeft, rangeRight);
        }

        if (Mathf.Abs(rightBound - leftBound) < 0.1f)
            rightBound = leftBound + 0.5f;

        rangeDir = startMovingRight ? 1 : -1;

        patrolRoutine = StartCoroutine(PatrolLoop());
    }

    private void OnDisable()
    {
        if (patrolRoutine != null) StopCoroutine(patrolRoutine);
    }

    //GIZMO VISION

    private bool GizmoDetectPlayer()
    {
        if (!player) return false;
        if (isDead || IsInSmoke || IsInCold) return false;

        Vector2 origin = (Vector2)transform.position + new Vector2(visionYOffset * 0f, visionYOffset);
        Vector2 dir = (player.position - transform.position);
        float dist = dir.magnitude;

        if (dist > visionRadius) return false;

        // Angle
        float angle = Vector2.Angle(new Vector2(facingSign, 0f), dir.normalized);
        if (angle > visionAngle * 0.5f) return false;

        // LOS
        if (useLineOfSight)
        {
            var hit = Physics2D.Raycast(origin, dir.normalized, dist, obstacleLayers);
            if (hit.collider != null) return false;
        }

        return true;
    }

    private void HandleGizmoVision()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (!player) return;

        if (GizmoDetectPlayer())
        {
            OnPlayerSpotted(player);
            emotionUI?.ForceAlert();
        }
        else
        {
            OnPlayerLost();
            emotionUI?.ForceHidden();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugVisionGizmo) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);

        Vector3 origin = transform.position + new Vector3(0f, visionYOffset, 0f);
        Gizmos.DrawWireSphere(origin, visionRadius);

        Vector3 left = Quaternion.Euler(0, 0, visionAngle * 0.5f) * Vector3.right * facingSign;
        Vector3 right = Quaternion.Euler(0, 0, -visionAngle * 0.5f) * Vector3.right * facingSign;

        Gizmos.DrawLine(origin, origin + left * visionRadius);
        Gizmos.DrawLine(origin, origin + right * visionRadius);
    }

    //UPDATE

    private void Update()
    {
        HandleGizmoVision();
    }

    // PATROL

    private IEnumerator PatrolLoop()
    {
        if (patrolMode == PatrolMode.Waypoints && waypoints.Length > 0)
        {
            int i = 0;
            while (!isDead)
            {
                if (chasing) { yield return null; continue; }
                Vector2 target = waypoints[i].position;
                if (lockYToStart) target.y = lockedY;
                yield return MoveTo(target, PatrolSpeedEff);
                yield return new WaitForSeconds(waitAtPoint);
                i = (i + 1) % waypoints.Length;
            }
        }
        else
        {
            while (!isDead)
            {
                if (chasing) { yield return null; continue; }
                Vector2 pos = rb.position;
                float tx = rangeDir > 0 ? rightBound : leftBound;
                float nx = Mathf.MoveTowards(pos.x, tx, PatrolSpeedEff * Time.fixedDeltaTime);
                Vector2 next = new(nx, lockYToStart ? lockedY : pos.y);
                rb.MovePosition(next);
                UpdateFacing(nx - pos.x);
                UpdateAnim(Mathf.Abs(nx - pos.x) > 0.001f);
                if (Mathf.Abs(nx - tx) <= boundLeeway)
                {
                    rangeDir *= -1;
                    UpdateAnim(false);
                    yield return new WaitForSeconds(waitAtPoint);
                }
                yield return new WaitForFixedUpdate();
            }
        }
    }

    private IEnumerator MoveTo(Vector2 target, float spd)
    {
        while (!chasing && !isDead && Vector2.Distance(rb.position, target) > 0.05f)
        {
            Vector2 pos = rb.position;
            Vector2 next = Vector2.MoveTowards(pos, target, spd * Time.fixedDeltaTime);
            if (lockYToStart) next.y = lockedY;
            rb.MovePosition(next);
            UpdateFacing(next.x - pos.x);
            UpdateAnim(true);
            yield return new WaitForFixedUpdate();
        }
        UpdateAnim(false);
    }

    // VISION EVENTS REMOVED

    private void OnPlayerSpotted(Transform t)
    {
        if (!chaseOnSight || IsInSmoke || IsInCold || isDead) return;
        if (!chasing)
        {
            player = t;
            chasing = true;
            StopPatrol();
            StartCoroutine(ChaseLoop());
        }
    }

    private void OnPlayerLost()
    {
        player = null;
        if (chasing)
        {
            chasing = false;
            ResumePatrol();
        }
    }

    private IEnumerator ChaseLoop()
    {
        while (chasing && player && !IsInSmoke && !IsInCold && !isDead)
        {
            Vector2 pos = rb.position;
            Vector2 goal = player.position;
            if (lockYToStart) goal.y = lockedY;

            float dist = Vector2.Distance(pos, goal);

            if (dist > stopChaseDistance)
            {
                Vector2 next = Vector2.MoveTowards(pos, goal, ChaseSpeedEff * Time.fixedDeltaTime);
                if (lockYToStart) next.y = lockedY;
                rb.MovePosition(next);
                UpdateFacing(next.x - pos.x);
                UpdateAnim(true);
            }
            else
            {
                UpdateAnim(false);
            }

            emotionUI?.ForceAlert();
            yield return new WaitForFixedUpdate();
        }
        UpdateAnim(false);
    }

    //CONTACT DAMAGE

    private void TryTouchDamage(Collider2D other)
    {
        if (!dealContactDamage || isDead) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        if (PlayerHiding.Instance && PlayerHiding.Instance.IsHidingInContainer) return;
        if (IsInSmoke || IsInCold) return;

        int id = other.transform.root.GetInstanceID();
        float now = Time.time;
        if (lastHit.TryGetValue(id, out float t) && now - t < perTargetCooldown)
            return;

        PlayerHealth.TryDamagePlayer(contactDamage, transform.position, knockbackForce);
        lastHit[id] = now;
    }

    private void OnCollisionEnter2D(Collision2D c) => TryTouchDamage(c.collider);
    private void OnCollisionStay2D(Collision2D c) => TryTouchDamage(c.collider);
    private void OnTriggerEnter2D(Collider2D o) => TryTouchDamage(o);
    private void OnTriggerStay2D(Collider2D o) => TryTouchDamage(o);

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
        if (!isDead && patrolRoutine == null)
            patrolRoutine = StartCoroutine(PatrolLoop());
    }

    private void UpdateFacing(float dx)
    {
        if (Mathf.Abs(dx) > 0.001f)
            facingSign = dx < 0 ? -1 : 1;

        if (spriteRenderer)
            spriteRenderer.flipX = facingSign < 0;
    }

    private void UpdateAnim(bool walk)
    {
        if (!animator) return;
        animator.SetBool("IsWalking", walk);
        animator.SetBool("IsIdle", !walk);
    }

    public void TakeDamage(int amt)
    {
        if (isDead) return;
        currentHealth -= Mathf.Max(1, amt);
        if (currentHealth <= 0) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        OnAnyEnemyDied?.Invoke(this);
        if (!string.IsNullOrEmpty(missionTag))
            MissionManager.Instance?.MarkKillComplete(missionTag);

        StopAllCoroutines();
        chasing = false;

        if (animator)
        {
            animator.SetTrigger("Die");
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsIdle", false);
        }

        if (emotionUI)
            emotionUI.gameObject.SetActive(false);

        dealContactDamage = false;

        foreach (var c in myCols) if (c) c.enabled = false;

        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    public void GoToTarget(Transform target, float stopDist, Action<EnemyController> onReach)
    {
        if (alarmSeekRoutine != null) StopCoroutine(alarmSeekRoutine);
        alarmSeekRoutine = StartCoroutine(SeekRoutine(target, stopDist, onReach));
    }

    private IEnumerator SeekRoutine(Transform t, float stopDist, Action<EnemyController> onReach)
    {
        chasing = false;
        StopPatrol();

        while (!isDead && t)
        {
            Vector2 pos = rb.position;
            Vector2 goal = t.position;
            if (lockYToStart) goal.y = lockedY;

            if (Vector2.Distance(pos, goal) <= stopDist)
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
