using Assets.Scripts.ECS.Components.Collision;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.ECS.Buffers
{
    public struct ColliderShapeBufferElement : IBufferElementData
    {
        // Sphere: (radius, _, _) 
        // Capsule: (radius, halfHeight, _)
        // Box: (halfExtents.x, halfExtents.y, halfExtents.z)
        public float3 Params;
        public ColliderShapeType ShapeType;

        public float3 LocalOffset;
        public quaternion LocalRotation;
    }
}