using UnityEngine;

public interface IOpenableDoor
{
    bool CanOpenFor(GameObject entity);
    void OpenForEntity(GameObject entity);
}
