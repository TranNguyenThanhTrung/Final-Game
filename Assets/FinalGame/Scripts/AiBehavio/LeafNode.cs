using System;

public class LeafNode : Node
{
    private Func<NodeState> action;

    public LeafNode(Func<NodeState> action)
    {
        this.action = action;
    }

    public override NodeState Evaluate()
    {
        return action();
    }
}
