using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Components
{
    [GhostComponent]
    public struct TeamComponent : IComponentData
    {
        public byte Value;
    }
}
