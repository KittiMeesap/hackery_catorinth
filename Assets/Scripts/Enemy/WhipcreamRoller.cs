using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class WhipcreamRoller : MonoBehaviour, ITemperatureAffectable
{
    public enum State { Idle, Move, Spin, Boom, Stunned }

    [Header("Patrol Range (X only)")]
    [SerializeField] private bool rangeIsOffsetFromStart = true;
    [SerializeField] private float rangeLeft = -2f;
    [SerializeField] private float rangeRight = 2f;
    [SerializeField] private float idleWaitTime = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float spinSpeed = 4f;
    [SerializeField] private float stopSpinDistanceLeeway = 0.05f;

    [Header("Vision")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float visionRadius = 4f;
    [SerializeField] private float visionAngle = 70f;
    [SerializeField] private float visionYOffset = 0.4f;

    [Header("Boom Attack")]
    [SerializeField] private float boomTriggerDistance = 1.0f;
    [SerializeField] private float boomRadius = 1.5f;
    [SerializeField] private int boomDamageHearts = 2;
    [SerializeField] private float boomKnockbackForce = 8f;

    [Header("Temperature")]
    [SerializeField] private float heatStunTime = 0.25f;

    [Header("Flash Settings")]
    [SerializeField] private float flashSpeed = 20f;
    [SerializeField] private Color flashColor = Color.red;

    [Header("Audio Keys")]
    [SerializeField] private string sfxMoveStart = "SFX_WhipMove";
    [SerializeField] private string sfxSpinLoop = "SFX_WhipSpin";
    [SerializeField] private string sfxBoom = "SFX_WhipBoom";
    [SerializeField] private string sfxFreeze = "SFX_WhipFreeze";
    [SerializeField] private string sfxUnfreeze = "SFX_WhipUnfreeze";
    [SerializeField] private string sfxHeatStun = "SFX_WhipHeatStun";

    [Header("Visuals / Anim")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Transform player;

    private State state = State.Idle;
    private int facingSign = 1;
    private float startX;
    private float leftBound;
    private float rightBound;

    private bool attackMode = false;
    private bool isDead = false;
    private bool isFrozen = false;
    private float currentTemperature = 0f;

    private Coroutine stateRoutine;
    private Coroutine flashRoutine;

    public float CurrentTemperature => currentTemperature;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Start()
    {
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

        ChangeState(State.Idle);
    }

    private void ChangeState(State next)
    {
        if (isFrozen && next != State.Idle) return;

        if (stateRoutine != null)
            StopCoroutine(stateRoutine);

        state = next;

        switch (state)
        {
            case State.Idle: stateRoutine = StartCoroutine(IdleState()); break;
            case State.Move: stateRoutine = StartCoroutine(MoveState()); break;
            case State.Spin: stateRoutine = StartCoroutine(SpinState()); break;
            case State.Stunned: stateRoutine = StartCoroutine(StunnedState()); break;
            case State.Boom: stateRoutine = StartCoroutine(BoomState()); break;
        }

        UpdateAnimatorState();
    }

    private IEnumerator IdleState()
    {
        rb.linearVelocity = Vector2.zero;
        attackMode = false;

        float timer = 0f;

        while (state == State.Idle && !isDead)
        {
            if (isFrozen)
            {
                rb.linearVelocity = Vector2.zero;
                yield return null;
                continue;
            }

            EnsurePlayerRef();

            if (PlayerInVision())
            {
                float dx = player.position.x - transform.position.x;
                SetFacing(Mathf.Sign(dx));
                attackMode = true;
                ChangeState(State.Move);
                yield break;
            }

            timer += Time.deltaTime;
            if (timer >= idleWaitTime)
            {
                ChangeState(State.Move);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator MoveState()
    {
        AudioManager.Instance?.PlaySFX(sfxMoveStart);

        float moveDuration = 0.2f;
        float timer = 0f;

        while (state == State.Move && !isDead && timer < moveDuration)
        {
            if (isFrozen)
            {
                ChangeState(State.Idle);
                yield break;
            }

            rb.linearVelocity = new Vector2(facingSign * moveSpeed, 0f);

            if (ShouldTriggerBoom())
            {
                ChangeState(State.Boom);
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        ChangeState(State.Spin);
    }

    private IEnumerator SpinState()
    {
        float maxSpinTime = 3f;
        float timer = 0f;

        while (state == State.Spin && !isDead)
        {
            if (isFrozen)
            {
                ChangeState(State.Idle);
                yield break;
            }

            rb.linearVelocity = new Vector2(facingSign * spinSpeed, 0f);

            if (ShouldTriggerBoom())
            {
                ChangeState(State.Boom);
                yield break;
            }

            if (HitObstacleAhead())
            {
                ChangeState(State.Idle);
                yield break;
            }

            if (!attackMode)
            {
                float x = rb.position.x;
                if (facingSign > 0 && x >= rightBound)
                {
                    rb.position = new Vector2(rightBound, rb.position.y);
                    SetFacing(-1);
                    ChangeState(State.Idle);
                    yield break;
                }
                else if (facingSign < 0 && x <= leftBound)
                {
                    rb.position = new Vector2(leftBound, rb.position.y);
                    SetFacing(1);
                    ChangeState(State.Idle);
                    yield break;
                }
            }

            timer += Time.deltaTime;
            if (timer >= maxSpinTime)
            {
                ChangeState(State.Idle);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator StunnedState()
    {
        rb.linearVelocity = Vector2.zero;

        AudioManager.Instance?.PlaySFX(sfxHeatStun);

        FlashRed(heatStunTime);

        yield return new WaitForSeconds(heatStunTime);

        ChangeState(State.Boom);
    }

    private IEnumerator BoomState()
    {
        rb.linearVelocity = Vector2.zero;

        FlashRed(0.15f);

        if (animator)
            animator.SetTrigger("Boom");

        AudioManager.Instance?.PlaySFX(sfxBoom);

        yield return new WaitForSeconds(0.1f);

        DoExplosionDamage();

        yield return new WaitForSeconds(0.25f);

        Destroy(gameObject);
    }

    private void FlashRed(float duration)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(duration));
    }

    private IEnumerator FlashRoutine(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            float t = Mathf.Sin(Time.time * flashSpeed) * 0.5f + 0.5f;
            spriteRenderer.color = Color.Lerp(Color.white, flashColor, t);

            timer += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = Color.white;
    }

    public void ApplyCold(float delta)
    {
        if (isDead) return;

        isFrozen = true;
        currentTemperature -= delta;

        AudioManager.Instance?.PlaySFX(sfxFreeze);

        rb.linearVelocity = Vector2.zero;
        ChangeState(State.Idle);
    }

    public void ExitCold()
    {
        isFrozen = false;
        currentTemperature = 0f;

        AudioManager.Instance?.PlaySFX(sfxUnfreeze);

        ChangeState(State.Idle);
    }

    public void ApplyHeat(float delta)
    {
        if (isDead) return;

        currentTemperature += delta;

        ChangeState(State.Stunned);
    }

    public void CoolDown(float delta)
    {
        currentTemperature -= delta;
        if (currentTemperature < 0)
            currentTemperature = 0;
    }

    private void EnsurePlayerRef()
    {
        if (player != null) return;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go) player = go.transform;
    }

    private bool PlayerInVision()
    {
        if (!player) return false;

        Vector2 origin = transform.position + new Vector3(0, visionYOffset);
        Vector2 dir = player.position - transform.position;
        float dist = dir.magnitude;

        if (dist > visionRadius) return false;

        float angle = Vector2.Angle(new Vector2(facingSign, 0), dir.normalized);
        if (angle > visionAngle * 0.5f) return false;

        var hit = Physics2D.Raycast(origin, dir.normalized, dist, obstacleLayers);
        if (hit.collider != null) return false;

        return true;
    }

    private bool ShouldTriggerBoom()
    {
        return player && Vector2.Distance(transform.position, player.position) <= boomTriggerDistance;
    }

    private bool HitObstacleAhead()
    {
        return Physics2D.Raycast(rb.position, new Vector2(facingSign, 0), 0.25f, obstacleLayers);
    }

    private void DoExplosionDamage()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, boomRadius, playerLayer);

        if (hit)
        {
            PlayerHealth.TryDamagePlayer(boomDamageHearts, transform.position, boomKnockbackForce);
        }
    }

    private void SetFacing(float dir)
    {
        facingSign = dir >= 0 ? 1 : -1;
        spriteRenderer.flipX = facingSign < 0;
    }

    private void UpdateAnimatorState()
    {
        animator.SetBool("IsIdle", state == State.Idle);
        animator.SetBool("IsMove", state == State.Move);
        animator.SetBool("IsSpin", state == State.Spin);
    }
}
