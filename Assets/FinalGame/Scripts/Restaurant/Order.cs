public class Order
{
    public CustommerBehavior Customer { get; set; }
    public Dish OrderedDish { get; set; }
    public float OrderTime { get; set; }
    public bool IsCompleted { get; set; }
}