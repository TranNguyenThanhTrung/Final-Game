using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class WaiterBehavior : MonoBehaviour
{
    #region Constants
    private const float ARRIVAL_THRESHOLD = 0.5f;
    private const float MOVEMENT_THRESHOLD = 0.1f;
    private const float ORDER_TAKING_TIME = 3f;
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

    public StaffState CurrentState { get; private set; } = StaffState.ReturningToWaitPosition;
    #endregion

    #region Private Fields
    private BehaviorTree _tree;
    private Order _currentOrder;
    private CustommerBehavior _currentCustomer;
    private float _currentMovementBlend;
    private float _orderTakingTimer;
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
        return new Sequence(new List<Node>
        {
            new Leaf(FindNewOrder),
            new Leaf(MoveToCustomer),
            new Leaf(TakeOrder),
            new Leaf(MoveToKitchenCounter),
            new Leaf(WaitForFood),
            new Leaf(DeliverFood)
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
            Debug.Log("Di toi cho kach " + CurrentState);
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
        CurrentState = StaffState.MovingToCustomer;
        if (_currentOrder == null) return Node.NodeState.FAILURE;

        var customerPosition = _currentOrder.Customer.transform.position;
        var moveResult = MoveTo(customerPosition);
        if (moveResult == Node.NodeState.SUCCESS)
        {
            Debug.Log("Move to customer state Check: " + moveResult);
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
            _orderTakingTimer = 0;
            TransitionToState(StaffState.ReturningToWaitPosition);
            State = Node.NodeState.SUCCESS;
            Debug.Log("Move to customer state Check: " + State);
            return State;
        }

        Debug.Log("Move to customer state Check: " + State);
        return State;
    }

    private Node.NodeState MoveToKitchenCounter()
    {
        var moveResult = MoveTo(kitchenCounterPosition.position);

        if (moveResult == Node.NodeState.SUCCESS)
        {
            TransitionToState(StaffState.WaitingForFood);
        }

        Debug.Log($"move to kitchen check: {moveResult}");
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