using System.Collections.Generic;

// ── 3 trạng thái mỗi Node phải trả về ───────────────────────
public enum NodeState { Success, Failure, Running }

// ── Base class cho tất cả Node ───────────────────────────────
public abstract class Node
{
    protected NodeState state;
    public NodeState State => state;
    public abstract NodeState Evaluate();
}

// ── Sequence (AND): tất cả con phải Success ──────────────────
public class Sequence : Node
{
    private List<Node> _children;
    public Sequence(List<Node> children) => _children = children;

    public override NodeState Evaluate()
    {
        bool anyRunning = false;

        foreach (var node in _children)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:
                    state = NodeState.Failure;
                    return state;           // 1 con Fail → cả Sequence Fail
                case NodeState.Running:
                    anyRunning = true;
                    continue;              // Đang chạy → tiếp tục check con sau
                case NodeState.Success:
                    continue;              // Con này xong → check con tiếp
            }
        }

        state = anyRunning ? NodeState.Running : NodeState.Success;
        return state;
    }
}

// ── Selector (OR): tìm đến khi có 1 con Success ──────────────
public class Selector : Node
{
    private List<Node> _children;
    public Selector(List<Node> children) => _children = children;

    public override NodeState Evaluate()
    {
        foreach (var node in _children)
        {
            switch (node.Evaluate())
            {
                case NodeState.Success:
                    state = NodeState.Success;
                    return state;           // 1 con Success → cả Selector Success
                case NodeState.Running:
                    state = NodeState.Running;
                    return state;           // Đang chạy → dừng, chờ frame sau
                case NodeState.Failure:
                    continue;              // Con này Fail → thử con tiếp theo
            }
        }

        state = NodeState.Failure;          // Tất cả Fail → Selector Fail
        return state;
    }
}

// ── ConditionNode: kiểm tra điều kiện (true/false) ───────────
public class ConditionNode : Node
{
    private System.Func<bool> _condition;
    public ConditionNode(System.Func<bool> condition) => _condition = condition;

    public override NodeState Evaluate()
    {
        state = _condition() ? NodeState.Success : NodeState.Failure;
        return state;
    }
}

// ── ActionNode: thực hiện hành động, trả về NodeState ────────
public class ActionNode : Node
{
    private System.Func<NodeState> _action;
    public ActionNode(System.Func<NodeState> action) => _action = action;

    public override NodeState Evaluate()
    {
        state = _action();
        return state;
    }
}