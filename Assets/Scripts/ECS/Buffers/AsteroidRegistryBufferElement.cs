using Unity.Entities;

namespace Assets.Scripts.ECS.Buffers
{
    public struct AsteroidRegistryBufferElement : IBufferElementData
    {
        public Entity Prefab;
    }
}
