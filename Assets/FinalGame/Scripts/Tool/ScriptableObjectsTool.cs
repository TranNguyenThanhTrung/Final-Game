//using UnityEngine;
//using System;
//using static ChefBehavior2;
//using UnityEngine.Tilemaps;

//[CreateAssetMenu(fileName = "OrderChannel", menuName = "Restaurant/Order Channel")]
//public class OrderChannel : ScriptableObject
//{

//    public event Action<FoodType> OnOrderCreated;
//    public event Action OnOrderCompleted;

//    public void CreateOrder(FoodType foodType)
//    {
//        OnOrderCreated?.Invoke(foodType);
//    }

//    public void CompleteOrder()
//    {
//        OnOrderCompleted?.Invoke();
//    }
//}
