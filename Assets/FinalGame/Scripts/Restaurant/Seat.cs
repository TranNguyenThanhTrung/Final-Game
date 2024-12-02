using UnityEngine;
using System.Collections.Generic;

public class Seat : MonoBehaviour
{
    public int seatID;
    [SerializeField] public CustommerBehavior currentCustomer;
    [SerializeField] private Transform chairTransform;
    [SerializeField] private Transform sitPosition;
    [SerializeField] private Vector3 seatRotation = Vector3.zero;


    [SerializeField] private bool availableChair = true;
    public bool hasCustommer = false;
    public bool AvailableChair
    {
        get { return availableChair; }
        set
        {
            availableChair = value;
            OnSeatStatusChanged();
        }
    }
    
    public Vector3 SitPosition => sitPosition != null ? sitPosition.position : transform.position;
    public Quaternion TargetRotation => Quaternion.Euler(seatRotation);
    public Transform TableTransform => chairTransform;
    public event SeatStatusChanged OnSeatStatusChanged;
    public delegate void SeatStatusChanged();

    public void Awake()
    {
        chairTransform = gameObject.GetComponent<Transform>();

    }
    public void OnValidate()
    {
        if (sitPosition == null)
        {
            GameObject sitPos = new GameObject("SitPosition");
            sitPos.transform.parent = transform;
            sitPos.transform.localPosition = Vector3.zero;
            sitPosition = sitPos.transform;
        }
    }
    public void OnEnable()
    {
        RestaurantManager.Instance?.RegisterSeat(this);
    }
    public void OnDisable()
    {
        RestaurantManager.Instance?.UnregisterSeat(this);
    }
    public void OnDrawGizmos()
    {
        // Vẽ visual trong editor để dễ nhìn trạng thái ghế
        Gizmos.color = AvailableChair ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
    public bool TryOccupySeat(CustommerBehavior customer)
    {
        if (AvailableChair)
        {
            AvailableChair = false;
            currentCustomer = customer;
            return true;
        }
        return false;
    }
    public void ReleaseSeat()
    {
        if (!AvailableChair)
        {
            AvailableChair = true;
            hasCustommer = false;
            currentCustomer = null;
        }
    }
    public float GetDistanceToSeat(Vector3 position)
    {
        return Vector3.Distance(position, transform.position);
    }
    public CustommerBehavior GetCurrentCustomer()
    {
        return currentCustomer;
    }
}