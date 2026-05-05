using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS.Components.SpatialHashing
{
    public struct SpatialCellComponent : IComponentData
    {
        public int3 Cell;
        public bool IsRegistered;
        public float BoundingRadius;
    }
}