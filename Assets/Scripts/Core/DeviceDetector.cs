using UnityEngine;
using UnityEngine.InputSystem;

public class DeviceDetector : MonoBehaviour
{
    public static System.Action<string> OnDeviceChanged;

    private PlayerInput playerInput;
    private string currentDevice = "";

    private void Awake()
    {
        playerInput = FindFirstObjectByType<PlayerInput>();
    }

    private void OnEnable()
    {
        playerInput.onControlsChanged += OnControlsChanged;
    }

    private void OnDisable()
    {
        playerInput.onControlsChanged -= OnControlsChanged;
    }

    private void OnControlsChanged(PlayerInput input)
    {
        string newDevice = input.currentControlScheme;

        if (newDevice != currentDevice)
        {
            currentDevice = newDevice;
            Debug.Log("Device Changed" + newDevice);

            OnDeviceChanged?.Invoke(newDevice);
        }
    }
}
