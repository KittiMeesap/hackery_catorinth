using UnityEngine;

public class Oven : InteractStation
{
    [Header("Animator")]
    public Animator animator;

    private static readonly int Open = Animator.StringToHash("Open");
    private static readonly int Close = Animator.StringToHash("Close");
    private static readonly int IsBaking = Animator.StringToHash("IsBaking");

    [Header("QTE Settings")]
    public int arrowCount = 4;
    public float timePerKey = 1.2f;

    [Header("SFX Keys")]
    public string sfxOvenLoop = "SFX_Oven_Loop";
    public string sfxOvenSuccess = "SFX_Oven_Success";
    public string sfxOvenFail = "SFX_Food_Burn";

    private PlayerInventory currentPlayer;
    private PlayerController currentController;
    private AudioSource loopSource;

    // =====================================================
    // LIFECYCLE
    // =====================================================
    protected override void Awake()
    {
        base.Awake();

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.spatialBlend = 1f;
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);

        if (other.CompareTag("Player"))
            animator.SetTrigger(Open);
    }

    protected override void OnTriggerExit2D(Collider2D other)
    {
        base.OnTriggerExit2D(other);

        if (other.CompareTag("Player"))
            animator.SetTrigger(Close);
    }

    // =====================================================
    // INTERACT
    // =====================================================
    public override void Interact(PlayerInventory player)
    {
        if (!player.HasBowl() || !player.bowl.CanBake())
            return;

        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        animator.SetBool(IsBaking, true);
        currentController.DisableMovement();
        currentController.SetCooking(true);
        LockInteraction();

        StartOvenLoop();

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartSequenceQTE(
            GenerateSequence(arrowCount),
            timePerKey
        );
    }

    // =====================================================
    // QTE
    // =====================================================
    private LogicalInput[] GenerateSequence(int count)
    {
        LogicalInput[] pool =
        {
            LogicalInput.Left,
            LogicalInput.Right,
            LogicalInput.Up,
            LogicalInput.Down
        };

        LogicalInput[] seq = new LogicalInput[count];
        for (int i = 0; i < count; i++)
            seq[i] = pool[Random.Range(0, pool.Length)];

        return seq;
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        StopOvenLoop();

        animator.SetBool(IsBaking, false);
        currentController.SetCooking(false);
        currentController.EnableMovement();
        UnlockInteraction();

        // ================= SUCCESS =================
        if (result == QTEResult.Success)
        {
            currentPlayer.bowl.DoBake();
            currentPlayer.OnInventoryChanged?.Invoke();

            AudioManager.Instance?.PlaySFXAt(
                sfxOvenSuccess,
                transform.position,
                true
            );
            return;
        }

        // ================= FAIL =================
        if (result == QTEResult.Fail)
        {
            currentPlayer.bowl.Clear();
            currentPlayer.bowl.state = ContainerData.ContainerState.Empty;

            currentPlayer.OnInventoryChanged?.Invoke();

            AudioManager.Instance?.PlaySFXAt(
                sfxOvenFail,
                transform.position,
                true
            );
        }
    }

    // AUDIO
    private void StartOvenLoop()
    {
        var clip = AudioManager.Instance?.GetClipByKey(sfxOvenLoop);
        if (clip == null) return;

        loopSource.clip = clip;
        loopSource.volume =
            AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
        loopSource.Play();
    }

    private void StopOvenLoop()
    {
        if (loopSource.isPlaying)
            loopSource.Stop();
    }
}
