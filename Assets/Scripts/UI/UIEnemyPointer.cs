using UnityEngine;

public class UIEnemyPointer : MonoBehaviour
{
    public Transform enemyTarget;
    public RectTransform enemyIconUI;
    public RectTransform arrowUI;
    public Camera cam;

    public float edgeOffset = 60f;

    private void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void Update()
    {
        if (enemyTarget == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(enemyTarget.position);

        bool isVisible =
            screenPos.z > 0 &&
            screenPos.x > 0 && screenPos.x < Screen.width &&
            screenPos.y > 0 && screenPos.y < Screen.height;

        if (isVisible)
        {
            enemyIconUI.gameObject.SetActive(true);
            arrowUI.gameObject.SetActive(false);

            enemyIconUI.position = screenPos;
        }
        else
        {
            enemyIconUI.gameObject.SetActive(false);
            arrowUI.gameObject.SetActive(true);

            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 dir = ((Vector2)screenPos - center).normalized;

            Vector2 edgePos = center + dir * ((Screen.height / 2f) - edgeOffset);
            arrowUI.position = edgePos;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowUI.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }
    }
}
