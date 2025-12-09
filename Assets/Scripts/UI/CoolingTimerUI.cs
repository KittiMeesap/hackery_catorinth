using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoolingTimerUI : MonoBehaviour
{
    public Image fillBar;
    public TextMeshProUGUI timeText;

    private void Start()
    {
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdateUI(float progress, float remaining)
    {
        fillBar.fillAmount = Mathf.Clamp01(progress);
        timeText.text = Mathf.CeilToInt(Mathf.Max(remaining, 0f)).ToString();
    }
}
