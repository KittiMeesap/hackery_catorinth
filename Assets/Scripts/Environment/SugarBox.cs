using System.Collections;
using UnityEngine;

public class SugarBox : MonoBehaviour, IHackableTarget, IHeatable
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Destroy Settings")]
    [SerializeField] private float destroyDelayAfterAnim = 0.5f;

    [Header("Audio ( key จาก SoundLibrary)")]
    [Tooltip("SoundLibrary เช่น SFX_SugarBox_Destroy")]
    [SerializeField] private string destroySoundKey = "SFX_SugarBox_Destroy";

    [Header("Heat Reaction")]
    [SerializeField] private float heatThreshold = 2f;
    [SerializeField] private float coolThreshold = 0f;

    private float heatLevel = 0f;
    private bool isDestroyed = false;

    public void OnTriggeredByComputer()
    {
        if (isDestroyed) return;
        StartCoroutine(DestroyRoutine());
    }

    public void ApplyHeat(float delta)
    {
        if (isDestroyed) return;

        heatLevel += delta;
        if (heatLevel >= heatThreshold)
        {
            Debug.Log("[SugarBox] Overheated and destroyed.");
            OnTriggeredByComputer();
        }
    }

    public void CoolDown(float delta)
    {
        if (isDestroyed) return;
        heatLevel = Mathf.Max(coolThreshold, heatLevel - delta);
    }

    private IEnumerator DestroyRoutine()
    {
        isDestroyed = true;

       
        if (animator != null)
            animator.SetTrigger("Destroy");

        
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(destroySoundKey))
            AudioManager.Instance.PlaySFX(destroySoundKey);

        yield return new WaitForSeconds(destroyDelayAfterAnim);

        Destroy(gameObject);
    }
}