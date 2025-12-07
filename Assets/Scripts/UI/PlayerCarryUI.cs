using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

        if (!inventory.HasContainer)
            return;

        var list = inventory.currentContainer.contents;

        if (list.Count == 0)
            return;

        for (int i = 0; i < list.Count && i < 4; i++)
        {
            var inst = Instantiate(ingredientIconPrefab, iconHolder);
            inst.GetComponent<Image>().sprite = list[i].icon;
        }
    }
}
