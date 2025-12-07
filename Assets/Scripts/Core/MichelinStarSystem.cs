using System;
using UnityEngine;
using UnityEngine.UI;

public class MichelinStarSystem : MonoBehaviour
{
    public static MichelinStarSystem Instance { get; private set; }

    [Header("Config")]
    public int maxStars = 5;

    [Header("Sprites")]
    public Sprite fullStarSprite;
    public Sprite emptyStarSprite;

    [Header("UI Star Slots")]
    public Image[] starImages;

    public int CurrentStars { get; private set; }

    public event Action<int> OnStarChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ResetStars();
    }

    public void ResetStars()
    {
        CurrentStars = maxStars;
        RefreshUI();
        OnStarChanged?.Invoke(CurrentStars);
    }

    public void LoseStar(int amount = 1)
    {
        CurrentStars = Mathf.Max(0, CurrentStars - amount);
        RefreshUI();
        OnStarChanged?.Invoke(CurrentStars);
    }

    public void GainStar(int amount = 1)
    {
        CurrentStars = Mathf.Min(maxStars, CurrentStars + amount);
        RefreshUI();
        OnStarChanged?.Invoke(CurrentStars);
    }

    private void RefreshUI()
    {
        if (starImages == null || starImages.Length == 0) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (i < CurrentStars)
                starImages[i].sprite = fullStarSprite;
            else
                starImages[i].sprite = emptyStarSprite;
        }
    }
}
