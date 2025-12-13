using System.Collections.Generic;
using UnityEngine;

public class CustomerQueueManager : MonoBehaviour
{
    [System.Serializable]
    public class CustomerPrefabEntry
    {
        public string id;
        public CustomerPersonality personality;
        public GameObject prefab;
        public float weight = 1f;
    }

    [Header("Queue Points")]
    public Transform[] queuePoints;

    [Header("Spawn & Exit")]
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Wave Settings")]
    public float waveDelay = 4f;
    public int[] waveSizes = new int[] { 2, 3, 2 };

    [Header("Spawn Settings")]
    public float spawnInterval = 1.5f;
    public int poolSize = 10;

    [Header("Customer Prefabs")]
    public List<CustomerPrefabEntry> customerPrefabs = new();

    private float spawnTimer;
    private float waveTimer;
    private int currentWave = 0;
    private int waveSpawnedCount = 0;

    private readonly List<CustomerController> pool = new();
    private readonly List<CustomerController> activeCustomers = new();

    private void Start()
    {
        spawnTimer = spawnInterval;
        waveTimer = waveDelay;
        CreatePool();
    }

    private void Update()
    {
        if (currentWave >= waveSizes.Length)
            return;

        if (waveTimer > 0f)
        {
            waveTimer -= Time.deltaTime;
            return;
        }

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            TrySpawnCustomer();
            spawnTimer = spawnInterval;
        }
    }

    // POOL
    private void CreatePool()
    {
        if (customerPrefabs.Count == 0)
        {
            Debug.LogError("CustomerQueueManager: No NPC Prefabs!");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            var entry = customerPrefabs[i % customerPrefabs.Count];
            if (entry == null || entry.prefab == null) continue;

            var obj = Instantiate(entry.prefab);
            obj.SetActive(false);

            var ctrl = obj.GetComponent<CustomerController>();
            if (ctrl == null)
            {
                Debug.LogError("CustomerQueueManager: Prefab didn't have CustomerController!");
                Destroy(obj);
                continue;
            }

            ctrl.queueManager = this;
            ctrl.prefabId = entry.id;
            ctrl.personality = entry.personality;

            pool.Add(ctrl);
        }
    }

    private CustomerController GetCustomerFromPool(CustomerPrefabEntry entry)
    {
        // Try find matching prefab id in pool
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) { pool.RemoveAt(i); i--; continue; }

            if (!string.IsNullOrEmpty(entry.id) && c.prefabId == entry.id)
            {
                pool.RemoveAt(i);
                c.gameObject.SetActive(true);

                c.queueManager = this;
                c.personality = entry.personality;
                c.prefabId = entry.id;
                return c;
            }
        }

        // Otherwise instantiate correct prefab
        var obj = Instantiate(entry.prefab);
        var ctrl = obj.GetComponent<CustomerController>();
        if (ctrl == null)
        {
            Debug.LogError("CustomerQueueManager: Spawned prefab missing CustomerController!");
            Destroy(obj);
            return null;
        }

        ctrl.queueManager = this;
        ctrl.personality = entry.personality;
        ctrl.prefabId = entry.id;
        return ctrl;
    }

    private void ReturnToPool(CustomerController ctrl)
    {
        if (ctrl == null) return;

        ctrl.gameObject.SetActive(false);
        pool.Add(ctrl);
    }

    // SPAWN LOGIC
    private void TrySpawnCustomer()
    {
        if (currentWave >= waveSizes.Length) return;

        int waveLimit = waveSizes[currentWave];
        if (waveSpawnedCount >= waveLimit)
        {
            currentWave++;
            waveSpawnedCount = 0;
            waveTimer = waveDelay;
            return;
        }

        int freeIndex = GetFirstFreeQueueIndex();
        if (freeIndex == -1) return;

        var entry = PickRandomCustomerEntry();
        if (entry == null || entry.prefab == null) return;

        // Must have recipe to spawn safely
        RecipeSO recipe = null;
        if (RecipeManager.Instance != null && RecipeManager.Instance.recipes != null && RecipeManager.Instance.recipes.Count > 0)
        {
            int idx = Random.Range(0, RecipeManager.Instance.recipes.Count);
            recipe = RecipeManager.Instance.recipes[idx];
        }

        if (recipe == null)
        {
            Debug.LogWarning("CustomerQueueManager: No recipe available, skipping spawn.");
            return;
        }

        var ctrl = GetCustomerFromPool(entry);
        if (ctrl == null) return;

        Vector3 spawnPos = spawnPoint ? spawnPoint.position : queuePoints[freeIndex].position;
        ctrl.transform.position = spawnPos;

        ctrl.Initialize(recipe, ctrl.customerFaceIcon, queuePoints[freeIndex].position, freeIndex);

        activeCustomers.Add(ctrl);
        waveSpawnedCount++;
    }

    private int GetFirstFreeQueueIndex()
    {
        if (queuePoints == null || queuePoints.Length == 0) return -1;

        bool[] used = new bool[queuePoints.Length];

        foreach (var c in activeCustomers)
        {
            if (c == null) continue;
            if (c.queueIndex >= 0 && c.queueIndex < used.Length)
                used[c.queueIndex] = true;
        }

        for (int i = 0; i < used.Length; i++)
        {
            if (!used[i])
                return i;
        }

        return -1;
    }

    private CustomerPrefabEntry PickRandomCustomerEntry()
    {
        float total = 0f;
        foreach (var e in customerPrefabs)
            total += Mathf.Max(0f, e.weight);

        if (total <= 0f) return null;

        float r = Random.value * total;
        float sum = 0f;

        foreach (var e in customerPrefabs)
        {
            sum += Mathf.Max(0f, e.weight);
            if (r <= sum) return e;
        }

        return customerPrefabs[0];
    }

    // CUSTOMER LEAVE
    public void OnCustomerLeft(CustomerController ctrl)
    {
        if (ctrl == null) return;

        activeCustomers.Remove(ctrl);
        ReturnToPool(ctrl);
        ReorderQueue();
    }

    private void ReorderQueue()
    {
        activeCustomers.RemoveAll(c => c == null);

        activeCustomers.Sort((a, b) => a.queueIndex.CompareTo(b.queueIndex));

        int limit = Mathf.Min(activeCustomers.Count, queuePoints.Length);

        for (int i = 0; i < limit; i++)
        {
            activeCustomers[i].SetQueueSlot(i, queuePoints[i].position);
        }
    }
}
