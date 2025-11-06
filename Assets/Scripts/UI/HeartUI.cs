using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartUI : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Sprite fullHeart;

    [Header("Perf")]
    [SerializeField] private bool usePooling = false;
    [SerializeField] private int initialPoolSize = 0;

    private readonly List<GameObject> activeHearts = new();
    private readonly Stack<GameObject> pool = new();

    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (!container) container = transform;

        if (usePooling && heartPrefab != null && initialPoolSize > 0)
        {
            for (int i = 0; i < initialPoolSize; i++)
                pool.Push(CreateNewHeart(inactive: true));
        }
    }

    private void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (!playerHealth)
        {
            Debug.LogWarning("[HeartUI] PlayerHealth not found in scene.");
            return;
        }

        RebuildToMax(playerHealth.MaxHearts);
        Refresh(playerHealth.CurrentHearts, playerHealth.MaxHearts);

        playerHealth.OnHealthChanged += Refresh;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    // ===== Core =====

    public void Refresh(int current, int max)
    {
        if (activeHearts.Count > max)
        {
            int toRemove = activeHearts.Count - max;
            for (int i = 0; i < toRemove; i++)
                RemoveOneFromEnd(destroyOrPool: true);
        }
        else if (activeHearts.Count < max)
        {
            int toAdd = max - activeHearts.Count;
            for (int i = 0; i < toAdd; i++)
                AddOne();
        }

        while (activeHearts.Count > current)
            RemoveOneFromEnd(destroyOrPool: false);
        while (activeHearts.Count < current)
            AddOne();
    }

    private void RebuildToMax(int max)
    {
        ClearAll(destroyAll: !usePooling);

        for (int i = 0; i < max; i++)
            AddOne();
    }

    private void AddOne()
    {
        GameObject go;
        if (usePooling && pool.Count > 0)
        {
            go = pool.Pop();
            go.transform.SetParent(container, false);
            go.SetActive(true);
        }
        else
        {
            go = CreateNewHeart(inactive: false);
        }

        var img = go.GetComponentInChildren<Image>();
        if (img != null && fullHeart != null)
            img.sprite = fullHeart;

        activeHearts.Add(go);
    }

    private void RemoveOneFromEnd(bool destroyOrPool)
    {
        if (activeHearts.Count == 0) return;

        int lastIndex = activeHearts.Count - 1;
        GameObject go = activeHearts[lastIndex];
        activeHearts.RemoveAt(lastIndex);

        if (usePooling)
        {
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            pool.Push(go);
        }
        else
        {
            if (destroyOrPool) Destroy(go);
            else Destroy(go);
        }
    }

    private void ClearAll(bool destroyAll)
    {
        for (int i = activeHearts.Count - 1; i >= 0; i--)
        {
            var go = activeHearts[i];
            if (usePooling && !destroyAll)
            {
                go.SetActive(false);
                go.transform.SetParent(transform, false);
                pool.Push(go);
            }
            else
            {
                Destroy(go);
            }
        }
        activeHearts.Clear();
    }

    private GameObject CreateNewHeart(bool inactive)
    {
        var go = Instantiate(heartPrefab, container);
        if (inactive) go.SetActive(false);

        var img = go.GetComponentInChildren<Image>();
        if (img != null && fullHeart != null)
            img.sprite = fullHeart;

        return go;
    }
}