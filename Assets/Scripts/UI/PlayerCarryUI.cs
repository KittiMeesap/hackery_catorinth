using UnityEngine;
using UnityEngine.UI;

public class PlayerCarryUI : MonoBehaviour
{
    [Header("UI")]
    public Transform iconHolder;
    public GameObject ingredientIconPrefab;

    private PlayerInventory inventory;

    private void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshUI;

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshUI;
    }

    void RefreshUI()
    {
        // Clear old icons
        foreach (Transform child in iconHolder)
            Destroy(child.gameObject);

        if (inventory == null)
            return;

        if (!inventory.HasBowl() && !inventory.HasServeBox())
            return;

        // SERVE BOX
        if (inventory.HasServeBox())
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite =
                inventory.serveBox.resultRecipe.outputIcon;
            return;
        }

        // BOWL
        var bowl = inventory.bowl;
        if (bowl == null)
            return;

        // PROCESSED STATES
        if (IsProcessedState(bowl.state))
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = bowl.GetIcon();
            return;
        }

        // RAW INGREDIENTS
        for (int i = 0; i < bowl.contents.Count; i++)
        {
            var icon = Instantiate(ingredientIconPrefab, iconHolder);
            icon.GetComponent<Image>().sprite = bowl.contents[i].icon;
        }
    }

    // STATE HELPER
    private bool IsProcessedState(ContainerData.ContainerState state)
    {
        return state == ContainerData.ContainerState.Mixed
            || state == ContainerData.ContainerState.Cooling
            || state == ContainerData.ContainerState.Baked
            || state == ContainerData.ContainerState.Finished;
    }
}
