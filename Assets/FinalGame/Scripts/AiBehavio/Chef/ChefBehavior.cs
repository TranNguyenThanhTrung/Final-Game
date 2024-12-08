using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using static WaiterBehavior;

public class ChefBehavior : MonoBehaviour
{
    #region SerializeFields
    [Header("Navigation")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationBlendSpeed = 0.1f;

    [Header("Positions")]
    [SerializeField] private GameObject kitchenCounterPosition;
    [SerializeField] private GameObject cookingStationPosition;
    [SerializeField] private GameObject waitingPosition;
    #endregion

    #region Constants
    private const float ARRIVAL_THRESHOLD = 1.5f;
    private const float MOVEMENT_THRESHOLD = 0.1f;
    private const float COOKING_TIME = 3f; // Time to cook a dish
    #endregion

    #region Static Animation Hash
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsCooking = Animator.StringToHash("IsCooking");
    private static readonly int MovementSpeed = Animator.StringToHash("MovementSpeed");
    #endregion

    #region State Management
    public enum ChefState
    {
        Idle,
        ReturnToWaitPos,
        PreparingFood,
        Cooking,
        DeliveringFood,
        Cleaning,
        MoveToCookingStation
    }
    public ChefState CurrentState { get; private set; } = ChefState.Idle;
    #endregion

    #region Private Fields
    private BehaviorTree _tree;
    private Order _currentOrder;
    private float _currentMovementBlend;
    private float _cookingTimer = 0f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializeBehaviorTree();
        SubscribeToEvents();
    }

    private void Update()
    {
        UpdateAnimations();
        _tree.Update();
    }

    private void OnDestroy()
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
            new Leaf(ReturnToWaitPos)
        });
    }

    private Node CreateOrderHandlingSequence()
    {
        return new Sequence(new List<Node>
        {
            new Leaf(FindPendingOrder),
            new Leaf(MoveToCookingStation),
            new Leaf(PrepareDish),
            new Leaf(DeliverFoodToWaiter),
            //new Leaf(CleanWorkStation)
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
        if (CurrentState == ChefState.Idle)
        {
            _currentOrder = order;
            TransitionToState(ChefState.PreparingFood);
        }
    }
    #endregion

    #region Behavior Tree Actions
    private Node.NodeState FindPendingOrder()
    {
        var pendingOrders = RestaurantManager.Instance.GetPendingOrders();
        if (pendingOrders.Count > 0)
        {
            TransitionToState(ChefState.MoveToCookingStation);
            _currentOrder = pendingOrders.First();
            Debug.Log($"<color=green>Take Order1594: {pendingOrders} {pendingOrders.Count} {_currentOrder.OrderedDish} -> Moveto cookstation</color>");
            return Node.NodeState.SUCCESS;
        }
        Debug.Log($"<color=green>Order1594:{_currentOrder}</color>");
        TransitionToState(ChefState.ReturnToWaitPos);
        return Node.NodeState.FAILURE;
    }

    private Node.NodeState MoveToCookingStation()
    {
        var moveResult = MoveTo(cookingStationPosition);
        if (moveResult == Node.NodeState.SUCCESS)
        {
            TransitionToState(ChefState.Cooking);
        }
        Debug.Log($"<color=green>Move to cooking station check: {moveResult}</color>");
        return moveResult;
    }

    private Node.NodeState PrepareDish()
    {
        if (CurrentState != ChefState.Cooking) return Node.NodeState.FAILURE;

        _cookingTimer += Time.deltaTime;
        animator.SetBool(IsCooking, true);

        if (_cookingTimer >= COOKING_TIME)
        {
            _cookingTimer = 0f;
            animator.SetBool(IsCooking, false);
            TransitionToState(ChefState.DeliveringFood);
            return Node.NodeState.SUCCESS;
        }

        return Node.NodeState.RUNNING;
    }

    private Node.NodeState DeliverFoodToWaiter()
    {
        var moveResult = MoveTo(kitchenCounterPosition);

        if (moveResult == Node.NodeState.SUCCESS)
        {
            // Signal that food is ready for pickup
            if (_currentOrder != null)
            {
                RestaurantManager.Instance.CompleteOrder(_currentOrder);
                _currentOrder = null;
            }
            return Node.NodeState.SUCCESS;
        }

        return moveResult;
    }

    private Node.NodeState CleanWorkStation()
    {
        TransitionToState(ChefState.Cleaning);
        // Add cleaning logic if needed
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState ReturnToIdleState()
    {
        TransitionToState(ChefState.Idle);
        return Node.NodeState.SUCCESS;
    }
    private Node.NodeState ReturnToWaitPos()
    {
        Debug.Log($"<color=green> Current state: {CurrentState} </color>");
        if (CurrentState == ChefState.ReturnToWaitPos)
        {
            var moveResult = MoveTo(waitingPosition);

            if (moveResult == Node.NodeState.SUCCESS)
            {
                TransitionToState(ChefState.Idle);
                Debug.Log($"<color=green> Return to wait pos {CurrentState} {moveResult}</color>");
                return moveResult;
            }
            return moveResult;
        }
        return Node.NodeState.FAILURE;
    }
    #endregion

    #region Navigation
    private Node.NodeState MoveTo(GameObject destination)
    {
        float distanceToTarget = Vector3.Distance(transform.position, destination.transform.position);
        Debug.Log($"<color=green>Target: {destination}" + $"Agent befor check: {agent.hasPath}</color>");
        if (!agent.hasPath || agent.remainingDistance > agent.stoppingDistance)
        {
            agent.SetDestination(destination.transform.position);
            Debug.Log($"<color=green>Agent after check: {agent.hasPath}</color>");
            return Node.NodeState.RUNNING;
        }

        if (distanceToTarget < ARRIVAL_THRESHOLD)
        {
            agent.ResetPath();
            return Node.NodeState.SUCCESS;
        }

        return Node.NodeState.RUNNING;
    }
    #endregion

    #region Helper Methods
    private void UpdateAnimations()
    {
        float targetBlend = agent.velocity.magnitude / agent.speed;
        _currentMovementBlend = Mathf.Lerp(_currentMovementBlend, targetBlend, animationBlendSpeed);

        animator?.SetFloat(MovementSpeed, _currentMovementBlend);
        animator?.SetBool(IsWalking, agent.velocity.magnitude > MOVEMENT_THRESHOLD);
    }

    private void TransitionToState(ChefState newState)
    {
        Debug.Log($"<color=green>Chef transitioning from {CurrentState} to {newState}</color>");

        // Enter new state logic
        switch (newState)
        {
            case ChefState.Idle:
                _currentOrder = null;
                _cookingTimer = 0f;
                break;
            case ChefState.Cooking:
                animator.SetBool(IsCooking, true);
                break;
            case ChefState.Cleaning:
                // Add any cleaning state initialization
                break;
        }

        CurrentState = newState;
    }
    #endregion

    #region Debug
    private void OnDrawGizmos()
    {
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.red;
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