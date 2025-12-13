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

    private PlayerInventory currentPlayer;
    private PlayerController currentController;

    private AudioSource loopSource;

    protected override void Awake()
    {
        base.Awake();

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
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

    public override void Interact(PlayerInventory player)
    {
        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        if (!player.HasBowl()) return;

        var bowl = player.bowl;
        if (bowl.matchedRecipe == null) return;
        if (!bowl.CanBake()) return;

        animator.SetBool(IsBaking, true);

        currentController.SetCooking(true);
        currentController.DisableMovement();
        LockInteraction();

        StartOvenLoop();
        StartQTE();
    }

    private void StartQTE()
    {
        string[] seq = GenerateRandomArrowSequence(arrowCount);

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartSequenceQTE(seq, timePerKey);
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        StopOvenLoop();

        animator.SetBool(IsBaking, false);
        currentController.SetCooking(false);

        if (result == QTEResult.Success)
        {
            currentPlayer.bowl.DoBake();
            currentPlayer.OnInventoryChanged?.Invoke();

            PlaySuccessSFX();
        }

        currentController.EnableMovement();
        UnlockInteraction();
    }

    private string[] GenerateRandomArrowSequence(int count)
    {
        string[] pool = { "left", "right", "up", "down" };
        string[] seq = new string[count];

        for (int i = 0; i < count; i++)
            seq[i] = pool[Random.Range(0, pool.Length)];

        return seq;
    }

    // =========================
    //  LOOP SOUND
    // =========================
    private void StartOvenLoop()
    {
        if (AudioManager.Instance == null) return;

        var clip = AudioManager.Instance.GetClipByKey(sfxOvenLoop);
        if (clip == null) return;

        loopSource.clip = clip;
        loopSource.volume = AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
        loopSource.Play();
    }

    private void StopOvenLoop()
    {
        if (loopSource.isPlaying)
            loopSource.Stop();
    }

    // =========================
    // ? SUCCESS ONE-SHOT
    // =========================
    private void PlaySuccessSFX()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFXAt(
            sfxOvenSuccess,
            transform.position,
            true
        );
    }
}
