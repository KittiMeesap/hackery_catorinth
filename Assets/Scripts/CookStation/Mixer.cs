using UnityEngine;

public class Mixer : InteractStation
{
    [Header("Settings")]
    public string mashKey = "Q";

    [Header("SFX Keys")]
    public string sfxMixLoop = "SFX_Mixer_Loop";

    private bool isMixing = false;
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

    public override void Interact(PlayerInventory player)
    {
        if (isMixing) return;

        if (!player.HasBowl()) return;
        if (player.bowl.contents.Count == 0) return;
        if (player.bowl.IsAlreadyMixed()) return;
        if (!RecipeManager.Instance.CanMix(player.bowl.contents)) return;

        LockInteraction();
        isMixing = true;

        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        currentController.DisableMovement();
        currentController.SetCooking(true);

        StartMixerLoop();

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartMashQTE(mashKey);
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        StopMixerLoop();

        currentController.EnableMovement();
        currentController.SetCooking(false);

        if (result == QTEResult.Success)
        {
            if (currentPlayer.bowl.TryMix())
                currentPlayer.OnInventoryChanged?.Invoke();
        }

        isMixing = false;
        UnlockInteraction();
    }

    // =========================
    // ?? MIXER LOOP SOUND
    // =========================

    private void StartMixerLoop()
    {
        if (AudioManager.Instance == null) return;

        var clip = AudioManager.Instance.GetClipByKey(sfxMixLoop);
        if (clip == null) return;

        loopSource.clip = clip;
        loopSource.volume = AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
        loopSource.Play();
    }

    private void StopMixerLoop()
    {
        if (loopSource.isPlaying)
            loopSource.Stop();
    }
}
