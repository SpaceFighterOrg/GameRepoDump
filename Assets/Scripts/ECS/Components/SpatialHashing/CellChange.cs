using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS.Components.SpatialHashing
{
    public struct CellChange
    {
        public Entity Entity;
        public int3 OldCell;
        public int3 NewCell;
        public bool IsNew;
    }
}