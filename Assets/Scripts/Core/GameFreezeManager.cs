using System.Collections.Generic;
using UnityEngine;

public class GameFreezeManager : MonoBehaviour
{
    public static GameFreezeManager Instance;

    private List<Animator> animators = new();
    private List<Rigidbody2D> rigidbodies = new();

    private bool isFrozen = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        animators.AddRange(
            FindObjectsByType<Animator>(FindObjectsSortMode.None)
        );

        rigidbodies.AddRange(
            FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None)
        );
    }

    public void FreezeGame()
    {
        if (isFrozen) return;
        isFrozen = true;

        foreach (var a in animators)
        {
            if (a != null)
                a.speed = 0f;
        }

        foreach (var rb in rigidbodies)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }
        }
    }

    public void UnfreezeGame()
    {
        if (!isFrozen) return;
        isFrozen = false;

        foreach (var a in animators)
        {
            if (a != null)
                a.speed = 1f;
        }

        foreach (var rb in rigidbodies)
        {
            if (rb != null)
                rb.simulated = true;
        }
    }
}
