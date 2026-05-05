using Unity.Entities;

namespace Assets.Scripts.ECS.Components
{
    public struct PlayerShipSpawnerComponent : IComponentData
    {
        public Entity Prefab;
    }
}