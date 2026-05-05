using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS.Components.SpatialHashing
{
    public struct SpatialHashCleanup : ICleanupComponentData
    {
        public int3 LastCell;
    }
}