using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class ConfirmPanelInputController : MonoBehaviour
{
    public Button confirmButton;
    public Button cancelButton;

    private InputAction submitAction;
    private InputAction cancelAction;

    private void OnEnable()
    {
        var map = GameInput.Instance.PlayerInputComponent.actions.FindActionMap("UI");
        submitAction = map.FindAction("Submit");
        cancelAction = map.FindAction("Cancel");

        submitAction.performed += OnSubmit;
        cancelAction.performed += OnCancel;

        EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
    }

    private void OnDisable()
    {
        submitAction.performed -= OnSubmit;
        cancelAction.performed -= OnCancel;
    }

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        confirmButton.onClick.Invoke();
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        cancelButton.onClick.Invoke();
    }
}
