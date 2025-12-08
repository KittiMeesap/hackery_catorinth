using UnityEngine;
using UnityEngine.UI;

public class CounterIconUI : MonoBehaviour
{
    [Header("Prefab & Holder")]
    public Transform iconHolder;
    public GameObject ingredientIconPrefab;

    public void Refresh(ContainerData bowl)
    {
        // Clear old icons
        foreach (Transform child in iconHolder)
            Destroy(child.gameObject);

        if (bowl == null)
            return;

        // ---- Mixed or Finished dishes ----
        if (bowl.state == ContainerData.ContainerState.Mixed ||
            bowl.state == ContainerData.ContainerState.Cooling ||
            bowl.state == ContainerData.ContainerState.Finished)
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = bowl.GetIcon();
            return;
        }

        // ---- Raw Ingredients ----
        for (int i = 0; i < bowl.contents.Count; i++)
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = bowl.contents[i].icon;
        }
    }

    public void Clear()
    {
        foreach (Transform child in iconHolder)
            Destroy(child.gameObject);
    }
}
