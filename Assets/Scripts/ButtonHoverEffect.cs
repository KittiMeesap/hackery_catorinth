using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonScaleOnHover : MonoBehaviour
{
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(BaseEventData eventData)
    {
        transform.localScale = hoverScale;
    }

    public void OnPointerExit(BaseEventData eventData)
    {
        transform.localScale = originalScale;
    }
}
