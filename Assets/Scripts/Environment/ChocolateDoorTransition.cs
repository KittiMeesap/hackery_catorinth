using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ChocolateDoorTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    public string transitionScene;
    public string finalScene;
    public float waitTime = 0.5f;

    [Header("Trigger Settings")]
    public string playerTag = "Player";

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;
        if (!other.CompareTag(playerTag)) return;

        isTransitioning = true;
        StartCoroutine(TransitionSequence());
    }

    private IEnumerator TransitionSequence()
    {
        yield return ScreenFader.Instance.FadeOut();

        SceneManager.LoadScene(transitionScene);

        yield return new WaitForSeconds(waitTime);

        yield return ScreenFader.Instance.FadeOut();

        SceneManager.LoadScene(finalScene);

        yield return ScreenFader.Instance.FadeIn();

        isTransitioning = false;
    }
}
