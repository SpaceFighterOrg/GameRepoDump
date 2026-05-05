using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Components.Tags
{
    [GhostComponent]
    public struct DeadTag : IComponentData { }
}
