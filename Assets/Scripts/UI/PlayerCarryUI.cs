using UnityEngine;
using UnityEngine.UI;

public class PlayerCarryUI : MonoBehaviour
{
    public Transform iconHolder;
    public GameObject ingredientIconPrefab;

    private PlayerInventory inventory;

    private void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();
        inventory.OnInventoryChanged += RefreshUI;
        RefreshUI();
    }

    void RefreshUI()
    {
        foreach (Transform child in iconHolder)
            Destroy(child.gameObject);

        if (!inventory.HasBowl() && !inventory.HasServeBox())
            return;

        // ---------- ServeBox ----------
        if (inventory.HasServeBox())
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = inventory.serveBox.resultRecipe.outputIcon;
            return;
        }

        // ---------- Bowl ----------
        var bowl = inventory.bowl;

        if (bowl == null || bowl.contents.Count == 0)
            return;

        if (bowl.state == ContainerData.ContainerState.Mixed ||
            bowl.state == ContainerData.ContainerState.Cooling ||
            bowl.state == ContainerData.ContainerState.Finished)
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = bowl.GetIcon();
            return;
        }

        // raw ingredients
        for (int i = 0; i < bowl.contents.Count; i++)
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = bowl.contents[i].icon;
        }
    }

}
