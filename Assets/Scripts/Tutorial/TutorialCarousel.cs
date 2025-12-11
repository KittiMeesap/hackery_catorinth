using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TutorialCarousel : MonoBehaviour
{
    [Header("Scroll View")]
    public ScrollRect scrollRect;
    public RectTransform content;

    [Header("Panels")]
    public RectTransform[] panels;

    [Header("Settings")]
    public float spacing = 700f;
    public float moveSpeed = 12f;

    // ================================
    // ?? PAGE DOTS SUPPORT
    // ================================
    [Header("Page Dots")]
    public Image[] pageDots;          
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(1, 1, 1, 0.3f);
    // ================================

    private int currentIndex = 0;
    private Vector2 targetPos;

    private InputAction navigate;
    private InputAction submit;

    private bool inputReady = false;

    private void Awake()
    {
        scrollRect.horizontal = false;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;
    }

    private void OnEnable()
    {
        GameInput.Instance.SetModeUI();

        var ui = GameInput.Instance.PlayerInputComponent.actions.FindActionMap("UI");

        navigate = ui.FindAction("Navigate");
        submit = ui.FindAction("Submit");

        navigate.performed += OnNavigate;
        submit.performed += OnSubmit;

        PositionPanels();

        SnapToIndex(0, true);

        
        UpdatePageDots(0);

        Invoke(nameof(ActivateInput), 0.1f);
    }

    private void OnDisable()
    {
        navigate.performed -= OnNavigate;
        submit.performed -= OnSubmit;
    }

    private void ActivateInput()
    {
        inputReady = true;
    }

    private void Update()
    {
        content.anchoredPosition = Vector2.Lerp(
            content.anchoredPosition,
            targetPos,
            Time.deltaTime * moveSpeed
        );
    }

    // ------------------------------------------------------------

    private void PositionPanels()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].anchoredPosition = new Vector2(i * spacing, 0);
        }
    }

    private void OnNavigate(InputAction.CallbackContext ctx)
    {
        if (!inputReady) return;

        Vector2 nav = ctx.ReadValue<Vector2>();

        if (nav.x > 0.5f) Move(+1);
        else if (nav.x < -0.5f) Move(-1);
    }

    private void Move(int dir)
    {
        currentIndex += dir;

        if (currentIndex < 0) currentIndex = panels.Length - 1;
        if (currentIndex >= panels.Length) currentIndex = 0;

        SnapToIndex(currentIndex);
    }

    private void SnapToIndex(int index, bool instant = false)
    {
        currentIndex = index;
        targetPos = new Vector2(-index * spacing, 0);

        if (instant)
            content.anchoredPosition = targetPos;

        
        UpdatePageDots(index);
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (!inputReady) return;

        Debug.Log("Selected Tutorial Page = " + currentIndex);
    }

    // ================================
    // ?? PAGE DOT UPDATE FUNCTION
    // ================================
    private void UpdatePageDots(int index)
    {
        if (pageDots == null || pageDots.Length == 0) return;

        for (int i = 0; i < pageDots.Length; i++)
        {
            pageDots[i].color = (i == index) ? activeColor : inactiveColor;
        }
    }
}
