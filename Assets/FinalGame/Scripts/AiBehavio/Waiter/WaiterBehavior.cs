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

    public bool hasCustomer = false;
    public bool hasOderFromKitchen = false;

    private BehaviorTree tree;
    private NavMeshAgent agent;

    public enum ActionState
    {
        Idle,
        Working
    }

    private NodeState treeStatus = NodeState.RUNNING;
    public ActionState state = ActionState.Idle;
    private void InitializeBehaviorTree()
    {
        WaiterBehavior waiterBehavior = new WaiterBehavior();
        SelectorNode jobState = new SelectorNode( new List<Node> {});
        
        
        LeafNode pickUpFood = new LeafNode(waiterBehavior.PickUpFood);
        LeafNode checkKitchenOder = new LeafNode(waiterBehavior.CheckKitchenOder);
        LeafNode goToKitchenTable = new LeafNode(waiterBehavior.GoToKitchenTable);
        LeafNode checkCustomerOder = new LeafNode(waiterBehavior.CheckCustomerOder);
        LeafNode goToCustomerTable = new LeafNode(waiterBehavior.GoToCustomerTable);
        LeafNode signalTransmission = new LeafNode(waiterBehavior.SignalTransmission);
        LeafNode idle = new LeafNode(waiterBehavior.Idle);

        SequenceNode hasCustomer = new SequenceNode(new List<Node> { checkCustomerOder,goToCustomerTable, signalTransmission});
        SequenceNode hasOderInKitchen = new SequenceNode(new List<Node> {checkKitchenOder,goToKitchenTable,pickUpFood,goToCustomerTable });
        SequenceNode Free = new SequenceNode(new List<Node> {idle});

        //Add child cho JobState
        

        
    }

    private void Update()
    {
        InitializeBehaviorTree();
        
    }
    private NodeState SignalTransmission()
    {
        Debug.Log("Đang rảnh truyền tính hiệu");
        return NodeState.SUCCESS;
    }

    private NodeState Idle()
    {
        Debug.Log("Đang rảnh rỗi");
        return NodeState.SUCCESS;
    }

    private NodeState PickUpFood()
    {
        Debug.Log("Đang cầm đồ ăn");
        return  NodeState.SUCCESS;
    }

    private NodeState GoToCustomerTable()
    {
        //var status = GoToLocation(customerTable.transform.position);
        //return status;
        Debug.Log("Đang đi tới bàn của khách");
        return NodeState.SUCCESS;
    }

    private NodeState GoToKitchenTable()
    {
        //var status = GoToLocation(kitchenTable.transform.position);
        //return status;
        Debug.Log("Đang đi tới bàn bếp");
        return NodeState.SUCCESS;
    }

    private NodeState CheckCustomerOder()
    {
        if (hasCustomer == true)
        {
            Debug.Log("Có khách gọi món");
            return NodeState.SUCCESS;
        }
        else
        {
            Debug.Log("Không có khách gọi món");
            return NodeState.FAILURE; }
    } 
    private NodeState CheckKitchenOder()
    {
        if (hasOderFromKitchen == true)
        {
            Debug.Log("Có món ăn từ bếp");
            return NodeState.SUCCESS;
        }
        else {
            Debug.Log("Không có món ăn từ bếp");
            return NodeState.FAILURE; }
    }

    private NodeState GoToLocation(Vector3 destination)
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
            return NodeState.FAILURE;
        }
        else if (distance < 2)
        {
            state = ActionState.Idle;
            return NodeState.SUCCESS;
        }
        return NodeState.RUNNING;
    }
}
