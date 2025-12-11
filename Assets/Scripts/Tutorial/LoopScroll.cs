using UnityEngine;
using UnityEngine.UI;

public class LoopScroll : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform content;

    private float itemWidth;
    private int itemCount;

    private void Start()
    {
        itemCount = content.childCount;

       
        itemWidth = content.GetChild(0).GetComponent<RectTransform>().rect.width;
    }

    private void Update()
    {
        float posX = content.anchoredPosition.x;
        float totalWidth = itemWidth * itemCount;

        
        if (posX > itemWidth)
        {
            content.anchoredPosition -= new Vector2(totalWidth, 0);
        }
        
        else if (posX < -totalWidth)
        {
            content.anchoredPosition += new Vector2(totalWidth, 0);
        }
    }
}
