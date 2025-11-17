using UnityEngine;

public class ReplayButtonFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    void Update()
    {
        if (target != null)
            transform.position = target.position + offset;
    }
}
