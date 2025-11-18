using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class ComicCutscene : MonoBehaviour
{
    [Header("Cutscene Pages (GameObjects in order)")]
    public GameObject[] pageObjects;

    [Header("Options")]
    public bool pauseGameTime = true;
    public float panelFadeDuration = 0.4f;
    public float panelInterval = 0.25f;
    public float subtitleFadeDuration = 0.35f;

    [Header("Scene Transition")]
    public bool loadSceneAtEnd = false;
    public string nextSceneName;

    private int currentIndex = 0;
    private bool allowNext = false;
    private bool isPlaying = false;

    private void Start()
    {
        StartCutscene();
    }

    public void StartCutscene()
    {
        if (pageObjects == null || pageObjects.Length == 0)
        {
            Debug.LogWarning("ComicCutscene: No pages assigned.");
            return;
        }

        foreach (var page in pageObjects)
            page.SetActive(false);

        if (pauseGameTime)
            Time.timeScale = 0f;

        currentIndex = 0;
        isPlaying = true;

        StartCoroutine(ShowPageRoutine());
    }

    private void Update()
    {
        if (!isPlaying || !allowNext) return;

        bool spacePressed = Keyboard.current?.spaceKey.wasPressedThisFrame == true;
        bool clickPressed = Mouse.current?.leftButton.wasPressedThisFrame == true;
        bool controllerPressed = Gamepad.current?.buttonSouth.wasPressedThisFrame == true;

        if (spacePressed || clickPressed || controllerPressed)
        {
            NextPage();
        }
    }

    IEnumerator ShowPageRoutine()
    {
        allowNext = false;

        foreach (var p in pageObjects)
            p.SetActive(false);

        GameObject currentPage = pageObjects[currentIndex];
        currentPage.SetActive(true);

        List<Image> panels = new List<Image>();
        foreach (Transform child in currentPage.transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
                panels.Add(img);
        }

        TextMeshProUGUI tmpSub = currentPage.GetComponentInChildren<TextMeshProUGUI>(true);

        if (tmpSub != null)
        {
            Color c = tmpSub.color;
            c.a = 0;
            tmpSub.color = c;
            tmpSub.gameObject.SetActive(false);
        }

        foreach (var p in panels)
        {
            Color c = p.color;
            c.a = 0;
            p.color = c;
        }

        for (int i = 0; i < panels.Count; i++)
        {
            yield return FadeInPanel(panels[i]);
            yield return new WaitForSecondsRealtime(panelInterval);
        }

        if (tmpSub != null)
        {
            tmpSub.gameObject.SetActive(true);
            yield return FadeInText(tmpSub);
        }

        allowNext = true;
    }

    IEnumerator FadeInPanel(Image panel)
    {
        float t = 0f;
        while (t < panelFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0, 1, t / panelFadeDuration);
            panel.color = new Color(1, 1, 1, a);
            yield return null;
        }
        panel.color = new Color(1, 1, 1, 1);
    }

    IEnumerator FadeInText(Graphic textObj)
    {
        float t = 0f;
        while (t < subtitleFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(0, 1, t / subtitleFadeDuration);
            Color c = textObj.color;
            c.a = a;
            textObj.color = c;
            yield return null;
        }
        Color final = textObj.color;
        final.a = 1;
        textObj.color = final;
    }

    void NextPage()
    {
        currentIndex++;

        if (currentIndex >= pageObjects.Length)
        {
            EndCutscene();
        }
        else
        {
            StartCoroutine(ShowPageRoutine());
        }
    }

    void EndCutscene()
    {
        isPlaying = false;

        if (pauseGameTime)
            Time.timeScale = 1f;

        foreach (var p in pageObjects)
            p.SetActive(false);

        if (loadSceneAtEnd && !string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        Destroy(gameObject);
    }
}
