using UnityEngine;

public class PhoneManager : MonoBehaviour
{
    public static PhoneManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // Remove battery-related methods if not needed.
    public bool CanHack()
    {
        // Can be updated if you plan to add some other condition
        return true;
    }

    public void UseBattery(int cost)
    {
        // No longer needed, but can be kept if other uses for battery remain
    }
}
