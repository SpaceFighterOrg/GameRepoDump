using Unity.Entities;

namespace Assets.Scripts.ECS.Components.Spawners
{
    public struct AsteroidSpawnerComponent : IComponentData
    {
        public float ScaleMin;
        public float ScaleMax;
        public float SpawnRadius;
        public int Count;
        public uint Seed;
        public byte MatchSlotIndex;
    }
}
