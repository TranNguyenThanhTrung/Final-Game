public class BehaviorTree
{
    private Node rootNode;  // Gốc của cây hành vi

    public BehaviorTree(Node rootNode)
    {
        this.rootNode = rootNode;
    }

    // Hàm này được gọi mỗi frame để đánh giá cây
    public void Update()    
    {
        if (rootNode != null)
        {
            rootNode.Evaluate();
        } 
    }
}
