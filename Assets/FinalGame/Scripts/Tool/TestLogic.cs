using System;
using System.Collections.Generic;
using System.Linq;

class MenuItem
{
    public int Price { get; set; }
    public double CookTime { get; set; }
}

class Equipment
{
    public int Level { get; set; }
    public double SpeedBonus { get; set; }
}

class Restaurant
{
    private int money = 100;
    private int customers = 0;
    private Dictionary<string, MenuItem> menu;
    private Dictionary<string, Equipment> equipment;
    private Random random = new Random();

    public Restaurant()
    {
        menu = new Dictionary<string, MenuItem>
        {
            {"Burger", new MenuItem {Price = 5, CookTime = 2}},
            {"Pizza", new MenuItem {Price = 8, CookTime = 3}},
            {"Salad", new MenuItem {Price = 4, CookTime = 1}}
        };

        equipment = new Dictionary<string, Equipment>
        {
            {"Stove", new Equipment {Level = 1, SpeedBonus = 0.1}},
            {"Oven", new Equipment {Level = 1, SpeedBonus = 0.1}}
        };
    }

    public string ServeCustomer()
    {
        string order = menu.Keys.ElementAt(random.Next(menu.Count));
        double cookTime = menu[order].CookTime;

        foreach (var equip in equipment.Values)
        {
            cookTime -= cookTime * equip.SpeedBonus;
        }

        money += menu[order].Price;
        customers++;

        return $"Served {order} in {cookTime:F1} seconds";
    }

    public string UpgradeEquipment(string equipmentName)
    {
        if (equipment.ContainsKey(equipmentName))
        {
            int cost = equipment[equipmentName].Level * 50;
            if (money >= cost)
            {
                money -= cost;
                equipment[equipmentName].Level++;
                equipment[equipmentName].SpeedBonus += 0.1;
                return $"Upgraded {equipmentName} to level {equipment[equipmentName].Level}";
            }
            else
            {
                return "Not enough money for upgrade";
            }
        }
        else
        {
            return "Invalid equipment";
        }
    }

    public string AddMenuItem(string name, int price, double cookTime)
    {
        if (!menu.ContainsKey(name))
        {
            menu[name] = new MenuItem { Price = price, CookTime = cookTime };
            return $"Added {name} to the menu";
        }
        else
        {
            return "Item already exists in the menu";
        }
    }

    public string DisplayStatus()
    {
        return $"Money: ${money}, Customers served: {customers}";
    }
}

class Program
{
    static void Main()
    {
        Restaurant restaurant = new Restaurant();

        for (int day = 1; day <= 5; day++)
        {
            Console.WriteLine($"Day {day}:");
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(restaurant.ServeCustomer());
            }

            if (day % 2 == 0)
            {
                Console.WriteLine(restaurant.UpgradeEquipment("Stove"));
            }

            if (day == 3)
            {
                Console.WriteLine(restaurant.AddMenuItem("Pasta", 7, 2));
            }

            Console.WriteLine(restaurant.DisplayStatus());
            Console.WriteLine();
        }
    }
}