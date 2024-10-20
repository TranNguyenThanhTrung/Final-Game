﻿using UnityEngine;
using static ChefBehavior2;
// TableManager.cs (đã có từ trước)
[CreateAssetMenu(fileName = "TableManager", menuName = "Restaurant/Table Manager")]
public class TableManager : ScriptableObject
{
    public TableData appleTable;
    public TableData burgerTable;
    public TableData waterTable;
    public TableData serveTable;
    //public TableData kitchenTable;

    public TableData GetTableByFoodType(FoodType foodType)
    {
        switch (foodType)
        {
            case FoodType.Apple: return appleTable;
            case FoodType.Burger: return burgerTable;
            case FoodType.Water: return waterTable;
            default: return null;
        }
    }
}
