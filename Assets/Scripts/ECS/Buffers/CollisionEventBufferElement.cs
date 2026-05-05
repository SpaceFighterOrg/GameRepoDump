using Assets.Scripts.ECS.Components.Collision;
using Unity.Entities;

namespace Assets.Scripts.ECS.Buffers
{
    public struct CollisionEventBufferElement : IBufferElementData
    {
        public Entity Other;
        public CollisionLayer OtherLayer;
    }
}