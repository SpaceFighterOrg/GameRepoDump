using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts
{
    [GhostComponent]
    public struct UsernameComponent : IComponentData
    {
        [GhostField] public FixedString128Bytes Username;
    }
}
