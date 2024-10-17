public enum NodeState
{
    SUCCESS,
    FAILURE,
    RUNNING
}

public abstract class Node
{
    protected NodeState _state;

    public NodeState state
    {
        get { return _state; }
    }

    public abstract NodeState Evaluate();
}
