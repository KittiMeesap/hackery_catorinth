using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    public bool IsPhoneOut { get; private set; } = false;
    public bool IsInHackingMode { get; private set; } = false;

    [Header("Mission Settings")]
    public MissionSetSO missionSetForScene;
    public TextMeshProUGUI missionText;

    [Header("External References")]
    public CountdownTimer countdownManager;

    [Header("Screen Fade")]
    public ScreenFader screenFader;

    [Header("Checkpoint System")]
    public Transform currentCheckpoint;
    public float savedCountdownTime = 0f;

    [Header("Door Unlock Persistence")]
    public HashSet<string> unlockedDoors = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (screenFader != null)
            StartCoroutine(screenFader.FadeIn());
    }

    public void ToggleHackingMode(bool isActive)
    {
        IsInHackingMode = isActive;
    }

    public void SetPhoneOut(bool isOut)
    {
        IsPhoneOut = isOut;
    }

    public void SetCheckpoint(Transform point)
    {
        currentCheckpoint = point;

        if (countdownManager != null)
            savedCountdownTime = countdownManager.GetCurrentTime();

        Debug.Log($"Checkpoint Saved at {point.name} | Time Left = {savedCountdownTime}");
    }

    public void RespawnPlayer(GameObject player)
    {
        if (player == null) return;

        if (currentCheckpoint != null)
            player.transform.position = currentCheckpoint.position;

        if (countdownManager != null)
            countdownManager.SetTime(savedCountdownTime);

        Debug.Log($"Respawned | Restored Countdown = {savedCountdownTime}");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void FreezeGame(bool freeze)
    {
        if (freeze)
        {
            Time.timeScale = 0f;

            if (GameFreezeManager.Instance != null)
                GameFreezeManager.Instance.FreezeGame();
        }
        else
        {
            Time.timeScale = 1f;

            if (GameFreezeManager.Instance != null)
                GameFreezeManager.Instance.UnfreezeGame();
        }
    }
}
