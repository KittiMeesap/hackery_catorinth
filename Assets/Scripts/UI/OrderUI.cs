using UnityEngine;
using TMPro;

public class OrderUI : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI orderText;

    private int target = 0;
    private int current = 0;

    public void Initialize(int targetOrders)
    {
        target = targetOrders;
        current = 0;
        RefreshUI();
    }

    public void SetValue(int completed)
    {
        current = completed;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (orderText != null)
            orderText.text = $"{current}/{target}";
    }
}
