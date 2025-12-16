using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EndDemoUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject mainButton;
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);

    [Header("SFX Keys")]
    public string clickKey = "UI_Click";
    public string hoverKey = "UI_Hover";

    private GameObject lastSelected;

    // INPUT
    private InputAction submitAction;

    private void OnEnable()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.SetModeUI();

            var map = GameInput.Instance.PlayerInputComponent.actions.FindActionMap("UI");
            submitAction = map.FindAction("Submit");
            submitAction.performed += OnSubmitPressed;
        }

        if (EventSystem.current != null && mainButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(mainButton);
        }
    }

    private void OnDisable()
    {
        if (submitAction != null)
            submitAction.performed -= OnSubmitPressed;
    }

    private void Update()
    {
        if (EventSystem.current == null) return;

        var current = EventSystem.current.currentSelectedGameObject;

        if (current != lastSelected)
        {
            if (lastSelected != null)
                lastSelected.transform.localScale = Vector3.one;

            if (current != null)
            {
                current.transform.localScale = hoverScale;
                PlayHover();
            }

            lastSelected = current;
        }
    }

    // =====================================================
    // SUBMIT (ENTER)
    // =====================================================
    private void OnSubmitPressed(InputAction.CallbackContext ctx)
    {
       
        if (EventSystem.current.currentSelectedGameObject != mainButton)
            return;

        OnMainMenuPressed();
    }

    // =====================================================
    // BUTTON ACTION
    // =====================================================
    public void OnMainMenuPressed()
    {
        PlayClick();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void PlayHover() => AudioManager.Instance?.PlaySFX(hoverKey);
    private void PlayClick() => AudioManager.Instance?.PlaySFX(clickKey);
}
