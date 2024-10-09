public class Leaf : Node
{
    public delegate Status Tick();
    private Tick ProcessMethod;

    public Leaf() { }

    public Leaf(string name, Tick processMeThod)
    {
        this.name = name;
        this.ProcessMethod = processMeThod;
    }
    public override Status Process()
    {
        if (ProcessMethod != null)
        {
            return ProcessMethod();
        }
        return Status.FAILURE;
    }
}

