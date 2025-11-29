using UnityEngine;

public interface IOpenableDoor
{
    bool CanOpenFor(GameObject entity);
    void OpenForEntity(GameObject entity);

    void MarkRecentlyTeleported(GameObject entity);
    void DisableInteractionTemporarily(float delay);
}
