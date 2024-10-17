using System.Collections.Generic;

public class SequenceNode : Node
{
    private List<Node> childNodes = new List<Node>();

    public SequenceNode(List<Node> nodes)
    {
        childNodes = nodes;
    }

    public override NodeState Evaluate()
    {
        foreach (Node node in childNodes)
        {
            NodeState result = node.Evaluate();
            if (result == NodeState.FAILURE)
            {
                _state = NodeState.FAILURE;
                return _state;
            }
        }
        _state = NodeState.SUCCESS;
        return _state;
    }
}
