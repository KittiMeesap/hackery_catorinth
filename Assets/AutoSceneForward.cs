using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AutoSceneForward : MonoBehaviour
{
    [SerializeField] private string nextScene;
    [SerializeField] private float delay = 0.5f;

    private void Start()
    {
        StartCoroutine(Forward());
    }

    private IEnumerator Forward()
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(nextScene);
    }
}
