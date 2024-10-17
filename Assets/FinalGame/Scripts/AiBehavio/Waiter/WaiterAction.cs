using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaiterAction
{
    bool hasOderFromKitchen;
    bool hasCustomer;
    public Node.NodeState SignalTransmission()
    {
        Debug.Log("?ang r?nh truy?n tính hi?u");
        return Node.NodeState.SUCCESS;
    }

    public Node.NodeState Idle()
    {
        Debug.Log("Dang ranh roi");
        return Node.NodeState.SUCCESS;
    }

    public Node.NodeState PickUpFood()
    {
        Debug.Log("Dang cam do an");
        return Node.NodeState.SUCCESS;
    }

    public Node.NodeState GoToCustomerTable()
    {
        //var status = GoToLocation(customerTable.transform.position);
        //return status;
        Debug.Log("Di den ban cua khach");
        return Node.NodeState.SUCCESS;
    }

    public Node.NodeState GoToKitchenTable()
    {
        //var status = GoToLocation(kitchenTable.transform.position);
        //return status;
        Debug.Log("Di den ban bep");
        return Node.NodeState.SUCCESS;
    }
    public Node.NodeState CheckCustomerOder()
    {
        if (hasCustomer == true)
        {
            Debug.Log("Co khach goi mon");
            return Node.NodeState.SUCCESS;
        }
        else
        {
            Debug.Log("Khongo co khach goi mon");
            return Node.NodeState.FAILURE;
        }
    }
    public Node.NodeState CheckKitchenOder()
    {
        if (hasOderFromKitchen == true)
        {
            Debug.Log("Co mon an tu bep");
            return Node.NodeState.SUCCESS;
        }
        else
        {
            Debug.Log("Khong Co mon an tu bep");
            return Node.NodeState.FAILURE;
        }
    }
}
