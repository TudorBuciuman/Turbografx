using UnityEngine;

public class Breakable : MonoBehaviour
{
    public void TryBreak(PlayerStats stats)
    {
        if (stats.CanBreakObjects())
        {
            Destroy(gameObject);
        }
    }
}