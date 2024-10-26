using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class RestaurantManager : MonoBehaviour
{
    private static RestaurantManager instance;
    public static RestaurantManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RestaurantManager>();
            }
            return instance;
        }
    }

    [SerializeField]
    private List<Seat> availableTables = new List<Seat>();
    private Queue<Order> pendingOrders = new Queue<Order>();
    private List<Order> activeOrders = new List<Order>();

    // Properties
    public int Level { get; private set; } = 1;
    public decimal Money { get; private set; } = 1000;
    public List<Staff> Staff { get; private set; } = new List<Staff>();

    // Events
    public event Action<Order> OnOrderReceived;
    public event Action<Order> OnOrderCompleted;
    public event Action<decimal> OnMoneyChanged;
    public event Action<Seat> OnTableStatusChanged;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeTables();
    }

    private void InitializeTables()
    {
        availableTables.Clear();
        availableTables.AddRange(FindObjectsOfType<Seat>());

        // Subscribe to table status changes
        foreach (var table in availableTables)
        {
            table.OnSeatStatusChanged += () => OnTableStatusChanged?.Invoke(table);
        }
    }

    // Table Management
    public Seat FindNearestAvailableTable(Vector3 position)
    {
        Seat nearestTable = null;
        float shortestDistance = float.MaxValue;

        foreach (var table in availableTables)
        {
            if (table.AvailableChair)
            {
                float distance = table.GetDistanceToSeat(position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestTable = table;
                }
            }
        }

        return nearestTable;
    }

    // Order Management
    public void SubmitOrder(CustommerBehavior customer, Dish dish)
    {
        Order newOrder = new Order
        {
            Customer = customer,
            OrderedDish = dish,
            OrderTime = Time.time
        };

        pendingOrders.Enqueue(newOrder);
        OnOrderReceived?.Invoke(newOrder);
       
    }

    public void CompleteOrder(Order order)
    {
        if (activeOrders.Contains(order))
        {
            activeOrders.Remove(order);
            OnOrderCompleted?.Invoke(order);

            // Add payment to restaurant money
            AddMoney(CalculateOrderPrice(order));
        }
    }

    // Money Management
    private void AddMoney(decimal amount)
    {
        Money += amount;
        OnMoneyChanged?.Invoke(Money);
    }

    private decimal CalculateOrderPrice(Order order)
    {
        // Implement your pricing logic here
        return 10.0m; // Placeholder price
    }

    void Update()
    {
        ProcessPendingOrders();
    }

    private void ProcessPendingOrders()
    {
        while (pendingOrders.Count > 0)
        {
            var order = pendingOrders.Dequeue();
            activeOrders.Add(order);
            // Additional processing logic here
        }
    }
    public List<Seat> GetAllAvailableSeats()
    {
        return availableTables.Where(table => table.AvailableChair).ToList();
    }

    public Seat FindBestAvailableTable(Vector3 position, float maxDistance = float.MaxValue)
    {
        var availableSeats = GetAllAvailableSeats();
        if (availableSeats.Count == 0) return null;

        return availableSeats
            .Where(seat => seat.GetDistanceToSeat(position) <= maxDistance)
            .OrderBy(seat => seat.GetDistanceToSeat(position))
            .FirstOrDefault();
    }

    public bool HasAvailableSeats()
    {
        return availableTables.Any(table => table.AvailableChair);
    }

    // Debug Methods
    public void DebugPrintStatus()
    {
        Debug.Log($"Restaurant Status:");
        Debug.Log($"Level: {Level}");
        Debug.Log($"Money: ${Money}");
        Debug.Log($"Active Tables: {availableTables.Count}");
        Debug.Log($"Pending Orders: {pendingOrders.Count}");
        Debug.Log($"Active Orders: {activeOrders.Count}");
    }
}