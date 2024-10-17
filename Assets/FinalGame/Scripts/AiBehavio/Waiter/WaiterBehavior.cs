using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.AI;

public class WaiterBehavior : MonoBehaviour
{
    public GameObject kitchenTable;
    public GameObject customerTable;

    public bool hasCustomer;
    public bool hasOderFromKitchen;

    private BehaviorTree tree;
    private NavMeshAgent agent;

    public enum ActionState
    {
        Idle,
        Working 
    }

    private Node.NodeState treeStatus = Node.NodeState.RUNNING;
    public ActionState state = ActionState.Idle;

    private void Awake()
    {

    }
    private void Start()
    {

        Node waiterBTRoot = InitializeBehaviorTree();
        tree = new BehaviorTree(waiterBTRoot);
    }


    private Node InitializeBehaviorTree()
    {
        
        Leaf pickUpFood = new Leaf(PickUpFood);
        Leaf checkKitchenOder = new Leaf(CheckKitchenOder);
        Leaf goToKitchenTable = new Leaf(GoToKitchenTable);
        Leaf checkCustomerOder = new Leaf(CheckCustomerOder);
        Leaf goToCustomerTable = new Leaf(GoToCustomerTable);
        Leaf signalTransmission = new Leaf(SignalTransmission);
        Leaf idle = new Leaf(Idle);


        Sequence hasCustomer = new Sequence(new List<Node> { checkCustomerOder, goToCustomerTable, signalTransmission });
        Sequence hasOderInKitchen = new Sequence(new List<Node> { checkKitchenOder, goToKitchenTable, pickUpFood, goToCustomerTable });
        Sequence Free = new Sequence(new List<Node> { idle });

        Selector jobState = new Selector(new List<Node> { hasCustomer, hasOderInKitchen, Free });
        return jobState;



    }

    private void Update()
    {
        tree.Update();
    }

    private Node.NodeState CheckCustomerOder()
    {
        if (hasCustomer == true)
        {
            Debug.Log("Có khách gọi món");
            return Node.NodeState.SUCCESS;
        }
        else
        {
            Debug.Log("Không có khách gọi món");
            return Node.NodeState.FAILURE;
        }
    }
    private Node.NodeState CheckKitchenOder()
    {
        if (hasOderFromKitchen == true)
        {
            Debug.Log("Có món ăn từ bếp");
            return Node.NodeState.SUCCESS;
        }
        else
        {
            Debug.Log("Không có món ăn từ bếp");
            return Node.NodeState.FAILURE;
        }
    }


    private Node.NodeState GoToCustomerTable()
    {
        //var status = GoToLocation(customerTable.transform.position);
        //return status;
        Debug.Log("Đang đi tới bàn của khách");
        return Node.NodeState.SUCCESS;
    }
    private Node.NodeState SignalTransmission()
    {
        Debug.Log("Đang rảnh truyền tính hiệu");
        return Node.NodeState.SUCCESS;
    }

    private Node.NodeState PickUpFood()
    {
        Debug.Log("Đang cầm đồ ăn");
        return Node.NodeState.SUCCESS;
    }
    private Node.NodeState GoToKitchenTable()
    {
        //var status = GoToLocation(kitchenTable.transform.position);
        //return status;
        Debug.Log("Đang đi tới bàn bếp");
        return Node.NodeState.SUCCESS;
    }
    //Khi dang ranh roi
    private Node.NodeState Idle()
    {
        Debug.Log("Đang rảnh rỗi");
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
