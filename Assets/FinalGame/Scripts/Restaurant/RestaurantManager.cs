using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestaurantManager : MonoBehaviour
{
    public int Level { get; private set; }
    public decimal Money { get; private set; }
    public List<Table> Tables { get; private set; }
    public List<Staff> Staff { get; private set; }
    public Queue<Order> CurrentOrders { get; private set; }

    private void ProcessOrders() { }
    private void CheckLevelUp() { }
    public void UnlockTable(int tableIndex) { }
   // public void AddMenuItem(MenuItem item) { }
    public void HireStaff(Staff newStaff) { }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
