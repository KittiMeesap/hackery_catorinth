using UnityEngine;
using UnityEngine.UI;

public class HeartSlot : MonoBehaviour
{
    public Image image;

    public void SetSprite(Sprite sprite)
    {
        if (image != null) image.sprite = sprite;
    }
}