using Assets.Scripts.MVVM.Models.DamageTypes;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Components.Projectiles
{
    [GhostComponent(
        PrefabType = GhostPrefabType.Server,
        SendTypeOptimization = GhostSendType.DontSend
    )]
    public struct ServerProjectileComponent : IComponentData
    {
        public uint NetworkId;
        public Entity Owner;

        public float Speed;

        public int Lifetime;
        public float TimeAlive;

        public int Damage;
        public DamageType DamageType;

        public float OwnerGracePeriod;
    }
}