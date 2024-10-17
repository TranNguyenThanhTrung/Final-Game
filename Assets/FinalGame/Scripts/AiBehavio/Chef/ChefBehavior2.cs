//using UnityEngine.AI;
//using UnityEngine;
//using System.Collections.Generic;
//using System;
//using Unity.VisualScripting;

//public class ChefBehavior2 : MonoBehaviour
//{
//    public enum FoodType
//    {
//        Burger,
//        Water,
//        Apple,
//        None
//    }
//    [Header("ScriptableObject References")]
//    [SerializeField] private TableManager tableManager;
//    [SerializeField] private OrderChannel orderChannel;

//    [Header("State Variables")]
//    public float currentTime = 0;
//    public float foodBaseTime;
//    public bool hasOrder;

//    public ActionState state = ActionState.Idle;

//    private NavMeshAgent agent;
//    private Behaviortree tree;
//    private Dictionary<FoodType, TableData> tableLookup = new Dictionary<FoodType, TableData>();
//    private NodeState treeStatus = NodeState.RUNNING;
//    public FoodType currentOrderType;
//    public FoodType customerOderType;

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
        
//        orderChannel.OnOrderCreated += HandleNewOrder;
//    }

//    private void OnDisable()
//    {
        
//        orderChannel.OnOrderCreated -= HandleNewOrder;
//    }

//    private void InitializeTables()
//    {
//        tableLookup.Clear();
//        tableLookup.Add(FoodType.Burger, tableManager.burgerTable);
//        tableLookup.Add(FoodType.Water, tableManager.waterTable);
//        tableLookup.Add(FoodType.Apple, tableManager.appleTable);
//    }

//    void Start()
//    {
//        InitializeBehaviorTree();
//    }

//    private void InitializeBehaviorTree()
//    {
//        tree = new Behaviortree();
//        SelectorNode service = new SelectorNode("Service");

//        LeafNode checkOrder = new LeafNode("Check Order", CheckOrder);
//        LeafNode serveCustomer = new LeafNode("Serve Customer", ServeFood);

//        SelectorNode cooking = new SelectorNode("Cooking");
//        LeafNode cookBurger = new LeafNode("Cook Burger", () => CookFood(FoodType.Burger));
//        LeafNode getApple = new LeafNode("Get Apple", () => CookFood(FoodType.Apple));
//        LeafNode getWater = new LeafNode("Get Water", () => CookFood(FoodType.Water));

//        cooking.AddChild(cookBurger);
//        cooking.AddChild(getWater);
//        cooking.AddChild(getApple);

//        service.AddChild(checkOrder);
//        service.AddChild(cooking);
//        service.AddChild(serveCustomer);

//        tree.AddChild(service);
//    }

    

//    void Update()
//    {
//        if (treeStatus != Node.Status.SUCCESS)
//        {
//            treeStatus = tree.Process();
//        }
//    }

//    private void HandleNewOrder(FoodType foodType)
//    {
//        if (!hasOrder)  
//        {
//            hasOrder = true;
//            currentOrderType = foodType;
//            treeStatus = Node.Status.RUNNING;
//            Debug.Log($"Received new order: {foodType}");
//        }
//    }

//    private Node.Status CheckOrder()
//    {
//        if (hasOrder)
//        {
//            currentOrderType = currentOrderType;
//        }

//        Debug.Log("checkOder");
//        return hasOrder ? Node.Status.SUCCESS : Node.Status.FAILURE;
//    }

//    private Node.Status CookFood(FoodType foodType)
//    {
//        if (currentOrderType != foodType)
//        {
//            return Node.Status.FAILURE;
//        }

//        if (!tableLookup.TryGetValue(foodType, out TableData targetTable))
//        {
//            Debug.LogError($"No table found for food type: {foodType}");
//            return Node.Status.FAILURE;
//        }

//        var status = GoToLocation(targetTable.tablePrefab.transform.position);

//        if (status == Node.Status.SUCCESS)
//        { 
//            currentTime = targetTable.processTime;
//        }

//        return status;
//    }

//    private Node.Status ServeFood()
//    {
//        var status = GoToLocation(tableManager.serveTable.tablePrefab.transform.position);
//        if (status == Node.Status.SUCCESS)
//        {
            
//            hasOrder = false;
//            orderChannel.CompleteOrder();
//            return Node.Status.SUCCESS;
//        }
//        return status;
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
//            currentTime -= Time.deltaTime;
//            if (currentTime < 0)
//            {
//                state = ActionState.Idle;
//                currentTime = foodBaseTime;
//                return Node.Status.SUCCESS;
//            }
//        }
//        return Node.Status.RUNNING;
//    }
//}