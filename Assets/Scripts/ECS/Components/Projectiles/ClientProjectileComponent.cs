using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Components.Projectiles
{
    [GhostComponent(
    PrefabType = GhostPrefabType.Client,
    SendTypeOptimization = GhostSendType.DontSend
)]
    public struct ClientProjectileComponent : IComponentData
    {
        public uint NetworkId;
        public float Speed;
        public float Lifetime;
        public float TimeAlive;
    }
}