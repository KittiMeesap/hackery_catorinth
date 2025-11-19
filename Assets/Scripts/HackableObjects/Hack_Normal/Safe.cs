using System.Collections;
using UnityEngine;

public class Safe : HackableObject
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openParam = "isOpen";

    [Header("Paper FX")]
    [SerializeField] private GameObject paperPrefab;
    [SerializeField] private Transform paperSpawnPoint;
    [SerializeField] private float flyDistance = 2f;
    [SerializeField] private float flyDuration = 1.5f;
    [SerializeField] private float spinSpeed = 720f;
    [SerializeField] private float disappearDelay = 0.3f;

    [Header("Audio")]
    [SerializeField] private string sfxOpenKey = "SFX_SafeOpen";
    [SerializeField] private string sfxPaperKey = "SFX_PaperFly";

    private bool isOpened = false;
    private bool isAnimating = false;

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    public override void OnEnterHackingMode()
    {
        base.OnEnterHackingMode();
    }

    protected override void HandleHackOptionComplete(HackOptionSO option)
    {
        base.HandleHackOptionComplete(option);

        if (isOpened || isAnimating) return;

        StartCoroutine(OpenSafeSequence());
    }

    private IEnumerator OpenSafeSequence()
    {
        isAnimating = true;

        if (animator)
            animator.SetBool(openParam, true);

        if (!string.IsNullOrEmpty(sfxOpenKey))
            AudioManager.Instance?.PlaySFX(sfxOpenKey);

        yield return new WaitForSeconds(0.2f);

        if (paperPrefab && paperSpawnPoint)
        {
            GameObject paper = Instantiate(paperPrefab, paperSpawnPoint.position, Quaternion.identity);
            StartCoroutine(PaperFlyRoutine(paper));
        }

        isOpened = true;

        yield return new WaitForSeconds(flyDuration + disappearDelay + 0.2f);
        isAnimating = false;
    }

    private IEnumerator PaperFlyRoutine(GameObject paper)
    {
        if (!paper) yield break;

        Vector3 startPos = paper.transform.position;
        Vector3 targetPos = startPos + Vector3.up * flyDistance;
        float elapsed = 0f;

        if (!string.IsNullOrEmpty(sfxPaperKey))
            AudioManager.Instance?.PlaySFX(sfxPaperKey);

        while (elapsed < flyDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flyDuration;

            paper.transform.position = Vector3.Lerp(startPos, targetPos, t);
            paper.transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime);

            yield return null;
        }

        yield return new WaitForSeconds(disappearDelay);

        var sr = paper.GetComponent<SpriteRenderer>();
        if (sr)
        {
            float fadeTime = 0.3f;
            float fade = 1f;

            while (fade > 0f)
            {
                fade -= Time.deltaTime / fadeTime;
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, fade);
                yield return null;
            }
        }

        Destroy(paper);
    }
}
