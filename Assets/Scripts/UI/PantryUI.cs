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
    public GridLayoutGroup gridLayout;
    public ScrollRect scrollRect;

    [Header("Selected Icons (max 4)")]
    public Image[] selectedSlots;

    private List<PantryButtonController> buttonControllers = new();
    private List<IngredientItemSO> pantryIngredients = new();
    private List<IngredientItemSO> selectedIngredients = new();

    private PlayerInventory currentPlayer;
    private PlayerController playerController;
    private InteractStation owner;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;

    private int currentIndex = 0;
    private int columns = 3;

    private float inputCooldown = 0.15f;
    private float inputTimer = 0f;

    private void Awake()
    {
        Instance = this;
        HideImmediate();
    }

    private void Start()
    {
        if (gridLayout != null)
            columns = Mathf.Max(1, gridLayout.constraintCount);
    }

    public void Open(IngredientItemSO[] items, PlayerInventory player, InteractStation station)
    {
        owner = station;
        currentPlayer = player;
        playerController = player.GetComponent<PlayerController>();

        if (currentPlayer.bowl == null)
            currentPlayer.PickCup();

        panelRoot.SetActive(true);

        pantryIngredients = new List<IngredientItemSO>(items);
        selectedIngredients = new List<IngredientItemSO>(currentPlayer.bowl.contents);

        RefreshSelectedIcons();
        BuildGrid();
        Show();

        var input = QTEManager.Instance.input;
        input.Player.Disable();
        input.UI.Enable();

        playerController.DisableMovement();

        navigateAction = input.UI.Navigate;
        submitAction = input.UI.Submit;
        cancelAction = input.UI.Cancel;

        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;

        FocusNearestUnlocked(0);
    }

    public void Close()
    {
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;

        var input = QTEManager.Instance.input;
        input.UI.Disable();
        input.Player.Enable();

        playerController.EnableMovement();

        Hide();
        owner.UnlockInteraction();
    }

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

    private void Update()
    {
        inputTimer -= Time.unscaledDeltaTime;
        if (navigateAction == null || inputTimer > 0f) return;

        Vector2 nav = navigateAction.ReadValue<Vector2>();

        if (Mathf.Abs(nav.x) < 0.4f && Mathf.Abs(nav.y) < 0.4f)
            return;

        inputTimer = inputCooldown;

        if (nav.y > 0.5f) MoveUp();
        else if (nav.y < -0.5f) MoveDown();
        else if (nav.x > 0.5f) MoveRight();
        else if (nav.x < -0.5f) MoveLeft();
    }

    private void MoveUp() => FocusNearestUnlocked(currentIndex - columns);
    private void MoveDown() => FocusNearestUnlocked(currentIndex + columns);
    private void MoveLeft()
    {
        if (currentIndex % columns == 0) return;
        FocusNearestUnlocked(currentIndex - 1);
    }
    private void MoveRight()
    {
        if (currentIndex % columns == columns - 1) return;
        FocusNearestUnlocked(currentIndex + 1);
    }

    private void FocusNearestUnlocked(int index)
    {
        if (buttonControllers.Count == 0) return;

        int direction = index > currentIndex ? 1 : -1;
        int i = index;

        while (i >= 0 && i < buttonControllers.Count)
        {
            if (!buttonControllers[i].IsLocked())
            {
                currentIndex = i;
                HighlightCurrent();
                ScrollToButton(i);
                return;
            }
            i += direction;
        }
    }

    private void HighlightCurrent()
    {
        for (int i = 0; i < buttonControllers.Count; i++)
        {
            var ctrl = buttonControllers[i];
            bool highlight = (i == currentIndex);
            bool selected = selectedIngredients.Contains(ctrl.Ingredient);

            ctrl.SetHighlight(highlight);
            ctrl.SetSelected(selected);
        }
    }

    private void ScrollToButton(int index)
    {
        if (scrollRect == null) return;

        RectTransform target = buttonControllers[index].GetComponent<RectTransform>();
        RectTransform content = itemGridParent.GetComponent<RectTransform>();

        Canvas.ForceUpdateCanvases();

        float viewportHeight = scrollRect.viewport.rect.height;
        float contentHeight = content.rect.height;
        float itemPosY = Mathf.Abs(target.anchoredPosition.y);

        float normalized = Mathf.Clamp01(itemPosY / (contentHeight - viewportHeight));
        scrollRect.verticalNormalizedPosition = 1f - normalized;
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (buttonControllers.Count == 0) return;

        var ctrl = buttonControllers[currentIndex];
        if (ctrl.IsLocked()) return;

        ToggleIngredient(ctrl.Ingredient);
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        currentPlayer.OnInventoryChanged?.Invoke();
        Close();
    }

    private void ToggleIngredient(IngredientItemSO item)
    {
        if (currentPlayer.bowl == null)
            currentPlayer.PickCup();

        var bowl = currentPlayer.bowl;

        // REMOVE
        if (selectedIngredients.Contains(item))
        {
            selectedIngredients.Remove(item);
            bowl.contents.Remove(item);

            if (bowl.contents.Count == 0)
                bowl.state = ContainerData.ContainerState.Empty;

            currentPlayer.OnInventoryChanged?.Invoke();
        }
        else // ADD
        {
            if (bowl.contents.Count >= 4) return;

            var allowed = RecipeManager.Instance.GetAllowedIngredients(selectedIngredients);
            if (allowed.Count > 0 && !allowed.Contains(item))
                return;

            selectedIngredients.Add(item);
            bowl.contents.Add(item);

            currentPlayer.OnInventoryChanged?.Invoke();
        }

        RefreshSelectedIcons();
        RefreshFiltering();
        HighlightCurrent();
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

    private void RefreshFiltering()
    {
        var allowed = RecipeManager.Instance.GetAllowedIngredients(selectedIngredients);
        int currentCount = currentPlayer.bowl != null ? currentPlayer.bowl.contents.Count : 0;

        for (int i = 0; i < pantryIngredients.Count; i++)
        {
            var item = pantryIngredients[i];
            var ctrl = buttonControllers[i];

            bool isSelected = selectedIngredients.Contains(item);
            bool capacityFull = currentCount >= 4;
            bool hasAllowedList = allowed.Count > 0;
            bool canPickByRecipe = !hasAllowedList || allowed.Contains(item);

            bool canPick = !isSelected && !capacityFull && canPickByRecipe;

            ctrl.SetSelected(isSelected);
            ctrl.SetLocked(!canPick && !isSelected);
        }
    }

    private void Show()
    {
        panelRoot.SetActive(true);
        StartCoroutine(ShowDelayed());
    }

    private System.Collections.IEnumerator ShowDelayed()
    {
        yield return null;
        StartCoroutine(FadeCanvas(0, 1, 0.12f));
    }

    private void Hide()
    {
        StartCoroutine(HideDelayed());
    }

    private System.Collections.IEnumerator HideDelayed()
    {
        yield return null;

        StartCoroutine(FadeCanvas(1, 0, 0.12f, () =>
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
