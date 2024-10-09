using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class ChefBehavior : MonoBehaviour
{
    public float currentTime = 0;
    public float foodBaseTime;

    public NavMeshAgent agent;

    private Behaviortree tree;

    public GameObject appleTable;
    public GameObject bugerTable;
    public GameObject waterTable;
    public GameObject serveTable;
    public GameObject kitChenTable;


    public enum typeFood
    {
        BUGER = 0,
        WATER = 1,
        APPLE = 2
    }
    public bool hasOder;

    public enum ActionState
    {
        IDLE, WORKING
    }

    public ActionState state = ActionState.IDLE;
    Node.Status treeStatus = Node.Status.RUNNING;
    private void Awake()
    {
        appleTable = GameObject.FindGameObjectWithTag("AppleTable");
        waterTable = GameObject.FindGameObjectWithTag("WaterTable");
        bugerTable = GameObject.FindGameObjectWithTag("BugerTable");
        serveTable = GameObject.FindGameObjectWithTag("ServeTable");
        agent = GetComponent<NavMeshAgent>();
    }
    void Start()
    {
        currentTime = foodBaseTime;
        tree = new Behaviortree();
        Sequence service = new Sequence("service");

        //Leaf getApple = new Leaf("Go to FrontDoor", GetApple);
        //Leaf getWater = new Leaf("Go to FrontDoor", GetWater);
        //Leaf makeBuger = new Leaf("Go to FrontDoor", MakeBuger);
        Leaf cookingDone = new Leaf("Cooking done",()=> GoKooking(waterTable));
        Leaf serviceCustomer = new Leaf("Go to FrontDoor", GoToSreveFood);


        Selector cooking = new Selector("Cooking");
        cooking.AddChild(cookingDone);
        //cooking.AddChild(makeBuger);
        //cooking.AddChild(getWater);
        //cooking.AddChild(getApple);

        service.AddChild(cooking);
        service.AddChild(serviceCustomer);
        tree.AddChild(service);
    }
    void Update()
    {
        if (treeStatus != Node.Status.SUCCESS)
        {
            treeStatus = tree.Process();
        }

    }

    private Node.Status HasOder(typeFood typeFood)
    {
        if (hasOder)
        {
            switch (typeFood)
            {
                case typeFood.BUGER:
                    foodBaseTime = 3;
                    GoKooking(bugerTable);
                    Debug.Log("Lam BUGER");
                    break;
                case typeFood.WATER:
                    foodBaseTime = 1;
                    GoKooking(waterTable);
                    Debug.Log("Dang lay Watter");
                    break;
                case typeFood.APPLE:
                    foodBaseTime = 1;
                    GoKooking(appleTable);
                    Debug.Log("Dang lay Apple");
                    break;
            }
            return Node.Status.FAILURE;
        }
        return Node.Status.SUCCESS;
    }
    Node.Status GoKooking(GameObject location)
    {
        var status = GoToLocation(location.transform.position);
        return status;
    }

    Node.Status Cooking()
    {
        HasOder(typeFood.BUGER);
        return Node.Status.RUNNING;
    }

    //private Node.Status GetApple()
    //{
    //    var status = GoKooking(appleTable);
    //    return status;
    //}

    //private Node.Status GetWater()
    //{
    //    var status = GoKooking(waterTable); ;
    //    return status;
    //}

    //private Node.Status MakeBuger()
    //{
    //    var status = GoKooking(bugerTable); ;
    //    return status;
    //}

    Node.Status GoToSreveFood()
    {
        var status = GoToLocation(serveTable.transform.position);
        return status;
    }


    Node.Status GoToLocation(Vector3 destination)
    {
        float distance = Vector3.Distance(transform.position, destination);
        if (state == ActionState.IDLE)
        {
            agent.SetDestination(destination);
            state = ActionState.WORKING;
        }
        else if (Vector3.Distance(agent.pathEndPosition, destination) >= 2)
        {
            state = ActionState.IDLE;
            return Node.Status.FAILURE;
        }
        else if (distance < 2)
        {
            currentTime -= Time.deltaTime;
            if (currentTime < 0)
            {
                state = ActionState.IDLE;
                currentTime = foodBaseTime;
                return Node.Status.SUCCESS;
            }
        }
        return Node.Status.RUNNING;
    }

}
