using Unity.Entities;

namespace Assets.Scripts.ECS.Components.Projectiles
{
    public struct DisabledByIonComponent : IComponentData
    {
        public float TimeRemaining;
    }
}
