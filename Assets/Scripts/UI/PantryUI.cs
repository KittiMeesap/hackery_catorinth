using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PantryUI : MonoBehaviour
{
    public static PantryUI Instance;

    [Header("UI Root")]
    public CanvasGroup canvasGroup;
    public GameObject panelRoot;

    [Header("Grid of Items")]
    public Transform itemGridParent;
    public GameObject itemButtonPrefab;

    [Header("Selected Icons (max 4)")]
    public Image[] selectedSlots;

    private List<PantryButtonController> buttonControllers = new();
    private List<IngredientItemSO> pantryIngredients = new();
    private List<IngredientItemSO> selectedIngredients = new();
    private PlayerInventory currentPlayer;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    private int currentIndex = 0;

    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    // ===== OPEN UI =====
    public void Open(IngredientItemSO[] ingredients, PlayerInventory player)
    {
        currentPlayer = player;

        pantryIngredients = new List<IngredientItemSO>(ingredients);
        selectedIngredients.Clear();
        RefreshSelectedIcons();

        BuildGrid();
        Show();

        var ui = QTEManager.Instance.input.UI;
        ui.Enable();

        navigateAction = ui.Navigate;
        submitAction = ui.Submit;
        cancelAction = ui.Cancel;

        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;

        FocusButton(0);
    }

    public void Close()
    {
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;

        QTEManager.Instance.input.UI.Disable();
        Hide();
    }

    // ===== BUILD GRID =====
    private void BuildGrid()
    {
        foreach (Transform c in itemGridParent)
            Destroy(c.gameObject);

        buttonControllers.Clear();

        foreach (var item in pantryIngredients)
        {
            var obj = Instantiate(itemButtonPrefab, itemGridParent);
            var ctrl = obj.GetComponent<PantryButtonController>();

            ctrl.SetIngredient(item);
            buttonControllers.Add(ctrl);
        }

        RefreshFiltering();
    }

    // ===== HIGHLIGHT + NAVIGATION =====
    private void FocusButton(int index)
    {
        if (buttonControllers.Count == 0) return;

        int direction = index > currentIndex ? 1 : -1;

        while (index >= 0 && index < buttonControllers.Count)
        {
            if (!buttonControllers[index].IsLocked())
            {
                currentIndex = index;

                for (int i = 0; i < buttonControllers.Count; i++)
                    buttonControllers[i].SetHighlight(i == currentIndex);

                return;
            }

            index += direction;
        }
    }

    private void Update()
    {
        if (navigateAction == null) return;

        Vector2 nav = navigateAction.ReadValue<Vector2>();

        if (nav.y > 0.5f) FocusButton(currentIndex - 1);
        if (nav.y < -0.5f) FocusButton(currentIndex + 1);
    }

    // ===== SELECT INGREDIENT =====
    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (buttonControllers.Count == 0) return;

        var ctrl = buttonControllers[currentIndex];
        if (ctrl.IsLocked()) return;

        ToggleIngredient(ctrl.Ingredient);
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        ApplySelectionToBowl();
        Close();
    }

    private void ToggleIngredient(IngredientItemSO item)
    {
        if (selectedIngredients.Contains(item))
            selectedIngredients.Remove(item);
        else
        {
            if (selectedIngredients.Count >= 4)
                return;
            selectedIngredients.Add(item);
        }

        RefreshSelectedIcons();
        RefreshFiltering();
    }

    private void RefreshSelectedIcons()
    {
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (i < selectedIngredients.Count)
            {
                selectedSlots[i].enabled = true;
                selectedSlots[i].sprite = selectedIngredients[i].icon;
            }
            else
                selectedSlots[i].enabled = false;
        }
    }

    // ===== FILTER ACCORDING TO RECIPE =====
    private void RefreshFiltering()
    {
        var allowed = RecipeManager.Instance.GetAllowedIngredients(selectedIngredients);

        for (int i = 0; i < pantryIngredients.Count; i++)
        {
            var item = pantryIngredients[i];
            var ctrl = buttonControllers[i];

            bool isSelected = selectedIngredients.Contains(item);
            bool canPick = allowed.Contains(item);

            ctrl.SetSelected(isSelected);
            ctrl.SetLocked(!isSelected && !canPick);
        }
    }

    // ===== APPLY SELECTED INGREDIENTS =====
    private void ApplySelectionToBowl()
    {
        if (currentPlayer.bowl == null)
            return;

        foreach (var ing in selectedIngredients)
            currentPlayer.bowl.AddIngredient(ing);

        currentPlayer.OnInventoryChanged?.Invoke();
    }

    // ===== SHOW / HIDE =====
    private void Show()
    {
        panelRoot.SetActive(true);
        StartCoroutine(FadeCanvas(0, 1, 0.15f));
    }

    private void Hide()
    {
        StartCoroutine(FadeCanvas(1, 0, 0.15f, () =>
        {
            panelRoot.SetActive(false);
        }));
    }

    private void HideImmediate()
    {
        panelRoot.SetActive(false);
        canvasGroup.alpha = 0;
    }

    private System.Collections.IEnumerator FadeCanvas(float a, float b, float time, System.Action onDone = null)
    {
        float t = 0;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(a, b, t / time);
            yield return null;
        }

        canvasGroup.alpha = b;
        onDone?.Invoke();
    }
}
