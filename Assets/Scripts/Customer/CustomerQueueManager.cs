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

    [Header("Spawn Clamp")]
    public float minSpawnInterval = 1.2f;
    public float maxSpawnInterval = 5f;

    [Header("Customer Prefabs")]
    public List<CustomerPrefabEntry> customerPrefabs = new();

    private readonly List<CustomerController> pool = new();
    private readonly List<CustomerController> activeCustomers = new();

    private int requiredCustomers;
    private int spawnedToday;
    private float spawnInterval;
    private float spawnTimer;

    private float adaptiveSpawnMultiplier = 1f;
    private float adaptiveVipBonus = 0f;

    private void Start()
    {
        CreatePool();
    }

    private void Update()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameState.Playing)
            return;

        if (spawnedToday >= requiredCustomers)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        TrySpawnCustomer();
        spawnTimer = spawnInterval * adaptiveSpawnMultiplier;
    }

    // DAY SETUP
    public void SetupForDay(int targetOrders, int dayIndex, float dayHours)
    {
        spawnedToday = 0;
        activeCustomers.Clear();

        requiredCustomers =
            Mathf.CeilToInt(targetOrders / 0.7f);

        requiredCustomers =
            Mathf.CeilToInt(requiredCustomers * (1f + dayIndex * 0.08f));

        float totalSeconds = dayHours * 3600f;

        spawnInterval = Mathf.Clamp(
            totalSeconds / requiredCustomers,
            minSpawnInterval,
            maxSpawnInterval
        );

        spawnTimer = spawnInterval;
        adaptiveSpawnMultiplier = 1f;
        adaptiveVipBonus = 0f;

        Debug.Log(
            $"[Day {dayIndex}] Orders:{targetOrders} " +
            $"Customers:{requiredCustomers} Interval:{spawnInterval:0.00}s"
        );
    }

    // ADAPTIVE DIFFICULTY
    public void ApplyAdaptiveDifficulty(
        float playerOrdersPerHour,
        float requiredOrdersPerHour)
    {
        float diff = playerOrdersPerHour - requiredOrdersPerHour;

        adaptiveSpawnMultiplier = Mathf.Clamp(
            1f - diff * 0.4f,
            0.75f,
            1.25f
        );

        adaptiveVipBonus = Mathf.Clamp(
            diff * 0.25f,
            -0.15f,
            0.25f
        );
    }

    // SPAWN
    private void TrySpawnCustomer()
    {
        int index = GetFirstFreeQueueIndex();
        if (index == -1) return;

        var recipe = GetRandomRecipe();
        if (recipe == null) return;

        var entry = PickCustomer();
        if (entry == null) return;

        var ctrl = GetFromPool(entry);
        ctrl.transform.position =
            spawnPoint ? spawnPoint.position : queuePoints[index].position;

        ctrl.Initialize(
            recipe,
            ctrl.customerFaceIcon,
            queuePoints[index].position,
            index
        );

        activeCustomers.Add(ctrl);
        spawnedToday++;

        GameManager.Instance.NotifyCustomerSpawned();
    }

    private CustomerPrefabEntry PickCustomer()
    {
        var day = GameManager.Instance.CurrentDayConfig;

        float vipChance = Mathf.Clamp01(
            day.vipSpawnChance + adaptiveVipBonus
        );

        bool wantVIP = Random.value < vipChance;

        float total = 0f;
        foreach (var e in customerPrefabs)
        {
            if (wantVIP && e.personality != CustomerPersonality.VIP) continue;
            if (!wantVIP && e.personality == CustomerPersonality.VIP) continue;
            total += e.weight;
        }

        float r = Random.value * total;
        float sum = 0f;

        foreach (var e in customerPrefabs)
        {
            if (wantVIP && e.personality != CustomerPersonality.VIP) continue;
            if (!wantVIP && e.personality == CustomerPersonality.VIP) continue;

            sum += e.weight;
            if (r <= sum) return e;
        }

        return null;
    }

    // POOL & QUEUE
    private void CreatePool()
    {
        for (int i = 0; i < 10; i++)
        {
            var entry = customerPrefabs[i % customerPrefabs.Count];
            var obj = Instantiate(entry.prefab);
            obj.SetActive(false);

            var ctrl = obj.GetComponent<CustomerController>();
            ctrl.queueManager = this;
            ctrl.prefabId = entry.id;
            ctrl.personality = entry.personality;

            pool.Add(ctrl);
        }
    }

    private CustomerController GetFromPool(CustomerPrefabEntry entry)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].prefabId == entry.id)
            {
                var c = pool[i];
                pool.RemoveAt(i);
                c.gameObject.SetActive(true);
                c.enabled = true;
                return c;
            }
        }

        var obj = Instantiate(entry.prefab);
        var ctrl = obj.GetComponent<CustomerController>();
        ctrl.queueManager = this;
        ctrl.prefabId = entry.id;
        ctrl.personality = entry.personality;
        return ctrl;
    }

    public void OnCustomerLeft(CustomerController ctrl)
    {
        activeCustomers.Remove(ctrl);
        ctrl.gameObject.SetActive(false);
        pool.Add(ctrl);
        ReorderQueue();
    }

    private void ReorderQueue()
    {
        activeCustomers.Sort((a, b) => a.queueIndex.CompareTo(b.queueIndex));
        for (int i = 0; i < activeCustomers.Count && i < queuePoints.Length; i++)
            activeCustomers[i].SetQueueSlot(i, queuePoints[i].position);
    }

    private int GetFirstFreeQueueIndex()
    {
        bool[] used = new bool[queuePoints.Length];
        foreach (var c in activeCustomers)
            used[c.queueIndex] = true;

        for (int i = 0; i < used.Length; i++)
            if (!used[i]) return i;

        return -1;
    }

    private RecipeSO GetRandomRecipe()
    {
        if (RecipeManager.Instance == null ||
            RecipeManager.Instance.recipes.Count == 0)
            return null;

        return RecipeManager.Instance.recipes[
            Random.Range(0, RecipeManager.Instance.recipes.Count)
        ];
    }

    public void FreezeAllCustomers()
    {
        foreach (var c in activeCustomers)
        {
            if (c == null) continue;
            c.Freeze();
        }
    }

    public void ClearAllCustomers()
    {
        foreach (var c in activeCustomers)
        {
            if (c == null) continue;

            c.ResetForPool();
            c.gameObject.SetActive(false);
            pool.Add(c);
        }

        activeCustomers.Clear();
    }

}
