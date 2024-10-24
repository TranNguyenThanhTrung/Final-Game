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

    private NavMeshAgent agent;
    private BehaviorTree tree;
    private Order order;

    //private Node.NodeState treeStatus = Node.NodeState.RUNNING;
    public ActionState state = ActionState.Idle;

    public GameObject frontDoor, dispawnPos, spawnPos;
    public Seat chair;


    public bool isEmtyChair;
    public bool isServed;
    public bool doingState = true;
    public bool ordered = false;

    private float orderWaitTime = 5f; // Thời gian chờ đồ ăn
    private float paymentTime = 3f; // Thời gian để thanh toán
    public float currentWaitTime = 3f;
    private bool isWaitingForFood = false;
    private bool isProcessingPayment = false;

    public Dish currentOrder;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        chair = GetComponent<Seat>();
        Node customerBTRoot = InitializeBehaviorTree();
        tree = new BehaviorTree(customerBTRoot);
    }
    private void Update()
    {
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
        Sequence hasChair = new Sequence(new List<Node> 
        { 
            goToEmptyChair, 
            oderFood, 
            orderCheck 
        });
        Sequence noChair = new Sequence(new List<Node> 
        {
            leaveRestaurant
        });
        Selector chairAvailability = new Selector(new List<Node> { 
            hasChair,   // Nếu có ghế -> thực hiện quy trình có ghế
            noChair    // Nếu không có ghế -> rời đi
        });
        Sequence enterRestaurant = new Sequence(new List<Node>
        {
            goToFrontDoor,
            chairAvailability
        });

        return enterRestaurant;
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

    private Node.NodeState GoToFrontDoor()
    {
        Debug.Log("Dang di toi cua chinh");
        Node.NodeState moveState = GoToLocation(frontDoor.transform.position);

        return Node.NodeState.SUCCESS;
    }
    private Node.NodeState CheckForAvailableChair()
    {
        // Tìm ghế gần nhất thông qua Restaurant Manager
        Seat nearestChair = RestaurantManager.Instance.FindNearestAvailableTable(transform.position);

        if (nearestChair != null)
        {
            chair = nearestChair; // Lưu ghế đã tìm thấy
            Debug.Log("Tìm thấy ghế trống");
            return Node.NodeState.SUCCESS;
        }

        Debug.Log("Không tìm thấy ghế trống - Rời khỏi nhà hàng");
        // Nếu không có ghế, đi ra
        GoToLocation(dispawnPos.transform.position);
        return Node.NodeState.FAILURE;
    }
    private Node.NodeState GoToEmptyChair()
    {
        if (chair != null && chair.TryOccupySeat(this))
        {
            Debug.Log("Đang di chuyển đến ghế");
            Node.NodeState moveState = GoToLocation(chair.transform.position);

            if (moveState == Node.NodeState.SUCCESS)
            {
                Debug.Log("Đã đến ghế");
                return Node.NodeState.SUCCESS;
            }
            return Node.NodeState.RUNNING;
        }
        if (chair == null)
        {
            Debug.Log(" Ghe bi trong");
        }    
        Debug.Log("Không thể ngồi vào ghế");
        
        return Node.NodeState.FAILURE;
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

    private Node.NodeState LeaveRestaurant()
    {
        if (chair != null)
        {
            chair.ReleaseSeat();
            chair = null;
        }

        Debug.Log("Rời nhà hàng");
        Node.NodeState moveState = GoToLocation(dispawnPos.transform.position);

        if (moveState == Node.NodeState.SUCCESS)
        {
            Destroy(gameObject); // Hủy object khi đã đến điểm dispawn
        }

        return moveState;
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
    private Node.NodeState GoToLocation(Vector3 destination)
    {
        float distanceToTarget = Vector3.Distance(transform.position, destination);

        if (state == ActionState.Idle)
        {
            agent.SetDestination(destination);
            state = ActionState.Working;
        }
        else if (Vector3.Distance(agent.pathEndPosition, destination) >= 2)
        {
            state = ActionState.Idle;
            return Node.NodeState.FAILURE;
        }
        else if (distanceToTarget < 0.5f) // Giảm khoảng cách kiểm tra
        {
            state = ActionState.Idle;
            return Node.NodeState.SUCCESS;
        }

        return Node.NodeState.RUNNING;
    }
    private void OnDrawGizmos()
    {
        if (chair != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, chair.transform.position);
        }
    }
}
