using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;
using System.Linq;

public class WaiterBehavior : MonoBehaviour
{
    #region Constants
    private const float ARRIVAL_THRESHOLD = 1.5f;
    private const float MOVEMENT_THRESHOLD = 0.1f;
    private const float ORDER_TAKING_TIME = 1f;
    #endregion

    #region SerializeFields
    [Header("Navigation")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationBlendSpeed = 0.1f;

    [Header("Positions")]
    [SerializeField] private Transform kitchenCounterPosition;

    [SerializeField] private Transform waitingPosition;
    private Seat _currentSeat;
    #endregion
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsWalkingWithFood = Animator.StringToHash("IsWalkingWithFood");
    private static readonly int MovementSpeed = Animator.StringToHash("MovementSpeed");
    #region State Management
    public enum StaffState
    {
        Idle,

        MovingToCustomer,
        TakingOrder,
        MovingToKitchenCounter,
        WaitingForFood,
        DeliveringFood,
        ReturningToWaitPosition
    }

    public StaffState CurrentState { get; private set; } = StaffState.Idle;
    #endregion

    #region Private Fields
    private BehaviorTree _tree;
    private Order _currentOrder;

    private float _currentMovementBlend;
    private float _orderTakingTimer = 0;
    private bool _hasDeliveredFood;
    #endregion

    #region Unity Lifecycle
    public void Awake()
    {
        InitializeComponents();
    }

    public void Start()
    {
        InitializeBehaviorTree();
        SubscribeToEvents();
    }

    public void Update()
    {
        UpdateAnimations();
        _tree.Update();
    }

    public void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        if (RestaurantManager.Instance == null)
        {
            enabled = false;
            return;
        }
    }

    private void InitializeBehaviorTree()
    {
        Node rootNode = CreateBehaviorTree();
        _tree = new BehaviorTree(rootNode);
    }

    private Node CreateBehaviorTree()
    {
        return new Selector(new List<Node>
        {
            CreateOrderHandlingSequence(),
            new Leaf(ReturnToWaitPosition)
        });
    }

    private Node CreateOrderHandlingSequence()
    {
        // Sửa lại logic
        //return new Sequence(new List<Node>
        //{
        //    new Leaf(FindNewOrder),
        //    new Leaf(MoveToCustomer),
        //    new Leaf(TakeOrder),
        //    new Leaf(MoveToKitchenCounter),
        //    new Leaf(WaitForMultipleFood),
        //    new Leaf(DeliverMultipleFood)
        //});
        return new Sequence(new List<Node>
        {
            new Leaf(FindSeatWithCustomer),      // Tìm ghế có khách
            new Leaf(MoveToCustomerSeat),        // Di chuyển đến ghế có khách
            new Leaf(TakeOrder),                 // Lấy order
            new Leaf(CheckForMoreCustomerOrders),                 // Lấy order
            new Leaf(MoveToKitchenCounter),      // Di chuyển đến quầy bếp
            new Leaf(WaitForMultipleFood),               // Chờ thức ăn
            new Leaf(DeliverMultipleFood)                // Phục vụ thức ăn
        });
    }

    private void SubscribeToEvents()
    {
        if (RestaurantManager.Instance != null)
        {

            RestaurantManager.Instance.OnOrderReceived += HandleNewOrder;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (RestaurantManager.Instance != null)
        {
            RestaurantManager.Instance.OnOrderReceived -= HandleNewOrder;
        }
    }
    #endregion

    #region Event Handlers
    private void HandleNewOrder(Order order)
    {
        if (CurrentState == StaffState.Idle)
        {
            _currentOrder = order;
            TransitionToState(StaffState.MovingToCustomer);

        }
    }
    #endregion

    #region Navigation
    private Node.NodeState MoveTo(Vector3 destination)
    {
        float distanceToTarget = Vector3.Distance(transform.position, destination);
        if (!agent.hasPath)
        {
            Debug.Log($"{agent.name}{agent.hasPath}");
            agent.SetDestination(destination);
            return Node.NodeState.RUNNING;
        }

        if (distanceToTarget < ARRIVAL_THRESHOLD)
        {
            Debug.Log($"{agent.name}{agent.hasPath}");
            agent.ResetPath();
            return Node.NodeState.SUCCESS;
        }
        return Node.NodeState.RUNNING;
    }
    #endregion

    #region Behavior Tree Actions
    private Node.NodeState CheckForMoreCustomerOrders()
    {
        var unservedSeats = RestaurantManager.Instance.GetAllSeats()
            .Where(seat =>
            {
                var customer = seat.GetCurrentCustomer();
                return customer != null && !seat.AvailableChair && !customer.HasBeenServed();
            })
            .ToList();

        if (unservedSeats.Any())
        {
            return Node.NodeState.RUNNING;
        }

        TransitionToState(StaffState.ReturningToWaitPosition);
        return Node.NodeState.SUCCESS;
    }
    private Node.NodeState TakeOrder()
    {
        if (CurrentState != StaffState.TakingOrder) return Node.NodeState.FAILURE;

        if (_currentOrder.Customer != null && !_currentOrder.Customer.HasBeenServed())
        {
            _currentOrder.Customer.MarkAsServed();
            Debug.Log($"Da oder: --------------------------{_currentOrder.Customer.HasBeenServed()}");
            TransitionToState(StaffState.MovingToKitchenCounter);
            return Node.NodeState.SUCCESS;
        }
        return Node.NodeState.RUNNING;
    }

    private Node.NodeState MoveToKitchenCounter()
    {
        var moveResult = MoveTo(kitchenCounterPosition.position);

        if (moveResult == Node.NodeState.SUCCESS)
        {
            TransitionToState(StaffState.WaitingForFood);
        }

        Debug.Log($"move to kitchenc counter check: {moveResult}");
        return moveResult;
    }

    private Node.NodeState WaitForMultipleFood()
    {
        var pendingOrders = RestaurantManager.Instance.GetPendingOrders();

        if (pendingOrders.Count == 0)
        {
            TransitionToState(StaffState.ReturningToWaitPosition);
            return Node.NodeState.SUCCESS;
        }

        _orderTakingTimer += Time.deltaTime;
        if (_orderTakingTimer >= 3f)
        {
            _orderTakingTimer = 0;
            TransitionToState(StaffState.DeliveringFood);
            return Node.NodeState.SUCCESS;
        }

        return Node.NodeState.RUNNING;
    }

    private Node.NodeState DeliverMultipleFood()
    {
        var pendingOrders = RestaurantManager.Instance.GetPendingOrders();

        if (pendingOrders.Count == 0)
        {
            TransitionToState(StaffState.ReturningToWaitPosition);
            return Node.NodeState.SUCCESS;
        }

        _currentOrder = pendingOrders[0];
        if (_hasDeliveredFood) return Node.NodeState.SUCCESS;

        if (_currentOrder?.Customer == null)
        {
            RestaurantManager.Instance.RemoveCompletedOrder(_currentOrder);
            return Node.NodeState.FAILURE;
        }

        var customerPosition = _currentOrder.Customer.transform.position;
        var moveResult = MoveTo(customerPosition);

        if (moveResult == Node.NodeState.SUCCESS)
        {
            CompleteDelivery();
            RestaurantManager.Instance.RemoveCompletedOrder(_currentOrder);
            return Node.NodeState.SUCCESS;
        }

        return Node.NodeState.RUNNING;
    }

    private Node.NodeState ReturnToWaitPosition()
    {
        if (CurrentState == StaffState.ReturningToWaitPosition)
        {

            var moveResult = MoveTo(waitingPosition.position);

            if (moveResult == Node.NodeState.SUCCESS)
            {
                TransitionToState(StaffState.Idle);
                return moveResult;
            }
            return moveResult;
        }
        return Node.NodeState.FAILURE;
    }
    #endregion
    #region BehaviorTree 2
    private Node.NodeState FindSeatWithCustomer()
    {
        // Tìm ghế có khách nhưng chưa có order
        var seatsWithCustomers = FindSeatsWithSeatedCustomers();

        if (seatsWithCustomers != null && seatsWithCustomers.Count > 0)
        {
            TransitionToState(StaffState.MovingToCustomer);
            var unservedSeats = seatsWithCustomers
           .Where(seat=>
           {
               var customer = seat.GetCurrentCustomer();
               return customer != null && !customer.HasBeenServed() && customer.HasOrdered();
           }).ToList();
            Debug.Log(unservedSeats.Count);
            if (unservedSeats.Count > 0)
            {
                _currentSeat = FindNearestSeatWithCustomer(seatsWithCustomers);

                if (_currentSeat != null)
                {
                    var customer = _currentSeat.GetCurrentCustomer();
                    if (customer != null)
                    {
                        _currentOrder = new Order
                        {
                            Customer = customer,
                            OrderedDish = customer.CurrentOrder
                        };
                        Debug.Log($"Found seat with customer: {_currentOrder.Customer} {_currentOrder.OrderedDish}");

                        return Node.NodeState.SUCCESS;
                    }
                }
            }
        }
        TransitionToState(StaffState.ReturningToWaitPosition);
        Debug.Log($"No seats with customers found: {seatsWithCustomers}");
        return Node.NodeState.FAILURE;
    }

    private Node.NodeState MoveToCustomerSeat()
    {
        if (_currentSeat == null) return Node.NodeState.FAILURE;

        var moveResult = MoveTo(_currentSeat.transform.position);

        if (moveResult == Node.NodeState.SUCCESS)
        {
            Debug.Log($"Reached seat {_currentSeat.seatID}");
            TransitionToState(StaffState.TakingOrder);
        }

        return moveResult;
    }
    // Các phương thức hỗ trợ mới
    private List<Seat> FindSeatsWithSeatedCustomers()
    {
        var seatsWithSeatedCustomers = new List<Seat>();

        foreach (var seat in RestaurantManager.Instance.GetAllSeats())
        {
            var customer = seat.GetCurrentCustomer();
            if (customer != null && !seat.AvailableChair && customer.HasOrdered())
            {
                seatsWithSeatedCustomers.Add(seat);
                Debug.Log($"nhan ghe {seat.currentCustomer}");
            }
            if (customer != null && !seat.AvailableChair&& customer.HasBeenServed())
            {
                seatsWithSeatedCustomers.Remove(seat);
                Debug.Log($"bo ghe {seat.currentCustomer}");
            }    
        }

        return seatsWithSeatedCustomers;
    }

    private Seat FindNearestSeatWithCustomer(List<Seat> seats)
    {
        Seat nearestSeat = null;
        float shortestDistance = float.MaxValue;

        foreach (var seat in seats)
        {
            float distance = Vector3.Distance(transform.position, seat.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestSeat = seat;
            }
        }

        return nearestSeat;
    }

    // Cần thêm phương thức này vào CustommerBehavior

    #endregion

    #region Helper Methods
    private void CompleteDelivery()
    {
        if (_currentOrder != null)
        {
            RestaurantManager.Instance.CompleteOrder(_currentOrder);
            _currentOrder = null;
            _hasDeliveredFood = true;
            //TransitionToState(StaffState.ReturningToWaitPosition);
        }
    }

    private void UpdateAnimations()
    {
        float targetBlend = agent.velocity.magnitude / agent.speed;
        _currentMovementBlend = Mathf.Lerp(_currentMovementBlend, targetBlend, animationBlendSpeed);

        animator?.SetFloat(MovementSpeed, _currentMovementBlend);
        animator?.SetBool(IsWalking, agent.velocity.magnitude > MOVEMENT_THRESHOLD);
    }

    private void TransitionToState(StaffState newState)
    {
        Debug.Log($"Staff transitioning from {CurrentState} to {newState}");

        // Exit current state logic
        //switch (CurrentState)
        //{
        //    case StaffState.DeliveringFood:
        //        _hasDeliveredFood = false;
        //        break;
        //}

        // Enter new state logic
        switch (newState)
        {
            case StaffState.Idle:
                _currentOrder = null;
                _orderTakingTimer = 0;
                break;
            case StaffState.MovingToCustomer:

                if (_currentOrder == null) return;
                _currentOrder.IsCompleted = true;
                break;
            case StaffState.ReturningToWaitPosition:
                _currentOrder = null;
                CurrentState = StaffState.ReturningToWaitPosition;
                break;
            case StaffState.TakingOrder:

                break;
            case StaffState.MovingToKitchenCounter:

                break;
        }

        CurrentState = newState;
    }
    #endregion

    #region Debug
    public void OnDrawGizmos()
    {
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            var previousPoint = transform.position;

            foreach (var corner in agent.path.corners)
            {
                Gizmos.DrawLine(previousPoint, corner);
                previousPoint = corner;
            }
        }
    }
    #endregion
}