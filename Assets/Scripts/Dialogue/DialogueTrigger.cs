using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        OnSceneStart,
        OnEnter,
        OnInteractIn
    }

    [Header("Dialogue")]
    [SerializeField] private DialogueAsset dialogue;

    [Header("Trigger Mode")]
    [SerializeField] private TriggerMode mode = TriggerMode.OnEnter;

    [SerializeField] private string playerTag = "Player";

    [SerializeField] private InputActionReference interactAction;

    [SerializeField] private bool autoEnableAction = true;

    [SerializeField] private GameObject interactPrompt;

    [Header("Repeat / Persistence")]
    [SerializeField] private bool oneShot = true;

    [SerializeField] private bool persistentAcrossSessions = false;

    [SerializeField] private string persistenceId = "";

    [Header("Misc")]
    [SerializeField] private bool ignoreWhileAnotherDialogueRunning = true;

    private bool played = false;
    private bool inside = false;
    private string prefKey;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider2D>();
        if (col && !col.isTrigger) col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            if (autoEnableAction && !interactAction.action.enabled)
                interactAction.action.Enable();

            interactAction.action.performed += OnInteractAction;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractAction;

            if (autoEnableAction && interactAction.action.enabled)
                interactAction.action.Disable();
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(persistenceId))
            prefKey = $"{gameObject.scene.name}:{gameObject.name}:DialoguePlayed";
        else
            prefKey = $"DialoguePlayed:{persistenceId}";

        if (persistentAcrossSessions)
            played = PlayerPrefs.GetInt(prefKey, 0) == 1;

        if (interactPrompt) interactPrompt.SetActive(false);

        if (mode == TriggerMode.OnSceneStart)
            TryStartDialogue();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        inside = true;

        if (mode == TriggerMode.OnEnter)
        {
            TryStartDialogue();
        }
        else if (mode == TriggerMode.OnInteractIn)
        {
            if (interactPrompt && !(played && oneShot))
                interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        inside = false;

        if (interactPrompt)
            interactPrompt.SetActive(false);
    }

    private void OnInteractAction(InputAction.CallbackContext ctx)
    {
        if (mode != TriggerMode.OnInteractIn) return;
        if (!inside) return;
        if (played && oneShot) return;

        if (ignoreWhileAnotherDialogueRunning &&
            DialogueManager.Instance != null &&
            DialogueManager.Instance.IsRunning)
            return;

        TryStartDialogue();
    }

    public void PlayNow() => TryStartDialogue();

    [ContextMenu("Reset Persistent Played State")]
    public void ResetPersistent()
    {
        played = false;
        if (persistentAcrossSessions)
        {
            PlayerPrefs.DeleteKey(prefKey);
            PlayerPrefs.Save();
        }
    }

    private void TryStartDialogue()
    {
        if (dialogue == null) return;
        if (played && oneShot) return;

        if (ignoreWhileAnotherDialogueRunning &&
            DialogueManager.Instance != null &&
            DialogueManager.Instance.IsRunning)
            return;

        DialogueManager.Instance?.Play(dialogue);

        if (oneShot)
        {
            played = true;
            if (persistentAcrossSessions)
            {
                PlayerPrefs.SetInt(prefKey, 1);
                PlayerPrefs.Save();
            }
            if (interactPrompt) interactPrompt.SetActive(false);
        }
    }
}
