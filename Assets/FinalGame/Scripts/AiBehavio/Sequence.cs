using System.Collections.Generic;

public class Sequence : Node
{
    private List<Node> childNodes = new List<Node>();

    public Sequence(List<Node> nodes)
    {
        childNodes = nodes;
    }

    public override NodeState Evaluate()
    {
        bool isAnyNodeRunning = false;
        foreach (Node node in childNodes)
        {
            switch (node.Evaluate())
            {
                case NodeState.FAILURE:
                    state = NodeState.FAILURE;
                    return state;
                case NodeState.RUNNING:
                    isAnyNodeRunning = true;
                    break;
            }
        }
        state = isAnyNodeRunning ? NodeState.RUNNING : NodeState.SUCCESS;
        return state;
    }
}
