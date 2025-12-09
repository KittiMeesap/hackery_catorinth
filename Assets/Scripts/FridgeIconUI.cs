using UnityEngine;
using UnityEngine.UI;

public class FridgeIconUI : MonoBehaviour
{
    [Header("Prefab & Holder")]
    public Transform iconHolder;
    public GameObject ingredientIconPrefab;

    public void Refresh(ContainerData bowl)
    {
        foreach (Transform child in iconHolder)
            Destroy(child.gameObject);

        if (bowl == null)
            return;

        // Show final processed icon
        if (bowl.state == ContainerData.ContainerState.Mixed ||
            bowl.state == ContainerData.ContainerState.Cooling ||
            bowl.state == ContainerData.ContainerState.Finished ||
            bowl.state == ContainerData.ContainerState.Baked)
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = bowl.GetIcon();
            return;
        }

        // Show raw ingredients if not mixed yet
        foreach (var ing in bowl.contents)
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = ing.icon;
        }
    }

    public void Clear()
    {
        foreach (Transform child in iconHolder)
            Destroy(child.gameObject);
    }
}
