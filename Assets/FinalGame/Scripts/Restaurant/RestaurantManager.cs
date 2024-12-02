using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class RestaurantManager : MonoBehaviour
{
    #region Instance
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
    #endregion
    #region Properties
    public GameObject frontDoor;
    public GameObject customerDispawnPoint;
    public int Level { get; private set; } = 1;
    public decimal Money { get; private set; } = 1000;
    public List<Staff> Staff { get; private set; } = new List<Staff>();
    private int currentCustomerCount = 0;
    #endregion
    #region Private Fields
    private List<Seat> availableSeat = new List<Seat>();
    private Queue<Order> pendingOrders = new Queue<Order>();
    private List<Order> activeOrders = new List<Order>();
    #endregion
    #region Events
    public static event Action OnSeatsChanged;
    public event Action<Order> OnOrderReceived;
    public event Action<Order> OnOrderCompleted;
    public event Action<decimal> OnMoneyChanged;
    public event Action<Seat> OnTableStatusChanged;
    #endregion

    public delegate void SeatsChangedHandler();
    #region Unity Lifecycle
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
    void Start() => InitializeSeat();
    void Update()
    {
        ProcessPendingOrders();
        //DebugPrintStatus();
    }
    #endregion

    private void InitializeSeat()
    {
        availableSeat.Clear();
        availableSeat.AddRange(FindObjectsOfType<Seat>());

        // Subscribe to Seat status changes
        foreach (var table in availableSeat)
        {
            table.OnSeatStatusChanged += () => OnTableStatusChanged?.Invoke(table);
        }
    }
    public void NotifySeatsChanged()
    {
        OnSeatsChanged?.Invoke();
    }
    public void RegisterSeat(Seat seat)
    {
        if (!availableSeat.Contains(seat))
        {
            availableSeat.Add(seat);
            seat.OnSeatStatusChanged += () => OnTableStatusChanged?.Invoke(seat);
            NotifySeatsChanged();
        }
    }
    public void UnregisterSeat(Seat seat)
    {
        if (availableSeat.Contains(seat))
        {
            availableSeat.Remove(seat);
            seat.OnSeatStatusChanged -= () => OnTableStatusChanged?.Invoke(seat);
            NotifySeatsChanged();
        }
    }
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

    //Chưa làm xong
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
    //----------------------------------------------------------------
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
    private void ProcessPendingOrders()
    {
        while (pendingOrders.Count > 0)
        {
            var order = pendingOrders.Dequeue();
            activeOrders.Add(order);
            // Additional processing logic here
        }
    }
    public Seat FindNearestAvailableSeat(Vector3 position)
    {
        Seat nearestTable = null;
        float shortestDistance = float.MaxValue;

        foreach (var table in availableSeat)
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
    public bool HasAvailableSeats()
    {
        return availableSeat.Any(Seat => Seat.AvailableChair);
    }
    public List<Seat> GetAllSeats()
    {
        return availableSeat;
    }
    public void DebugPrintStatus()
    {
        Debug.Log($"Restaurant Status:");
        Debug.Log($"Level: {Level}");
        Debug.Log($"Money: ${Money}");
        Debug.Log($"Active Tables: {availableSeat.Count}");
        Debug.Log($"Pending Orders: {pendingOrders.Count}");
        Debug.Log($"Active Orders: {activeOrders.Count}");
    }
}