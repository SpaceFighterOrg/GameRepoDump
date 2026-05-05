using Unity.Entities;

namespace Assets.Scripts.ECS.Components
{
    public struct PlayerDeathComponent : IComponentData
    {
        public double TimeOfDeath;
    }
}
