public class ActionNode : Node
{
    public override NodeState Evaluate()
    {
        // Logic th?c hi?n h�nh ??ng c?a node ? ?�y.
        // V� d? ki?m tra xem m?t ??i t??ng c� di chuy?n th�nh c�ng kh�ng
        bool actionSuccess = true;

        if (actionSuccess)
        {
            _state = NodeState.SUCCESS;
            return _state;
        }
        else
        {
            _state = NodeState.FAILURE;
            return _state;
        }
    }
}
