using Unity.Collections;
using Unity.Entities;

namespace Assets.Scripts.ECS.Components
{
    public struct ConnectToServerRequest : IComponentData
    {
        public FixedString64Bytes Ip;
        public ushort Port;
    }
}
