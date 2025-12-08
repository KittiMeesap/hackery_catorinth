using UnityEngine;

public class UnlockableAppliance : MonoBehaviour
{
    [Tooltip("Unique ID that must match UnlockEntrySO.applianceID")]
    public string unlockID;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Unlock()
    {
        gameObject.SetActive(true);
    }
}
