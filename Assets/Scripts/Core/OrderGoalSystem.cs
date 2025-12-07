using UnityEngine;

public class OrderGoalSystem : MonoBehaviour
{
    public int TargetOrders { get; private set; }
    public int CompletedOrders { get; private set; }

    public void Initialize(int target)
    {
        TargetOrders = target;
        CompletedOrders = 0;
    }

    public void AddOrderSuccess()
    {
        CompletedOrders++;
    }

    public bool IsGoalReached()
    {
        return CompletedOrders >= TargetOrders;
    }
}
