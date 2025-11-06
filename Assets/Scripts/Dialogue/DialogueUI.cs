using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;

public class DialogueUI : MonoBehaviour
{
    [Header("Refs (drag from hierarchy)")]
    [SerializeField] private Image playerPortrait;
    [SerializeField] private Image npcPortrait;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;

    [SerializeField] private GameObject playerNameFrame;
    [SerializeField] private GameObject npcNameFrame;

    [Tooltip("Root panel to show/hide. If null, use this GameObject.")]
    [SerializeField] private GameObject dialogueUI;

    [Header("Typing")]
    [SerializeField, Min(1f)] private float charsPerSecond = 40f;

    [Header("Non-speaker look")]
    [SerializeField] private bool useRuntimeGrayscale = false;
    [SerializeField] private Color inactiveTint = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Behaviour")]
    [SerializeField] private bool pauseGameDuringDialogue = true;
    [SerializeField] private bool freezePlayerDuringDialogue = true;

    public bool IsOpen { get; private set; }
    public System.Action OnClosed;

    private Coroutine typingCo;
    private Sprite playerOriginalSprite;
    private Sprite npcOriginalSprite;
    private readonly Dictionary<Sprite, Sprite> grayscaleCache = new Dictionary<Sprite, Sprite>();
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (playerPortrait) playerPortrait.color = Color.white;
        if (npcPortrait) npcPortrait.color = Color.white;

        if (dialogueUI != null) dialogueUI.SetActive(false);
        IsOpen = false;
    }

    // ---------- Lifecycle ----------
    public void Open()
    {
        if (pauseGameDuringDialogue)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        if (freezePlayerDuringDialogue)
            PlayerController.Instance?.SetFrozen(true);

        dialogueText.text = "";
        ClearNPCUI();

        SetVisible(true);
        IsOpen = true;
    }

    public void Close()
    {
        SetVisible(false);

        if (freezePlayerDuringDialogue)
            PlayerController.Instance?.SetFrozen(false);
        if (pauseGameDuringDialogue)
            Time.timeScale = previousTimeScale;

        IsOpen = false;
        OnClosed?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        if (dialogueUI != null) dialogueUI.SetActive(visible);
        else gameObject.SetActive(visible);
    }

    // ---------- Bind content (portraits only) ----------
    public void SetPlayer(CharacterData player, Sprite overridePortrait = null)
    {
        playerOriginalSprite = overridePortrait ? overridePortrait : player?.defaultPortrait;
        if (playerPortrait) playerPortrait.sprite = playerOriginalSprite;
    }

    public void SetNPC(CharacterData npc, Sprite overridePortrait = null)
    {
        if (npc == null) return; // keep last npc
        npcOriginalSprite = overridePortrait ? overridePortrait : npc?.defaultPortrait;
        if (npcPortrait) npcPortrait.sprite = npcOriginalSprite;
    }

    // ---------- Names via Localization ----------
    private IEnumerator UpdateNames(CharacterData player, CharacterData npc)
    {
        // Player name
        if (playerNameText)
        {
            if (player?.displayName != null)
            {
                var op = player.displayName.GetLocalizedStringAsync();
                while (!op.IsDone) yield return null;
                playerNameText.text = op.Result ?? "";
            }
            else playerNameText.text = "";
        }

        // NPC name
        if (npcNameText)
        {
            if (npc?.displayName != null)
            {
                var op = npc.displayName.GetLocalizedStringAsync();
                while (!op.IsDone) yield return null;
                npcNameText.text = op.Result ?? "";
            }
            else npcNameText.text = "";
        }
    }

    public void HighlightSpeaker(bool playerSpeaking)
    {
        if (playerNameFrame) playerNameFrame.SetActive(playerSpeaking);
        if (npcNameFrame) npcNameFrame.SetActive(!playerSpeaking);
        ApplySpeakerEffects(playerSpeaking);
    }

    private void ApplySpeakerEffects(bool playerSpeaking)
    {
        // Player side
        if (playerPortrait)
        {
            if (playerSpeaking)
            {
                playerPortrait.sprite = playerOriginalSprite;
                playerPortrait.color = Color.white;
            }
            else
            {
                if (useRuntimeGrayscale)
                {
                    var gray = GetOrMakeGrayscale(playerOriginalSprite);
                    if (gray != null) { playerPortrait.sprite = gray; playerPortrait.color = Color.white; }
                    else { playerPortrait.sprite = playerOriginalSprite; playerPortrait.color = inactiveTint; }
                }
                else
                {
                    playerPortrait.sprite = playerOriginalSprite;
                    playerPortrait.color = inactiveTint;
                }
            }
        }

        // NPC side
        if (npcPortrait)
        {
            if (!playerSpeaking)
            {
                npcPortrait.sprite = npcOriginalSprite;
                npcPortrait.color = Color.white;
            }
            else
            {
                if (useRuntimeGrayscale)
                {
                    var gray = GetOrMakeGrayscale(npcOriginalSprite);
                    if (gray != null) { npcPortrait.sprite = gray; npcPortrait.color = Color.white; }
                    else { npcPortrait.sprite = npcOriginalSprite; npcPortrait.color = inactiveTint; }
                }
                else
                {
                    npcPortrait.sprite = npcOriginalSprite;
                    npcPortrait.color = inactiveTint;
                }
            }
        }
    }

    private Sprite GetOrMakeGrayscale(Sprite src)
    {
        if (src == null) return null;
        if (grayscaleCache.TryGetValue(src, out var cached)) return cached;

        var tex = src.texture;
        if (!tex || !tex.isReadable)
        {
            Debug.LogWarning($"Texture not readable for grayscale: {src?.name}");
            return null;
        }

        Rect r = src.rect;
        int w = Mathf.RoundToInt(r.width);
        int h = Mathf.RoundToInt(r.height);
        int x = Mathf.RoundToInt(r.x);
        int y = Mathf.RoundToInt(r.y);

        Color[] pix = tex.GetPixels(x, y, w, h);
        for (int i = 0; i < pix.Length; i++)
        {
            var c = pix[i];
            float g = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            pix[i] = new Color(g, g, g, c.a);
        }

        Texture2D grayTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        grayTex.name = (src.name ?? "portrait") + "_gray";
        grayTex.SetPixels(pix);
        grayTex.Apply(false, false);

        Sprite graySprite = Sprite.Create(grayTex, new Rect(0, 0, w, h), src.pivot, src.pixelsPerUnit);
        grayscaleCache[src] = graySprite;
        return graySprite;
    }

    private void OnDestroy()
    {
        foreach (var kv in grayscaleCache)
        {
            var s = kv.Value;
            if (s)
            {
                if (s.texture) Destroy(s.texture);
                Destroy(s);
            }
        }
        grayscaleCache.Clear();
    }

    // ---------- Typing ----------
    public void SetFullText(string text) => dialogueText.text = text;

    public void StartTyping(string text)
    {
        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(TypeRoutine(text));
    }

    public bool IsTyping => typingCo != null;

    public void SkipTyping()
    {
        if (typingCo != null)
        {
            StopCoroutine(typingCo);
            typingCo = null;
        }
    }

    private IEnumerator TypeRoutine(string text)
    {
        dialogueText.text = "";
        float t = 0f;
        int index = 0;
        while (index < text.Length)
        {
            t += Time.unscaledDeltaTime * charsPerSecond; // still works at timeScale=0
            int newIndex = Mathf.Clamp(Mathf.FloorToInt(t), 0, text.Length);
            if (newIndex != index)
            {
                index = newIndex;
                dialogueText.text = text.Substring(0, index);
            }
            yield return null;
        }
        typingCo = null;
    }

    // ---------- Space-only input ----------
    private IEnumerator WaitForSpaceRelease()
    {
        if (Keyboard.current == null) yield break;
        yield return null; // avoid carry-over press
        while (Keyboard.current.spaceKey.isPressed)
            yield return null;
    }

    private IEnumerator WaitForSpacePress()
    {
        if (Keyboard.current == null) yield break;
        while (!Keyboard.current.spaceKey.wasPressedThisFrame)
            yield return null;
    }

    // ---------- Helpers ----------
    private static bool IsSameCharacter(CharacterData a, CharacterData b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        if (!string.IsNullOrEmpty(a.characterId) && !string.IsNullOrEmpty(b.characterId))
            return a.characterId == b.characterId;
        return false;
    }

    private CharacterData FindFirstOtherCharacter(DialogueAsset asset, CharacterData playerCharacter)
    {
        if (asset?.lines == null) return null;
        foreach (var l in asset.lines)
            if (l?.characterData != null && !IsSameCharacter(l.characterData, playerCharacter))
                return l.characterData;
        return null;
    }

    private void ClearNPCUI()
    {
        npcOriginalSprite = null;
        if (npcPortrait) npcPortrait.sprite = null;
        if (npcNameText) npcNameText.text = "";
    }

    // ---------- Runner ----------
    public void Play(DialogueAsset asset, CharacterData playerCharacter)
    {
        if (asset == null)
        {
            Debug.LogWarning("DialogueUI.Play: asset is null");
            return;
        }

        if (dialogueUI != null && !dialogueUI.activeSelf) dialogueUI.SetActive(true);
        else if (dialogueUI == null && !gameObject.activeInHierarchy) gameObject.SetActive(true);

        StartCoroutine(Run(asset, playerCharacter));
    }

    private IEnumerator Run(DialogueAsset asset, CharacterData playerCharacter)
    {
        Open();

        if (asset.lines == null || asset.lines.Count == 0)
        {
            Debug.LogWarning("DialogueUI.Run: asset has no lines.");
            Close();
            yield break;
        }

        CharacterData currentNPC = FindFirstOtherCharacter(asset, playerCharacter);

        for (int i = 0; i < asset.lines.Count; i++)
        {
            var line = asset.lines[i];

            // Speaker strictly by CharacterData
            bool playerSpeaking = IsSameCharacter(line.characterData, playerCharacter);

            // Track current NPC (supports multiple NPCs across the sequence)
            if (!playerSpeaking && line.characterData != null && !IsSameCharacter(line.characterData, playerCharacter))
                currentNPC = line.characterData;

            // Bind portraits
            SetPlayer(playerCharacter, playerSpeaking ? line.overridePortrait : null);
            SetNPC(currentNPC, !playerSpeaking ? line.overridePortrait : null);

            // Update localized names (await async handles)
            yield return UpdateNames(playerCharacter, currentNPC);

            // Emphasize active speaker
            HighlightSpeaker(playerSpeaking);

            // Resolve localized line text
            string text = "";
            if (line.dialogueText != null)
            {
                var op = line.dialogueText.GetLocalizedStringAsync();
                while (!op.IsDone) yield return null;
                text = op.Result ?? "";
            }

            // Type and input to advance
            StartTyping(text);

            yield return WaitForSpaceRelease();
            yield return WaitForSpacePress();

            if (IsTyping)
            {
                SkipTyping();
                SetFullText(text);
                yield return WaitForSpaceRelease();
                yield return WaitForSpacePress();
            }
        }

        Close();
    }
}
