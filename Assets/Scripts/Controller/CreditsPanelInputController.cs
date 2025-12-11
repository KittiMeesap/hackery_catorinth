using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class CreditsPanelInputController : MonoBehaviour
{
    private InputAction cancelAction;

    private void OnEnable()
    {
        var map = GameInput.Instance.PlayerInputComponent.actions.FindActionMap("UI");
        cancelAction = map.FindAction("Cancel");

        cancelAction.performed += OnCancel;

        EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnDisable()
    {
        if (cancelAction != null)
            cancelAction.performed -= OnCancel;
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        var main = FindFirstObjectByType<MainMenu>();
        if (main != null)
        {
            main.SetUILocked(false);
            main.ShowMainMenu();
        }

        gameObject.SetActive(false);
    }
}
