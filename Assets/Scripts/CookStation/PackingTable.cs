using UnityEngine;

public class PackingTable : InteractStation
{
    [Header("QTE Settings")]
    public int arrowCount = 3;
    public float timePerKey = 1.2f;

    [Header("SFX Keys")]
    public string sfxPackingLoop = "SFX_Packing_Loop";
    public string sfxPackingSuccess = "SFX_Packing_Success";

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
        if (!player.HasBowl())
        {
            Debug.Log("Packing: Need bowl with finished product.");
            return;
        }

        var bowl = player.bowl;

        // Allow Finished or Sliced
        if (bowl.state != ContainerData.ContainerState.Finished &&
            bowl.state != ContainerData.ContainerState.Sliced)
        {
            Debug.Log("Packing: Item not ready.");
            return;
        }

        currentPlayer = player;
        currentController = player.GetComponent<PlayerController>();

        LockInteraction();
        currentController.DisableMovement();
        currentController.SetCooking(true);

        StartPackingLoop();
        StartSequenceQTE();
    }

    private void StartSequenceQTE()
    {
        string[] seq = GenerateRandomArrowSequence(arrowCount);

        QTEManager.Instance.OnQTEFinished += OnQTEFinished;
        QTEManager.Instance.StartSequenceQTE(seq, timePerKey);
    }

    private void OnQTEFinished(QTEResult result)
    {
        QTEManager.Instance.OnQTEFinished -= OnQTEFinished;

        StopPackingLoop();

        currentController.SetCooking(false);
        currentController.EnableMovement();
        UnlockInteraction();

        if (result == QTEResult.Fail)
        {
            Debug.Log("Packing failed.");
            return;
        }

        Debug.Log("Packing success!");

        // Convert to serve box (supports sliced variant)
        currentPlayer.ConvertToServeBox();

        // Return empty bowl to counter
        ContainerData empty = new ContainerData();
        empty.state = ContainerData.ContainerState.Empty;
        currentPlayer.ReturnBowlToLastCounter(empty);

        currentPlayer.OnInventoryChanged?.Invoke();

        PlaySuccessSFX();
    }

    private string[] GenerateRandomArrowSequence(int count)
    {
        string[] pool = { "left", "right", "up", "down" };
        string[] seq = new string[count];

        for (int i = 0; i < count; i++)
            seq[i] = pool[Random.Range(0, pool.Length)];

        return seq;
    }

   
    private void StartPackingLoop()
    {
        if (AudioManager.Instance == null) return;

        var clip = AudioManager.Instance.GetClipByKey(sfxPackingLoop);
        if (clip == null) return;

        loopSource.clip = clip;
        loopSource.volume = AudioManager.Instance.sfxVolume * AudioManager.Instance.masterVolume;
        loopSource.Play();
    }

    private void StopPackingLoop()
    {
        if (loopSource.isPlaying)
            loopSource.Stop();
    }

    
    private void PlaySuccessSFX()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.PlaySFXAt(
            sfxPackingSuccess,
            transform.position,
            true
        );
    }
}
