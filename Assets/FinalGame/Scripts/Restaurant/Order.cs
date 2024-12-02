public class Order
{
    public CustommerBehavior Customer { get; set; }
    public Seat CustomerSeat { get; set; }
    public RestaurantManager restaurantManager { get; set; }
    public Dish OrderedDish { get; set; }
    public float OrderTime { get; set; }
    public bool IsCompleted { get; set; }
}