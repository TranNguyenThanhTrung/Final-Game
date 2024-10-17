using UnityEngine.AI;
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using System.Collections;

public class ChefBehavior : MonoBehaviour
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

    public bool Oder;
    public bool foodDone;
    public bool pick;

    private BehaviorTree tree;
    private NavMeshAgent agent;

    private Node.NodeState treeStatus = Node.NodeState.RUNNING;
    public ActionState state = ActionState.Idle;

    private Node InitializeBehaviorTree()
    {

        Leaf goToKitchen = new Leaf(GoToKitchen);
        Leaf idle = new Leaf(Idle);
        Leaf bringFoodToTable = new Leaf(BringFoodToTable);
        Leaf pickUpFood = new Leaf(PickUpFood);


        Sequence free = new Sequence(new List<Node> { idle });
        Sequence serverOder = new Sequence(new List<Node> { pickUpFood, bringFoodToTable });
        Selector foodState = new Selector(new List<Node> { serverOder, idle });
        Sequence hasOder = new Sequence(new List<Node> { goToKitchen, foodState });

        Selector jobState = new Selector(new List<Node> { hasOder, free });

        return jobState;
    }
    private void Start()
    {
        Node chefBTRoot = InitializeBehaviorTree();
        tree = new BehaviorTree(chefBTRoot);
    }

    private void Update()
    {
        tree.Update();
    }

    private Node.NodeState PickUpFood()
    {
        if (foodDone)
        {
            Debug.Log("Da cam mon an");
           
            return Node.NodeState.SUCCESS;
            
        }
        else
        {
            return Node.NodeState.FAILURE;
        }
    }

    private Node.NodeState BringFoodToTable()
    {

            Debug.Log("Dang dem do an ra");
            return Node.NodeState.SUCCESS;
    }

    private Node.NodeState Idle()
    {
        Debug.Log("Dang ranh roi");
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState GoToKitchen()
    {
        if (Oder)
        {
            Debug.Log("Dang vao bep");
            return Node.NodeState.SUCCESS;
        }
        else
        {
            return Node.NodeState.FAILURE;
        }
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