using System.Collections.Generic;
using UnityEngine;

public class ChefBehavior : MonoBehaviour
{
    #region SerializeFields
    [Header("Cooking Settings")]
    [SerializeField] private float baseCookingTime = 5f;
    [SerializeField] private int maxSimultaneousOrders = 2;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    #endregion

    #region Private Fields
    private Queue<Order> _pendingOrders = new Queue<Order>();
    private List<Order> _activeOrders = new List<Order>();
    private Dictionary<Dish, float> _cookingTimes = new Dictionary<Dish, float>();
    #endregion

    #region Animation Parameters
    private static readonly int IsCooking = Animator.StringToHash("IsCooking");
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeCookingTimes();
        RestaurantManager.Instance.OnOrderReceived += HandleNewOrder;
    }

    private void Update()
    {
        ProcessOrders();
        UpdateAnimations();
    }

    private void OnDestroy()
    {
        if (RestaurantManager.Instance != null)
        {
            RestaurantManager.Instance.OnOrderReceived -= HandleNewOrder;
        }
    }
    #endregion

    #region Order Processing
    private void InitializeCookingTimes()
    {
        // Set cooking times for different dishes
        foreach (Dish dish in System.Enum.GetValues(typeof(Dish)))
        {
            _cookingTimes[dish] = baseCookingTime;
        }
    }

    private void HandleNewOrder(Order order)
    {
        _pendingOrders.Enqueue(order);
    }

    private void ProcessOrders()
    {
        // Start cooking new orders if capacity allows
        while (_activeOrders.Count < maxSimultaneousOrders && _pendingOrders.Count > 0)
        {
            Order nextOrder = _pendingOrders.Dequeue();
            StartCookingOrder(nextOrder);
        }

        // Process active orders
        for (int i = _activeOrders.Count - 1; i >= 0; i--)
        {
            Order order = _activeOrders[i];
            if (order.IsCompleted)
            {
                CompleteOrder(order);
                _activeOrders.RemoveAt(i);
            }
        }
    }

    private void StartCookingOrder(Order order)
    {
        _activeOrders.Add(order);
        StartCoroutine(CookOrder(order));
    }

    private System.Collections.IEnumerator CookOrder(Order order)
    {
        float cookingTime = _cookingTimes[order.OrderedDish];
        yield return new WaitForSeconds(cookingTime);

        order.IsCompleted = true;
        RestaurantManager.Instance.CompleteOrder(order);
    }

    private void CompleteOrder(Order order)
    {
        // Notify restaurant manager or other systems
        Debug.Log($"Completed cooking order: {order.OrderedDish}");
    }
    #endregion

    #region Animation
    private void UpdateAnimations()
    {
        animator.SetBool(IsCooking, _activeOrders.Count > 0);
    }
    #endregion

    #region Helper Methods
    public bool HasCapacityForNewOrders()
    {
        return _activeOrders.Count < maxSimultaneousOrders;
    }

    public int GetQueueLength()
    {
        return _pendingOrders.Count + _activeOrders.Count;
    }
    #endregion
}
