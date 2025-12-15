using UnityEngine;

public class Mixer : InteractStation
{
    [Header("SFX Keys")]
    public string sfxMixLoop = "SFX_Mixer_Loop";
    public string sfxMixSuccess = "SFX_Mixer_Success";

    private bool isMixing;
    private PlayerInventory currentPlayer;
    private PlayerController currentController;

    private AudioSource loopSource;

    protected override void Awake()
    {
        base.Awake();

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.spatialBlend = 1f;
    }

    public override void Interact(PlayerInventory player)
    {
        if (isMixing)
        {
            NotificationUI.Instance?.Show("Already mixing!", NotifyType.Info);
            return;
        }

        if (!player.HasBowl())
        {
            NotificationUI.Instance?.Show("You need a bowl first!", NotifyType.Warning);
            return;
        }

        if (player.bowl.IsAlreadyMixed())
        {
            NotificationUI.Instance?.Show(
                "This dish has already been mixed",
                NotifyType.Info
            );
            return;
        }

        if (!RecipeManager.Instance.CanMix(player.bowl.contents))
        {
            NotificationUI.Instance?.Show(
                "These ingredients can't be mixed yet",
                NotifyType.Error
            );
            return;
        }

        // =========================
        // START MIXING
        // =========================
        isMixing = true;
        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        LockInteraction();
        currentController.DisableMovement();
        currentController.SetCooking(true);

        StartMixerLoop();

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartMashQTE();
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        StopMixerLoop();

        currentController.SetCooking(false);
        currentController.EnableMovement();
        UnlockInteraction();

        if (result == QTEResult.Success)
        {
            if (currentPlayer.bowl.TryMix())
            {
                currentPlayer.OnInventoryChanged?.Invoke();
                PlaySuccessSFX();
            }
        }
        else
        {
            NotificationUI.Instance?.Show(
                "Mixing failed!",
                NotifyType.Warning
            );
        }

        isMixing = false;
    }

    // =========================
    // ?? LOOP SOUND
    // =========================
    private void StartMixerLoop()
    {
        if (AudioManager.Instance == null) return;

        var clip = AudioManager.Instance.GetClipByKey(sfxMixLoop);
        if (clip == null) return;

        loopSource.clip = clip;
        loopSource.volume =
            AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
        loopSource.Play();
    }

    private void StopMixerLoop()
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
            sfxMixSuccess,
            transform.position,
            true
        );
    }

    // =========================
    // CLEANUP
    // =========================
    private void OnDisable()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (QTEManager.Instance != null)
            QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        StopMixerLoop();

        if (currentController != null)
        {
            currentController.SetCooking(false);
            currentController.EnableMovement();
            currentController.ForceIdle();
        }

        UnlockInteraction();
        isMixing = false;
    }
}
