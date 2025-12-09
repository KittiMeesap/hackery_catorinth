using UnityEngine;

public static class CounterUtility
{
    public static Counter FindBestCounterForReturn(Vector3 from)
    {
        Counter[] counters = Object.FindObjectsByType<Counter>(FindObjectsSortMode.None);

        Counter best = null;
        float minDist = Mathf.Infinity;

        foreach (var c in counters)
        {
            if (c.bowlOnCounter != null)
                continue;

            float d = Vector2.Distance(from, c.transform.position);
            if (d < minDist)
            {
                minDist = d;
                best = c;
            }
        }

        return best;
    }
}
