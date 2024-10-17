using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CustommerBehavior : MonoBehaviour
{
    public enum FoodType
    {
        Burger,
        Water,
        Apple,
        None
    }
    public enum ActionState
    {
        Idle,
        Working
    }

    private NavMeshAgent agent;
    private BehaviorTree tree;

    private Node.NodeState treeStatus = Node.NodeState.RUNNING;
    public ActionState state = ActionState.Idle;


    public bool isEmtyChair;
    public bool isServed;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        Node customerBTRoot = InitializeBehaviorTree();
        tree = new BehaviorTree(customerBTRoot);
    }


    private Node InitializeBehaviorTree()
    {
        Leaf processPayment = new Leaf(Pay);
        Leaf leaveRestaurant = new Leaf(LeaveRestaurant);
        Leaf isWaiting = new Leaf(IsWaiting);
        Leaf goToEmptyChair = new Leaf(GoToEmptyChair);
        Leaf goToFrontDoor = new Leaf(GoToFrontDoor);

        Sequence hasBeenServed = new Sequence(new List<Node> { processPayment, leaveRestaurant });
        Sequence notBeenServed = new Sequence(new List<Node> { isWaiting });
        Selector orderCheck = new Selector(new List<Node> { hasBeenServed, notBeenServed });
        Sequence hasChair = new Sequence(new List<Node> { goToEmptyChair, orderCheck });
        Sequence noAvailableChair = new Sequence(new List<Node> { leaveRestaurant });
        Selector checkSeatAvailability = new Selector(new List<Node> { hasChair, noAvailableChair });

        Sequence enterRestaurant = new Sequence(new List<Node> { goToFrontDoor, checkSeatAvailability });
        return enterRestaurant;
    }
    private void Update()
    {
        tree.Update();
    }

    private Node.NodeState GoToFrontDoor()
    {
        Debug.Log("Dang di toi cua chinh");
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState GoToEmptyChair()
    {
        if (isEmtyChair == true)
        {
            Debug.Log("Dang tien toi ghe");
            return Node.NodeState.SUCCESS;
        }
        else
        {
            Debug.Log(" Khong co ghe trong");
            return Node.NodeState.FAILURE;
        }
    }

    private Node.NodeState IsWaiting()
    {
        Debug.Log("Dang cho mon");
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState LeaveRestaurant()
    {
        Debug.Log("Roi nha hang");
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState Pay()
    {
        if (isServed == true)
        {
            Debug.Log("Dang tinh tien");
            return Node.NodeState.SUCCESS;
        }
        else { return Node.NodeState.FAILURE; }
    }
    private Node.NodeState GoToLocation(Vector3 destination)
    {
        float distance = Vector3.Distance(transform.position, destination);

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
        else if (distance < 2)
        {
            state = ActionState.Idle;
            return Node.NodeState.SUCCESS;
        }
        return Node.NodeState.RUNNING;
    }
}
