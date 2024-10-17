using System.Collections.Generic;

public class SelectorNode : Node
{
    private List<Node> childNodes = new List<Node>();

    public SelectorNode(List<Node> nodes)
    {
        childNodes = nodes;
    }

    public override NodeState Evaluate()
    {
        foreach (Node node in childNodes)
        {
            NodeState result = node.Evaluate();
            if (result == NodeState.SUCCESS)
            {
                _state = NodeState.SUCCESS;
                return _state;
            }
        }
        _state = NodeState.FAILURE;
        return _state;
    }
}
