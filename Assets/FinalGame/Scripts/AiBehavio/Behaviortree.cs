public class Behaviortree : Node
{
    public Behaviortree()
    {
        name = "Root";
    }
    public Behaviortree(string name)
    {
        this.name = name;
    }
    public override Status Process()
    {
        return children[currentChild].Process();
    }
}
