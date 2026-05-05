using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS.Components
{
    public struct ShipDestructionCleanupComponent : ICleanupComponentData
    {
        public float3 LastPosition;
    }
}
