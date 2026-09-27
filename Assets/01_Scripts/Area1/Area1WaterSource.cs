using UnityEngine;
namespace SlimeRancher.Area1
{
    // Only colliders explicitly marked as a source can supply water.
    [RequireComponent(typeof(Collider))]
    public sealed class Area1WaterSource : MonoBehaviour { }
}
