public class ActionNode : Node
{
    public override NodeState Evaluate()
    {
        // Logic th?c hi?n hành ??ng c?a node ? ?ây.
        // Ví d? ki?m tra xem m?t ??i t??ng có di chuy?n thành công không
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
