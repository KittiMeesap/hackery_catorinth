using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialTrigger : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup canvasGroup;     // ลาก CanvasGroup ของ TutorialTip เข้าไป
    public TMP_Text tipText;            // ลาก Text เข้าไป

    [Header("Tutorial Settings")]
    [TextArea]
    public string message = "Tutorial Message";
    public float duration = 10f;
    public float fadeTime = 0.4f;

    private bool triggered = false;
    private Coroutine routine;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;                // ทำครั้งเดียว
        if (!other.CompareTag("Player")) return;

        triggered = true;
        ShowTutorial();

        // ลบ Trigger ตัวเอง ไม่ทำงานซ้ำ
        Destroy(gameObject, duration + fadeTime + 0.2f);
    }

    private void ShowTutorial()
    {
        if (canvasGroup == null || tipText == null)
        {
            Debug.LogError("TutorialTrigger: CanvasGroup หรือ TMP Text ไม่ได้ Assign");
            return;
        }

        tipText.text = message;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(TutorialRoutine());
    }

    private IEnumerator TutorialRoutine()
    {
        canvasGroup.gameObject.SetActive(true);

        // Fade in
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / fadeTime);
            yield return null;
        }
        canvasGroup.alpha = 1;

        // รอข้อความค้าง
        yield return new WaitForSeconds(duration);

        // Fade out
        t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, t / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 0;
        canvasGroup.gameObject.SetActive(false);
    }
}
