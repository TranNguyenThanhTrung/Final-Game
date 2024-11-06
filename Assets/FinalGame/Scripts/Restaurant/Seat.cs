using UnityEngine;
using System.Collections.Generic;

public class Seat : MonoBehaviour
{
    [SerializeField]private bool availableChair = true;
    [SerializeField] private CustommerBehavior currentCustomer;
    [SerializeField] private Transform tableTransform; // Reference đến bàn
    [SerializeField] private Transform sitPosition;
    [SerializeField] private Vector3 seatRotation = Vector3.zero;

    public Vector3 SitPosition => sitPosition != null ? sitPosition.position : transform.position;

    public Quaternion TargetRotation => Quaternion.Euler(seatRotation);

    public Transform TableTransform => tableTransform;

    public bool AvailableChair
    {
        get { return availableChair; }
        set
        {
            availableChair = value;
            OnSeatStatusChanged();
        }
    }

    // Event để thông báo khi trạng thái ghế thay đổi
    public delegate void SeatStatusChanged();
    public event SeatStatusChanged OnSeatStatusChanged;
    private void OnValidate()
    {
        // Tự động tạo sitPosition nếu chưa có
        if (sitPosition == null)
        {
            GameObject sitPos = new GameObject("SitPosition");
            sitPos.transform.parent = transform;
            sitPos.transform.localPosition = Vector3.zero;
            sitPosition = sitPos.transform;
        }
    }

    // Kiểm tra và đặt ghế cho khách
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

    // Giải phóng ghế
    public void ReleaseSeat()
    {
        if (!AvailableChair)
        {
            AvailableChair = true;
            currentCustomer = null;
        }
    }

    // Kiểm tra xem ai đang ngồi ở ghế này
    public CustommerBehavior GetCurrentCustomer()
    {
        return currentCustomer;
    }

    // Kiểm tra khoảng cách từ một vị trí đến ghế
    public float GetDistanceToSeat(Vector3 position)
    {
        return Vector3.Distance(position, transform.position);
    }

    private void OnDrawGizmos()
    {
        // Vẽ visual trong editor để dễ nhìn trạng thái ghế
        Gizmos.color = AvailableChair ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}