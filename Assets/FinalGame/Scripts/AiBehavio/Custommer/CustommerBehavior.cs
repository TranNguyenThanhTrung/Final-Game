//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.AI;

//public class CustommerBehavior : MonoBehaviour
//{
//    public GameObject spawnPos;
//    public GameObject resTaurantPos;
//    public GameObject disSpawnPos;
//    [SerializeField] private Seat seat;
//    public enum FoodType
//    {
//        Burger,
//        Water,
//        Apple,
//        None
//    }
//    [Header("ScriptableObject References")]
//    public ActionState state = ActionState.Idle;

//    private NavMeshAgent agent;
//    private Behaviortree tree;
//    private Node.Status treeStatus = Node.Status.RUNNING;

//    public enum ActionState
//    {
//        Idle,
//        Working
//    }

//    private void Awake()
//    {
//        agent = GetComponent<NavMeshAgent>();
//        InitializeTables();
//    }

//    private void OnEnable()
//    {
//        //orderChannel.OnOrderCreated += HandleNewOrder;
//    }

//    private void OnDisable()
//    {
//        //orderChannel.OnOrderCreated -= HandleNewOrder;
//    }

//    private void InitializeTables()
//    {
//    }

//    void Start()
//    {
//        InitializeBehaviorTree();
//    }

//    private void InitializeBehaviorTree()
//    {
//        tree = new Behaviortree();
//        Sequence GoToEat = new Sequence("Service");

//        Leaf moveToRestaurant = new Leaf("Move ToRestaurant", () => MoveToRestaurant());
//        Leaf findSeat = new Leaf("Find Seat", () => FindSeat());
//        Leaf oder = new Leaf("Oder", () => Oder());
//        Leaf waitForOder = new Leaf("Wait For Oder", () => WaitForOder());
//        Leaf leaveRestaurant = new Leaf("Leave Restaurant", () => LeaveRestaurant());

//        GoToEat.AddChild(moveToRestaurant);
//        GoToEat.AddChild(findSeat);
//        GoToEat.AddChild(oder);
//        GoToEat.AddChild(waitForOder);
//        GoToEat.AddChild(leaveRestaurant);

//        tree.AddChild(GoToEat);
//    }

//    private Node.Status Oder()
//    {
//        throw new NotImplementedException();
//    }


//    private Node.Status WaitForOder()
//    {
//        throw new NotImplementedException();
//    }

//    private Node.Status FindSeat()
//    {
//        if (!seat.hasCustommer)
//        {
//            //Ngoi luon
//            return Node.Status.SUCCESS;
//        }
//        else 
//        { 
//            // tim ghe khac
//        }
//        return Node.Status.SUCCESS;
//    }

//    private Node.Status MoveToRestaurant()
//    {
//        var status = GoToLocation(resTaurantPos.transform.position);
//        return status;
//    }
//    private Node.Status LeaveRestaurant()
//    {
//        var status = GoToLocation(disSpawnPos.transform.position);
//        return status;
//    }

//    void Update()
//    {
//        if (treeStatus != Node.Status.SUCCESS)
//        {
//            treeStatus = tree.Process();
//        }
//    }

//    private Node.Status GoToLocation(Vector3 destination)
//    {
//        float distance = Vector3.Distance(transform.position, destination);

//        if (state == ActionState.Idle)
//        {
//            agent.SetDestination(destination);
//            state = ActionState.Working;
//        }
//        else if (Vector3.Distance(agent.pathEndPosition, destination) >= 2)
//        {
//            state = ActionState.Idle;
//            return Node.Status.FAILURE;
//        }
//        else if (distance < 2)
//        {
//            state = ActionState.Idle;
//            return Node.Status.SUCCESS;

//        }
//        return Node.Status.RUNNING;
//    }
//}
