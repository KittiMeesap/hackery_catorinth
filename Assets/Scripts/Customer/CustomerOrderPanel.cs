using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomerOrderPanel : MonoBehaviour
{
    public static CustomerOrderPanel Instance { get; private set; }

    [Header("Root")]
    public GameObject panelRoot;

    [Header("Icons")]
    public Image customerIcon;
    public Image recipeIcon;

    [Header("Timer")]
    public Image timerFill;

    [Header("Groups")]
    public Transform recipeGroup;
    public Transform toolsGroup;

    [Header("Prefabs")]
    public GameObject recipeIconPrefab;
    public GameObject toolIconPrefab;

    [Header("Tool Icons")]
    public List<ToolIconEntry> toolIcons = new();

    [Header("Options")]
    public bool showMixerIcon = false;

    private Dictionary<CustomerToolType, Sprite> toolIconMap;

    [System.Serializable]
    public class ToolIconEntry
    {
        public CustomerToolType type;
        public Sprite icon;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildToolIconMap();
        Hide();
    }

    private void BuildToolIconMap()
    {
        toolIconMap = new Dictionary<CustomerToolType, Sprite>();
        foreach (var e in toolIcons)
        {
            if (!toolIconMap.ContainsKey(e.type) && e.icon != null)
                toolIconMap.Add(e.type, e.icon);
        }
    }

    public void Show(CustomerController customer, RecipeSO recipe, Sprite customerSprite)
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (customerIcon != null)
            customerIcon.sprite = customerSprite;

        if (recipeIcon != null && recipe != null)
            recipeIcon.sprite = recipe.outputIcon;

        BuildRecipeIcons(recipe);
        BuildToolIcons(recipe);
        UpdateTimer(1f);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void UpdateTimer(float normalized)
    {
        if (timerFill != null)
            timerFill.fillAmount = Mathf.Clamp01(normalized);
    }

    private void BuildRecipeIcons(RecipeSO recipe)
    {
        foreach (Transform c in recipeGroup)
            Destroy(c.gameObject);

        if (recipe == null) return;

        foreach (var ing in recipe.ingredients)
        {
            var obj = Instantiate(recipeIconPrefab, recipeGroup);
            var img = obj.GetComponent<Image>();
            if (img != null)
                img.sprite = ing.icon;
        }
    }

    private void BuildToolIcons(RecipeSO recipe)
    {
        foreach (Transform c in toolsGroup)
            Destroy(c.gameObject);

        if (recipe == null) return;

        List<CustomerToolType> tools = new List<CustomerToolType>();

        if (showMixerIcon)
            tools.Add(CustomerToolType.Mixer);

        switch (recipe.flow)
        {
            case ProcessFlow.BakeOnly:
                // Mix -> Bake -> Finish
                tools.Add(CustomerToolType.Oven);
                break;

            case ProcessFlow.CoolOnly:
                // Mix -> Cool -> Finish
                tools.Add(CustomerToolType.Fridge);
                break;

            case ProcessFlow.BakeThenCool:
                // Mix -> Bake -> Cool -> Finish
                tools.Add(CustomerToolType.Oven);
                tools.Add(CustomerToolType.Fridge);
                break;

            case ProcessFlow.CoolThenBake:
                // Mix -> Cool -> Bake -> Finish
                tools.Add(CustomerToolType.Fridge);
                tools.Add(CustomerToolType.Oven);
                break;
        }

        foreach (var tool in tools)
        {
            if (!toolIconMap.TryGetValue(tool, out var sprite) || sprite == null)
                continue;

            var obj = Instantiate(toolIconPrefab, toolsGroup);
            var img = obj.GetComponent<Image>();
            if (img != null)
                img.sprite = sprite;
        }
    }
}
