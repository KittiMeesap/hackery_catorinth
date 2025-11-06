using UnityEngine;
using UnityEngine.UI;

public class ArrowUI : MonoBehaviour
{
    public enum Direction { Up, Down, Left, Right, None }

    private Image arrowImage;
    private Direction arrowDirection;

    [Header("Arrow Colors")]
    public Color correctColor = Color.green;
    public Color incorrectColor = Color.red;

    private void Awake()
    {
        arrowImage = GetComponent<Image>();
        if (arrowImage == null)
            arrowImage = GetComponentInChildren<Image>();

        if (arrowImage == null)
            Debug.LogWarning($"[ArrowUI] Missing Image component on {gameObject.name}");
    }

    public void Initialize(Direction direction)
    {
        arrowDirection = direction;
        ResetArrow();
    }

    public Direction GetArrowDirection() => arrowDirection;

    public void SetCorrect()
    {
        if (arrowImage != null)
            arrowImage.color = correctColor;
    }

    public void SetIncorrect()
    {
        if (arrowImage != null)
            arrowImage.color = incorrectColor;
    }

    public void ResetArrow()
    {
        if (arrowImage != null)
            arrowImage.color = Color.white;
    }
}
