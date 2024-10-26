using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.AI;


public class CustommerBehavior : MonoBehaviour
{
    public enum ActionState
    {
        Idle,
        Working
    }
    public ActionState state = ActionState.Idle;
    private List<Seat> availableSeats = new List<Seat>();

    private NavMeshAgent agent;
    private BehaviorTree tree;
    private Order order;
    private Seat chair;
    public GameObject frontDoor, dispawnPos, spawnPos;

    private bool isEmtyChair;
    private bool isServed;
    private bool doingState = true;
    private bool ordered = false;
    private bool isWaitingForFood = false;
    private bool isProcessingPayment = false;
    private bool hasReachedFrontDoor = false;
    private bool isMoving = false;


    private const float maxStuckTime = 3.0f;
    private const float MAX_SEAT_SEARCH_DISTANCE = 20f; // Khoảng cách tối đa để tìm ghế
    private float timeStuck = 0;
    private float orderWaitTime = 5f; // Thời gian chờ đồ ăn
    private float paymentTime = 3f; // Thời gian để thanh toán
    public float currentWaitTime = 3f;

    public Dish currentOrder;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent không tìm thấy trên Customer!");
        }
    }
    private void Start()
    {
        chair = GetComponent<Seat>();
        Node customerBTRoot = InitializeBehaviorTree();
        tree = new BehaviorTree(customerBTRoot);
    }
    private void Update()
    {
        if (agent.hasPath && state == ActionState.Working)
        {
            Debug.DrawLine(transform.position, agent.destination, Color.yellow);
        }

        tree.Update();
    }



    private Node InitializeBehaviorTree()
    {
        Leaf goToFrontDoor = new Leaf(GoToFrontDoor);
        Leaf goToEmptyChair = new Leaf(GoToEmptyChair);
        Leaf checkForAvailableChair = new Leaf(CheckForAvailableChair);
        Leaf isWaiting = new Leaf(IsWaiting);
        Leaf oderFood = new Leaf(OrderFood);
        Leaf processPayment = new Leaf(Pay);
        Leaf leaveRestaurant = new Leaf(LeaveRestaurant);

        Sequence hasBeenServed = new Sequence(new List<Node> 
        { 
            processPayment,
            leaveRestaurant 
        });
        Sequence notBeenServed = new Sequence(new List<Node> 
        { 
            isWaiting 
        });

        Selector orderCheck = new Selector(new List<Node>
        { 
            hasBeenServed,
            notBeenServed 
        });

        Sequence findAndSitSequence = new Sequence(new List<Node>
        {
            checkForAvailableChair,
            goToEmptyChair,
            oderFood,
            orderCheck
        });
        Sequence noChairSequence = new Sequence(new List<Node> 
        {
            leaveRestaurant
        });
        Selector chairAvailability = new Selector(new List<Node> {
            findAndSitSequence,   // Nếu có ghế -> thực hiện quy trình có ghế
            noChairSequence    // Nếu không có ghế -> rời đi
        });
        Sequence enterRestaurant = new Sequence(new List<Node>
        {
            goToFrontDoor,
            chairAvailability
        });

        return enterRestaurant;
    }

    private Node.NodeState GoToFrontDoor()
    {
        if (hasReachedFrontDoor) return Node.NodeState.SUCCESS;

        Debug.Log("Đang di chuyển tới cửa chính");
        Node.NodeState moveState = GoToLocation(frontDoor.transform.position);

        if (moveState == Node.NodeState.SUCCESS)
        {
            hasReachedFrontDoor = true;
            Debug.Log("Đã đến cửa chính - Tiếp tục tìm ghế");
        }

        return moveState;
    }

    private Node.NodeState CheckForAvailableChair()
    {
        Debug.Log("Đang kiểm tra ghế trống...");

        // Đảm bảo ghế cũ đã được giải phóng
        if (chair != null)
        {
            chair.ReleaseSeat();
            chair = null;
        }

        // Tìm ghế gần nhất thông qua Restaurant Manager
        Seat nearestChair = RestaurantManager.Instance.FindNearestAvailableTable(transform.position);

        if (nearestChair != null)
        {
            chair = nearestChair;
            Debug.Log($"Tìm thấy ghế trống tại {chair.transform.position}");
            return Node.NodeState.SUCCESS;
        }

        Debug.Log("Không tìm thấy ghế trống - Chuẩn bị rời đi");
        return Node.NodeState.FAILURE;
    }

    private Node.NodeState GoToEmptyChair()
    {
        if (chair == null)
        {
            Debug.LogError("GoToEmptyChair: Không có ghế để đi tới!");
            return Node.NodeState.FAILURE;
        }

        if (!chair.AvailableChair)
        {
            Debug.LogWarning("GoToEmptyChair: Ghế không còn trống!");
            chair = null;
            return Node.NodeState.FAILURE;
        }

        // Cố gắng chiếm ghế nếu chưa chiếm
        if (state == ActionState.Idle)
        {
            if (!chair.TryOccupySeat(this))
            {
                Debug.LogWarning("GoToEmptyChair: Không thể chiếm ghế!");
                chair = null;
                return Node.NodeState.FAILURE;
            }
            Debug.Log($"GoToEmptyChair: Đã chiếm ghế {chair.gameObject.name}, bắt đầu di chuyển");
        }

        // Thực hiện di chuyển
        Node.NodeState moveState = GoToLocation(chair.transform.position);

        switch (moveState)
        {
            case Node.NodeState.SUCCESS:
                Debug.Log("GoToEmptyChair: Đã đến ghế thành công");
                state = ActionState.Idle;
                return Node.NodeState.SUCCESS;

            case Node.NodeState.FAILURE:
                Debug.LogWarning("GoToEmptyChair: Di chuyển thất bại - giải phóng ghế");
                if (chair != null)
                {
                    chair.ReleaseSeat();
                    chair = null;
                }
                state = ActionState.Idle;
                return Node.NodeState.FAILURE;

            default:
                return Node.NodeState.RUNNING;
        }
    }

    private Node.NodeState OrderFood()
    {
        if (!ordered)
        {
            currentOrder = (Dish)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(Dish)).Length);
            Debug.Log($"Gọi món: {currentOrder}");

            // Gửi order thông qua RestaurantManager
            RestaurantManager.Instance.SubmitOrder(this, currentOrder);

            ordered = true;
            isWaitingForFood = true;
            currentWaitTime = 0f;
            return Node.NodeState.RUNNING;
        }
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState IsWaiting()
    {
        if (isWaitingForFood)
        {
            if (currentWaitTime >= orderWaitTime)
            {
                isWaitingForFood = false;
                isServed = true;
                return Node.NodeState.SUCCESS;
            }
            return Node.NodeState.RUNNING;
        }
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState Pay()
    {
        if (isServed)
        {
            Debug.Log("Đang tính tiền");
            // RestaurantManager sẽ tự động xử lý thanh toán khi order complete
            return Node.NodeState.SUCCESS;
        }
        return Node.NodeState.FAILURE;
    }

    private Node.NodeState LeaveRestaurant()
    {
        if (chair != null)
        {
            chair.ReleaseSeat();
            chair = null;
        }

        Debug.Log("Đang rời nhà hàng");
        Node.NodeState moveState = GoToLocation(dispawnPos.transform.position);

        if (moveState == Node.NodeState.SUCCESS)
        {
            Debug.Log("Đã đến điểm rời đi - Hủy object");
            Destroy(gameObject);
        }

        return moveState;
    }



    private Node.NodeState GoToLocation(Vector3 destination)
    {
        float distanceToTarget = Vector3.Distance(transform.position, destination);

        if (state == ActionState.Idle)
        {
            Debug.Log($"Bắt đầu di chuyển đến {destination}");
            agent.SetDestination(destination);
            state = ActionState.Working;
        }

        // Kiểm tra path
        if (!agent.hasPath)
        {
            Debug.LogWarning("Không tìm thấy đường đi!");
            state = ActionState.Idle;
            return Node.NodeState.FAILURE;
        }

        // Kiểm tra đến nơi
        if (distanceToTarget < 0.5f)
        {
            Debug.Log($"Đã đến điểm đích {destination}");
            state = ActionState.Idle;
            return Node.NodeState.SUCCESS;
        }

        // Kiểm tra bị kẹt
        if (agent.velocity.magnitude < 0.01f && state == ActionState.Working)
        {
            timeStuck += Time.deltaTime;
            if (timeStuck > maxStuckTime)
            {
                Debug.LogWarning("Agent bị kẹt quá lâu!");
                state = ActionState.Idle;
                timeStuck = 0;
                return Node.NodeState.FAILURE;
            }
        }
        else
        {
            timeStuck = 0;
        }

        return Node.NodeState.RUNNING;
    }
    public void ResetState()
    {
        hasReachedFrontDoor = false;
        isMoving = false;
        state = ActionState.Idle;
        if (agent != null) agent.ResetPath();
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Vẽ đường đi hiện tại
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            var path = agent.path;
            Vector3 previousCorner = transform.position;
            foreach (var corner in path.corners)
            {
                Gizmos.DrawLine(previousCorner, corner);
                previousCorner = corner;
            }
        }

        // Vẽ đường đến ghế đã chọn
        if (chair != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, chair.transform.position);
            Gizmos.DrawWireSphere(chair.transform.position, 0.5f);
        }
    }
#endif
}
