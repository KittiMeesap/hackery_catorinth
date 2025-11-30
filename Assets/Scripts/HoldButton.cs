using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Hold Settings")]
    public float initialDelay = 0.3f;
    public float repeatRate = 0.05f;

    private bool isHolding = false;
    private float timer = 0f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        isHolding = false;
        timer = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopHold();
    }

    private void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == gameObject &&
            (Keyboard.current?.enterKey.isPressed == true ||
             Keyboard.current?.spaceKey.isPressed == true ||
             Gamepad.current?.buttonSouth.isPressed == true))
        {
            StartHold();
        }
        else if (isHolding && !MouseIsDown())
        {
            StopHold();
        }

        if (!isHolding) return;

        timer += Time.unscaledDeltaTime;

        if (timer >= initialDelay)
        {
            button.onClick.Invoke();
            timer -= repeatRate;
        }
    }

    private bool MouseIsDown()
    {
        return Input.GetMouseButton(0);
    }

    private void StartHold()
    {
        if (!isHolding)
        {
            isHolding = true;
            timer = 0f;
            button.onClick.Invoke();
        }
    }

    private void StopHold()
    {
        isHolding = false;
    }
}
