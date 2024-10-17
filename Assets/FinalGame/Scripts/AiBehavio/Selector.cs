using System.Collections.Generic;

public class Selector : Node
{

    private List<Node> childNodes = new List<Node>();

    public Selector(List<Node> nodes)
    {
        childNodes = nodes;
    }

    public override NodeState Evaluate()
    {
        foreach (Node node in childNodes)
        {
            switch (node.Evaluate())
            { 
                case NodeState.SUCCESS:
                    state = NodeState.SUCCESS;
                    return state;
                case NodeState.RUNNING:
                    state = NodeState.RUNNING;
                    return state;
            }
        }
        state = NodeState.FAILURE;
        return state;
    }
}
