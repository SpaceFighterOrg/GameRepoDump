using Unity.Entities;

namespace Assets.Scripts.ECS.Components.Tags
{
    public struct PlayerConnectionCleanup : ICleanupComponentData
    {
        public uint NetworkId;
        public byte MatchSlotIndex;
        public byte Team;
    }
}
