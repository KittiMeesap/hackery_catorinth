using UnityEngine;

public class Mixer : InteractStation
{
    [Header("SFX Keys")]
    public string sfxMixLoop = "SFX_Mixer_Loop";

    private bool isMixing;
    private PlayerInventory currentPlayer;
    private PlayerController currentController;
    private AudioSource loopSource;

    protected override void Awake()
    {
        base.Awake();
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.spatialBlend = 1f;
    }

    public override void Interact(PlayerInventory player)
    {
        if (isMixing) return;
        if (!player.HasBowl()) return;
        if (!RecipeManager.Instance.CanMix(player.bowl.contents)) return;

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
        currentController.EnableMovement();
        currentController.SetCooking(false);
        UnlockInteraction();

        if (result == QTEResult.Success)
            currentPlayer.bowl.TryMix();

        isMixing = false;
    }

    private void StartMixerLoop()
    {
        var clip = AudioManager.Instance?.GetClipByKey(sfxMixLoop);
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
