

public abstract class Node
{
    public enum NodeState
    {
        SUCCESS,
        FAILURE,
        RUNNING
    }
    protected NodeState state;

    public NodeState State => state;
    public abstract NodeState Evaluate();
}
