using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BGMZone : MonoBehaviour
{
    [Header("BGM Settings")]
    [SerializeField] private string bgmKey;
    [SerializeField] private bool revertToDefaultOnExit = true;
    [SerializeField] private string defaultBGMKey = "BGM_Default";
    [SerializeField] private bool crossfade = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!string.IsNullOrEmpty(bgmKey))
            AudioManager.Instance?.PlayBGM(bgmKey, crossfade);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (revertToDefaultOnExit && !string.IsNullOrEmpty(defaultBGMKey))
            AudioManager.Instance?.PlayBGM(defaultBGMKey, crossfade);
    }
}