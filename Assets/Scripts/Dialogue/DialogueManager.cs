using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Config")]
    [Tooltip("If not assigned, the manager will try to detect player from the DialogueAsset.")]
    [SerializeField] private CharacterData playerCharacter;
    [SerializeField] private DialogueUI ui;

    public event Action DialogueStarted;
    public event Action DialogueEnded;

    public bool IsRunning => running;

    private bool running = false;
    private Coroutine waitCo;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (ui) ui.SetVisible(false);
    }

    public void Play(DialogueAsset asset)
    {
        if (running || asset == null)
        {
            if (asset == null) Debug.LogWarning("DialogueManager.Play: asset is null.");
            return;
        }
        if (ui == null)
        {
            Debug.LogError("DialogueManager: DialogueUI is not assigned.");
            return;
        }

        var resolvedPlayer = ResolvePlayerCharacter(asset);

        running = true;
        DialogueStarted?.Invoke();

        ui.Play(asset, resolvedPlayer);

        if (waitCo != null) StopCoroutine(waitCo);
        waitCo = StartCoroutine(WaitForClose());
    }

    public Task PlayAsync(DialogueAsset asset)
    {
        if (asset == null || ui == null || running)
        {
            if (asset == null) Debug.LogWarning("DialogueManager.PlayAsync: asset is null.");
            return Task.CompletedTask;
        }

        var resolvedPlayer = ResolvePlayerCharacter(asset);
        var tcs = new TaskCompletionSource<bool>();

        running = true;
        DialogueStarted?.Invoke();

        void OnClosedHandler()
        {
            ui.OnClosed -= OnClosedHandler;
            running = false;
            DialogueEnded?.Invoke();
            tcs.TrySetResult(true);
        }

        ui.OnClosed += OnClosedHandler;
        ui.Play(asset, resolvedPlayer);

        return tcs.Task;
    }

    public IEnumerator PlayRoutine(DialogueAsset asset)
    {
        if (asset == null || ui == null) yield break;

        var resolvedPlayer = ResolvePlayerCharacter(asset);

        bool done = false;
        void OnClosedHandler()
        {
            ui.OnClosed -= OnClosedHandler;
            done = true;
        }

        running = true;
        DialogueStarted?.Invoke();

        ui.OnClosed += OnClosedHandler;
        ui.Play(asset, resolvedPlayer);

        while (!done) yield return null;

        running = false;
        DialogueEnded?.Invoke();
    }

    public void Stop()
    {
        if (!running) return;
        ui?.Close();
    }

    private IEnumerator WaitForClose()
    {
        while (ui != null && ui.IsOpen)
            yield return null;

        running = false;
        waitCo = null;
        DialogueEnded?.Invoke();
    }

    private CharacterData ResolvePlayerCharacter(DialogueAsset asset)
    {
        if (playerCharacter != null) return playerCharacter;

        // Prefer explicit isPlayer flags where author set them
        foreach (var l in asset.lines)
            if (l != null && l.isPlayer && l.characterData != null)
                return l.characterData;

        // Fallback: first non-null characterData
        foreach (var l in asset.lines)
            if (l != null && l.characterData != null)
                return l.characterData;

        return null;
    }
}
