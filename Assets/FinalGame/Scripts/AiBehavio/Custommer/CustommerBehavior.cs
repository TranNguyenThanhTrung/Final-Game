using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;

public class CustommerBehavior : MonoBehaviour
{
    #region Constants
    private const float MAX_STUCK_TIME = 3.0f;
    private const float ORDER_WAIT_TIME = 5f;
    private const float PAYMENT_TIME = 3f;
    private const float ARRIVAL_THRESHOLD = 0.5f;
    private const float MOVEMENT_THRESHOLD = 0.1f;
    #endregion

    #region SerializeFields
    [Header("Navigation Points")]
    [SerializeField] private GameObject frontDoor;
    [SerializeField] private GameObject dispawnPos;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationBlendSpeed = 0.1f;
    [SerializeField] private float seatTriggerDistance = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    #endregion

    #region Animation Parameters
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsSitting = Animator.StringToHash("IsSitting");
    private static readonly int MovementSpeed = Animator.StringToHash("MovementSpeed");
    private static readonly int SitTrigger = Animator.StringToHash("Sit");
    private static readonly int StandTrigger = Animator.StringToHash("Stand");
    #endregion

    #region State Management
    public enum CustomerState
    {
        Idle,
        Walking,
        Sitting,
        Eating,
        Leaving,
        Working
    }

    public CustomerState CurrentState { get; private set; } = CustomerState.Idle;
    public Dish CurrentOrder { get; private set; }
    public float CurrentWaitTime { get; private set; } = 3f;
    #endregion

    #region Private Fields
    private NavMeshAgent _agent;
    private BehaviorTree _tree;
    private Seat _currentSeat;

    private bool _hasReachedFrontDoor;
    private bool _hasOrdered;
    private bool _isWaitingForFood;
    private bool _isServed;
    private bool _isRotatingToTable;
    private float _timeStuck;
    private float _currentMovementBlend;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializeBehaviorTree();
    }

    private void Update()
    {
        UpdateAnimations();
        DrawDebugPath();
        _tree.Update();
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            Debug.LogError($"[{nameof(CustommerBehavior)}] NavMeshAgent component missing!");
        }
    }

    private void InitializeBehaviorTree()
    {
        Node rootNode = CreateBehaviorTree();
        _tree = new BehaviorTree(rootNode);
    }

    private Node CreateBehaviorTree()
    {
        var enterRestaurant = new Sequence(new List<Node>
        {
            new Leaf(GoToFrontDoor),
            new Selector(new List<Node>
            {
                CreateSeatingSequence(),
                new Leaf(LeaveRestaurant)
            })
        });

        return enterRestaurant;
    }

    private Node CreateSeatingSequence()
    {
        return new Sequence(new List<Node>
        {
            new Leaf(FindAvailableSeat),
            new Leaf(GoToSeat),
            new Leaf(OrderFood),
            CreateOrderCheckSelector()
        });
    }

    private Node CreateOrderCheckSelector()
    {
        return new Selector(new List<Node>
        {
            new Sequence(new List<Node>
            {
                new Leaf(ProcessPayment),
                new Leaf(LeaveRestaurant)
            }),
            new Leaf(WaitForOrder)
        });
    }
    #endregion

    #region Animation Management
    private void UpdateAnimations()
    {
        // Update movement animation
        float targetBlend = _agent.velocity.magnitude / _agent.speed;
        _currentMovementBlend = Mathf.Lerp(_currentMovementBlend, targetBlend, animationBlendSpeed);
        animator.SetFloat(MovementSpeed, _currentMovementBlend);

        // Update walking state
        bool isMoving = _agent.velocity.magnitude > MOVEMENT_THRESHOLD;
        animator.SetBool(IsWalking, isMoving);
    }

    private void PlaySitAnimation()
    {
        animator.SetTrigger(SitTrigger);
        animator.SetBool(IsSitting, true);
    }

    private void PlayStandAnimation()
    {
        animator.SetTrigger(StandTrigger);
        animator.SetBool(IsSitting, false);
    }


    #endregion

    #region Behavior Tree Actions
    private Node.NodeState GoToFrontDoor()
    {
        if (_hasReachedFrontDoor) return Node.NodeState.SUCCESS;

        var moveState = MoveTo(frontDoor.transform.position);
        if (moveState == Node.NodeState.SUCCESS)
        {
            _hasReachedFrontDoor = true;
            Debug.Log("Reached front door - Proceeding to find seat");
        }

        return moveState;
    }

    private Node.NodeState FindAvailableSeat()
    {
        ReleasePreviousSeat();

        var nearestSeat = RestaurantManager.Instance.FindNearestAvailableTable(transform.position);
        if (nearestSeat != null && nearestSeat.TryOccupySeat(this))
        {
            _currentSeat = nearestSeat;
            Debug.Log($"Found and occupied seat at {_currentSeat.transform.position}");
            return Node.NodeState.SUCCESS;
        }

        Debug.Log("No available seats found - Preparing to leave");
        return Node.NodeState.FAILURE;
    }

    private Node.NodeState GoToSeat()
    {
        float distanceToSeat = Vector3.Distance(transform.position, _currentSeat.transform.position);
        var moveState = MoveTo(_currentSeat.transform.position);
        if (_currentSeat == null)
        {
            Debug.LogError("GoToSeat: No seat assigned!");
            return Node.NodeState.FAILURE;
        }

        if (distanceToSeat <= seatTriggerDistance)
        {
            // Dịch chuyển người chơi lên ghế
            TeleportToSeat();

            // Bắt đầu xoay về phía bàn
            StartRotatingTowardsTable();

            CurrentState = CustomerState.Sitting;
            PlaySitAnimation();
            return Node.NodeState.SUCCESS;
        }

        switch (moveState)
        {
            case Node.NodeState.SUCCESS:
                Debug.Log("Successfully reached seat");
                PlaySitAnimation();
                CurrentState = CustomerState.Sitting;
                return Node.NodeState.SUCCESS;

            case Node.NodeState.FAILURE:
                Debug.LogWarning("Failed to reach seat - releasing reservation");
                ReleasePreviousSeat();
                CurrentState = CustomerState.Idle;
                return Node.NodeState.FAILURE;

            default:
                return Node.NodeState.RUNNING;
        }
    }
    private void TeleportToSeat()
    {
        // Tắt NavMeshAgent để có thể teleport
        _agent.enabled = false;
        switch (_currentSeat.seatID)
        {
            case 1:
                transform.position = _currentSeat.transform.position - new Vector3(0, 0.15f, 0);
                break;
            case 2:
                transform.position = _currentSeat.transform.position - new Vector3(0, 0.4f, 0);
                break;
            case 3:
                transform.position = _currentSeat.transform.position - new Vector3(0, 0, 0);
                break;
        }
        // Dịch chuyển người chơi đến đúng vị trí ghế


        // Bật lại NavMeshAgent
        _agent.enabled = true;
    }
    private void StartRotatingTowardsTable()
    {
        if (_currentSeat.TableTransform != null)
        {
            _isRotatingToTable = true;
            StartCoroutine(RotateTowardsTable());
        }
    }
    private System.Collections.IEnumerator RotateTowardsTable()
    {
        // Lấy hướng của bàn từ ghế
        Vector3 targetDirection = _currentSeat.TableTransform.forward;

        while (_isRotatingToTable)
        {
            // Tính toán góc xoay hiện tại
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // Xoay mượt về hướng bàn
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Kiểm tra nếu đã xoay gần đúng hướng
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                transform.rotation = targetRotation;
                _isRotatingToTable = false;
            }

            yield return null;
        }
    }

    private Node.NodeState OrderFood()
    {
        if (!_hasOrdered)
        {
            CurrentOrder = (Dish)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Dish)).Length);
            Debug.Log($"Ordering: {CurrentOrder}");

            RestaurantManager.Instance.SubmitOrder(this, CurrentOrder);

            _hasOrdered = true;
            _isWaitingForFood = true;
            CurrentWaitTime = 0f;
            return Node.NodeState.RUNNING;
        }
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState WaitForOrder()
    {
        if (!_isWaitingForFood) return Node.NodeState.SUCCESS;

        if (CurrentWaitTime >= ORDER_WAIT_TIME)
        {
            _isWaitingForFood = false;
            _isServed = true;
            return Node.NodeState.SUCCESS;
        }
        return Node.NodeState.RUNNING;
    }

    private Node.NodeState ProcessPayment()
    {
        if (_isServed)
        {
            PlayStandAnimation();
            return Node.NodeState.SUCCESS;
        }
        return Node.NodeState.FAILURE;
    }

    private Node.NodeState LeaveRestaurant()
    {
        ReleasePreviousSeat();
        CurrentState = CustomerState.Leaving;

        var moveState = MoveTo(dispawnPos.transform.position);
        if (moveState == Node.NodeState.SUCCESS)
        {
            Debug.Log("Reached exit point - Destroying object");
            Destroy(gameObject);
        }

        return moveState;
    }
    #endregion

    #region Navigation
    private Node.NodeState MoveTo(Vector3 destination)
    {
        float distanceToTarget = Vector3.Distance(transform.position, destination);

        if (CurrentState == CustomerState.Idle)
        {
            Debug.Log($"Starting movement to {destination}");
            _agent.SetDestination(destination);
            CurrentState = CustomerState.Working;
        }

        if (!_agent.hasPath)
        {
            Debug.LogWarning("No valid path found!");
            CurrentState = CustomerState.Idle;
            return Node.NodeState.FAILURE;
        }

        if (distanceToTarget < ARRIVAL_THRESHOLD)
        {
            Debug.Log($"Reached destination {destination}");
            CurrentState = CustomerState.Idle;
            return Node.NodeState.SUCCESS;
        }

        return HandleStuckDetection();
    }

    private Node.NodeState HandleStuckDetection()
    {
        if (_agent.velocity.magnitude < 0.01f && CurrentState == CustomerState.Working)
        {
            _timeStuck += Time.deltaTime;
            if (_timeStuck > MAX_STUCK_TIME)
            {
                Debug.LogWarning("Agent stuck for too long!");
                CurrentState = CustomerState.Idle;
                _timeStuck = 0;
                return Node.NodeState.FAILURE;
            }
        }
        else
        {
            _timeStuck = 0;
        }

        return Node.NodeState.RUNNING;
    }
    #endregion

    #region Public Methods
    public void ResetState()
    {
        _hasReachedFrontDoor = false;
        CurrentState = CustomerState.Idle;
        _agent?.ResetPath();
    }
    #endregion

    #region Private Helper Methods
    private void ReleasePreviousSeat()
    {
        if (_currentSeat != null)
        {
            _currentSeat.ReleaseSeat();
            _currentSeat = null;
        }
    }

    private void DrawDebugPath()
    {
        if (_agent.hasPath && CurrentState == CustomerState.Working)
        {
            Debug.DrawLine(transform.position, _agent.destination, Color.yellow);
        }
    }
    #endregion

    #region Debug Visualization
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        DrawPathGizmos();
        DrawSeatGizmos();
    }

    private void DrawPathGizmos()
    {
        if (_agent != null && _agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3 previousCorner = transform.position;
            foreach (var corner in _agent.path.corners)
            {
                Gizmos.DrawLine(previousCorner, corner);
                previousCorner = corner;
            }
        }
    }

    private void DrawSeatGizmos()
    {
        if (_currentSeat != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _currentSeat.transform.position);
            Gizmos.DrawWireSphere(_currentSeat.transform.position, 0.5f);
        }
    }
#endif
    #endregion

    #region State Transitions
    private void TransitionToState(CustomerState newState)
    {
        // Exit current state
        switch (CurrentState)
        {
            case CustomerState.Sitting:
                PlayStandAnimation();
                break;
        }

        // Enter new state
        switch (newState)
        {
            case CustomerState.Walking:
                animator.SetBool(IsWalking, true);
                break;
            case CustomerState.Sitting:
                PlaySitAnimation();
                break;

        }

        CurrentState = newState;
    }
    #endregion
}