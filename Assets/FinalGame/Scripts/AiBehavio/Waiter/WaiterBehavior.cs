using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;

public class WaiterBehavior : MonoBehaviour
{
    #region Constants
    private const float ARRIVAL_THRESHOLD = 0.5f;
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
        //    new Leaf(WaitForFood),
        //    new Leaf(DeliverFood)
        //});
        return new Sequence(new List<Node>
        {
            new Leaf(FindSeatWithCustomer),      // Tìm ghế có khách
            new Leaf(MoveToCustomerSeat),        // Di chuyển đến ghế có khách
            new Leaf(FindCustomerOrder),         // Tìm order của khách tại ghế đó
            new Leaf(MoveToCustomer),            // Di chuyển đến khách
            new Leaf(TakeOrder),                 // Lấy order
            new Leaf(MoveToKitchenCounter),      // Di chuyển đến quầy bếp
            new Leaf(WaitForFood),               // Chờ thức ăn
            new Leaf(DeliverFood)                // Phục vụ thức ăn
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
    private Node.NodeState FindNewOrder()
    {
        if (_currentOrder != null)
        {

            Debug.Log("CurrentOder----------- Thanh cong: " + _currentOrder);
            return Node.NodeState.SUCCESS;
        }
        else
        {

            Debug.Log("CurrentOder-----------That Bai: " + _currentOrder);
            return Node.NodeState.FAILURE;
        }
    }

    private Node.NodeState MoveToCustomer()
    {
        var currentOrder = _currentOrder;
        CurrentState = StaffState.MovingToCustomer;
        if (_currentOrder == null) return Node.NodeState.FAILURE;
        var customerPosition = _currentOrder.Customer.transform.position;
        
        var moveResult = MoveTo(customerPosition);
        if (moveResult == Node.NodeState.SUCCESS)
        {
            TransitionToState(StaffState.TakingOrder);
        }

        Debug.Log("Move to customer state Check: " + moveResult);
        return moveResult;
    }

    private Node.NodeState TakeOrder()
    {
        var State = Node.NodeState.RUNNING;
        if (CurrentState != StaffState.TakingOrder) return Node.NodeState.FAILURE;
        _orderTakingTimer += Time.deltaTime;

        if (_orderTakingTimer >= ORDER_TAKING_TIME)
        {
            Debug.Log($"Khach goi {_currentOrder.Customer.name}");
            _orderTakingTimer = 0;
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

    private Node.NodeState WaitForFood()
    {
        _orderTakingTimer += Time.deltaTime;
        if (_orderTakingTimer >= 3f)
        {
            _orderTakingTimer = 0;
            TransitionToState(StaffState.DeliveringFood);
            return Node.NodeState.SUCCESS;
        }

        return Node.NodeState.RUNNING;
    }

    private Node.NodeState DeliverFood()
    {
        if (_hasDeliveredFood) return Node.NodeState.SUCCESS;

        var customerPosition = _currentOrder.Customer.transform.position;
        var moveResult = MoveTo(customerPosition);

        if (moveResult == Node.NodeState.SUCCESS)
        {
            CompleteDelivery();
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
            // Chọn ghế gần nhất
            _currentSeat = FindNearestSeatWithCustomer(seatsWithCustomers);

            if (_currentSeat != null)
            {
                Debug.Log($"Found seat with customer: {_currentOrder}");
                return Node.NodeState.SUCCESS;
            }
        }


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
            TransitionToState(StaffState.MovingToCustomer);
        }

        return moveResult;
    }
    private Node.NodeState FindCustomerOrder()
    {
        if (_currentSeat == null) return Node.NodeState.FAILURE;

        var customer = _currentSeat.GetCurrentCustomer();
        if (customer != null && !customer.HasOrdered())
        {
            _currentOrder = new Order
            {
                Customer = customer,
                CustomerSeat = _currentSeat
            };

            Debug.Log($"Found unordered customer at seat {_currentSeat.seatID}");
            return Node.NodeState.SUCCESS;
        }

        Debug.Log("No unordered customer found at seat");
        return Node.NodeState.FAILURE;
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
                Debug.Log($"Found seated unordered customer at seat {seat.seatID}");
                seatsWithSeatedCustomers.Add(seat);
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