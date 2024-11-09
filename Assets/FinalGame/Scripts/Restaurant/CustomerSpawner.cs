using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpeedUpgrade
    {
        public int level;
        public float spawnTimeReduction;
        public int upgradeCost;
    }

    [System.Serializable]
    public class CustomerPrefabData
    {
        public GameObject prefab;
        [Range(0, 100)]
        public float spawnWeight = 25f; // Tỷ lệ xuất hiện của prefab này (tổng weight nên = 100)
    }

    [Header("Customer Prefabs")]
    [SerializeField] private List<CustomerPrefabData> customerPrefabs;
    [SerializeField] private bool useSpawnWeights = true; // Toggle để bật/tắt hệ thống weight

    [Header("Base Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float baseSpawnTime = 5f;

    [Header("Upgrade Settings")]
    [SerializeField] private List<SpeedUpgrade> speedUpgrades;

    private float currentSpawnTime;
    private float spawnTimer;
    private bool isSpawning = true;
    private RestaurantManager restaurantManager;
    private int currentSpeedLevel = 1;

    // Cache cho hệ thống weight
    private float totalWeight;
    private List<float> cumulativeWeights;

    private void Start()
    {
        restaurantManager = RestaurantManager.Instance;
        UpdateSpawnTime();
        InitializeWeights();
        ValidateCustomerPrefabs();
        StartCoroutine(SpawnRoutine());
    }

    private void ValidateCustomerPrefabs()
    {
        if (customerPrefabs == null || customerPrefabs.Count == 0)
        {
            Debug.LogError("No customer prefabs assigned to CustomerSpawner!");
            enabled = false;
            return;
        }

        // Kiểm tra prefab nào bị null
        for (int i = 0; i < customerPrefabs.Count; i++)
        {
            if (customerPrefabs[i].prefab == null)
            {
                Debug.LogError($"Customer prefab at index {i} is null!");
            }
        }
    }

    private void InitializeWeights()
    {
        if (!useSpawnWeights)
        {
            return;
        }

        totalWeight = 0f;
        cumulativeWeights = new List<float>();

        foreach (var prefabData in customerPrefabs)
        {
            totalWeight += prefabData.spawnWeight;
            cumulativeWeights.Add(totalWeight);
        }

        // Kiểm tra và chuẩn hóa weights nếu tổng không bằng 100
        if (Mathf.Abs(totalWeight - 100f) > 0.01f)
        {
            Debug.LogWarning($"Total spawn weights ({totalWeight}) != 100. Weights will be normalized.");
            NormalizeWeights();
        }
    }

    private void NormalizeWeights()
    {
        float factor = 100f / totalWeight;
        totalWeight = 0f;
        cumulativeWeights.Clear();

        for (int i = 0; i < customerPrefabs.Count; i++)
        {
            customerPrefabs[i].spawnWeight *= factor;
            totalWeight += customerPrefabs[i].spawnWeight;
            cumulativeWeights.Add(totalWeight);
        }
    }

    private GameObject GetRandomCustomerPrefab()
    {
        if (customerPrefabs.Count == 0) return null;

        if (!useSpawnWeights)
        {
            // Simple random selection if not using weights
            return customerPrefabs[Random.Range(0, customerPrefabs.Count)].prefab;
        }

        // Weight-based random selection
        float random = Random.Range(0f, totalWeight);
        for (int i = 0; i < cumulativeWeights.Count; i++)
        {
            if (random <= cumulativeWeights[i])
            {
                return customerPrefabs[i].prefab;
            }
        }

        // Fallback to last prefab if something goes wrong
        return customerPrefabs[customerPrefabs.Count - 1].prefab;
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (restaurantManager.HasAvailableSeats())
            {
                TrySpawnCustomer();
                yield return new WaitForSeconds(currentSpawnTime);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private void TrySpawnCustomer()
    {
        Seat availableSeat = restaurantManager.FindNearestAvailableTable(spawnPoint.position);

        if (availableSeat != null)
        {
            SpawnCustomer();
        }
    }

    private void SpawnCustomer()
    {
        GameObject prefabToSpawn = GetRandomCustomerPrefab();
        if (prefabToSpawn != null && spawnPoint != null)
        {
            GameObject customer = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

            CustommerBehavior behavior = customer.GetComponent<CustommerBehavior>();
            if (behavior != null)
            {
                behavior.ResetState();
            }
        }
    }

    private void UpdateSpawnTime()
    {
        float reductionPercent = GetCurrentSpeedReduction() / 100f;
        currentSpawnTime = baseSpawnTime * (1f - reductionPercent);
        currentSpawnTime = Mathf.Max(currentSpawnTime, 0.5f);
    }

    private float GetCurrentSpeedReduction()
    {
        var upgrade = speedUpgrades.Find(u => u.level == currentSpeedLevel);
        return upgrade?.spawnTimeReduction ?? 0f;
    }

    #region Upgrade System
    public bool CanUpgradeSpeed()
    {
        var nextUpgrade = speedUpgrades.Find(u => u.level == currentSpeedLevel + 1);
        if (nextUpgrade == null) return false;

        return restaurantManager.Money >= nextUpgrade.upgradeCost;
    }

    public bool TryUpgradeSpeed()
    {
        if (!CanUpgradeSpeed()) return false;

        var nextUpgrade = speedUpgrades.Find(u => u.level == currentSpeedLevel + 1);
        if (nextUpgrade == null) return false;

        decimal cost = nextUpgrade.upgradeCost;
        if (restaurantManager.Money >= cost)
        {
            currentSpeedLevel++;
            UpdateSpawnTime();
            return true;
        }

        return false;
    }
    #endregion

    #region Debug Methods
    [ContextMenu("Debug Spawn All Customers")]
    private void DebugSpawnAllCustomers()
    {
        foreach (var prefabData in customerPrefabs)
        {
            if (prefabData.prefab != null)
            {
                GameObject customer = Instantiate(prefabData.prefab, spawnPoint.position, spawnPoint.rotation);
                Debug.Log($"Spawned customer: {prefabData.prefab.name}");
            }
        }
    }

    [ContextMenu("Debug Print Weights")]
    private void DebugPrintWeights()
    {
        for (int i = 0; i < customerPrefabs.Count; i++)
        {
            Debug.Log($"Customer {i}: Weight = {customerPrefabs[i].spawnWeight}, " +
                     $"Cumulative = {cumulativeWeights[i]}");
        }
    }
    #endregion
}