using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Utils
{
    [BurstCompile]
    public static class SpatialHashUtil
    {
        [BurstCompile]
        public static void WorldToCell(in float3 pos, float cellSize, out int3 cell)
        {
            cell = new int3(
                (int)math.floor(pos.x / cellSize),
                (int)math.floor(pos.y / cellSize),
                (int)math.floor(pos.z / cellSize)
            );
        }

        [BurstCompile]
        public static void HashCell(in int3 cell, out int hash)
        {
            hash = cell.x * 73856093 ^ cell.y * 19349663 ^ cell.z * 83492791;
        }

        [BurstCompile]
        public static void RemoveEntityFromGrid(
            ref NativeParallelMultiHashMap<int, Entity> grid,
            in Entity entity,
            in int3 cell)
        {
            HashCell(in cell, out int cellHash);

            if (!grid.TryGetFirstValue(cellHash, out var foundEntity, out var it))
                return;

            do
            {
                if (foundEntity == entity)
                {
                    grid.Remove(it);
                    return;
                }
            } while (grid.TryGetNextValue(out foundEntity, ref it));
        }
    }
}