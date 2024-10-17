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

    private NavMeshAgent agent;
    private BehaviorTree tree;

    private Node.NodeState treeStatus = Node.NodeState.RUNNING;
    public ActionState state = ActionState.Idle;

    public enum ActionState
    {
        Idle,
        Working
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }


    private void InitializeBehaviorTree()
    {
        Leaf processPayment = new Leaf(Pay);
        Leaf leaveRestaurant = new Leaf(LeaveRestaurant);
        Leaf isWaiting = new Leaf(IsWaiting);
        Leaf goToEmptyChair = new Leaf(GoToEmptyChair);
        Leaf goToFrontDoor = new Leaf(GoToFrontDoor);

        Sequence hasBeenServed = new Sequence(new List<Node> { processPayment, leaveRestaurant});
        Sequence notBeenServed = new Sequence(new List<Node> { isWaiting});
        Selector orderCheck = new Selector(new List<Node> { hasBeenServed, notBeenServed });
        Sequence hasChair = new Sequence(new List<Node>  {goToEmptyChair, orderCheck });
        Sequence noAvailableChair = new Sequence(new List<Node> { leaveRestaurant});
        Selector checkSeatAvailability = new Selector(new List<Node> { hasChair, noAvailableChair});

        Sequence enterRestaurant = new Sequence(new List<Node> {goToFrontDoor , checkSeatAvailability });
    }

    private Node.NodeState GoToFrontDoor()
    {
        throw new NotImplementedException();
    }

    private Node.NodeState GoToEmptyChair()
    {
        throw new NotImplementedException();
    }

    private Node.NodeState IsWaiting()
    {
        throw new NotImplementedException();
    }

    private Node.NodeState LeaveRestaurant()
    {
        throw new NotImplementedException();
    }

    private Node.NodeState Pay()
    { 
    
        return Node.NodeState.SUCCESS;
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
