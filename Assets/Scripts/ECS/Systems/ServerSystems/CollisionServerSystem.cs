using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Collision;
using Assets.Scripts.ECS.Components.SpatialHashing;
using Assets.Scripts.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct CollisionServerSystem : ISystem
    {
        private NativeParallelMultiHashMap<int, Entity> _grid;
        private NativeQueue<CellChange> _pendingChanges;
        private NativeQueue<CollisionPair> _collisionPairs;

        private const float _cellSize = 10f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _grid = new NativeParallelMultiHashMap<int, Entity>(1024 * 10, Allocator.Persistent);
            _pendingChanges = new NativeQueue<CellChange>(Allocator.Persistent);
            _collisionPairs = new NativeQueue<CollisionPair>(Allocator.Persistent);
            state.RequireForUpdate<ColliderShapeBufferElement>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_grid.IsCreated) _grid.Dispose();
            if (_pendingChanges.IsCreated) _pendingChanges.Dispose();
            if (_collisionPairs.IsCreated) _collisionPairs.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            RegisterNewColliders(ref state, ecb);
            RemoveStaleColliders(ref state, ecb);

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            _pendingChanges.Clear();
            _collisionPairs.Clear();

            var detectCellsHandle = new DetectCellChangesJob
            {
                CellSize = _cellSize,
                PendingChanges = _pendingChanges.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);

            var applyHandle = new ApplyGridChangesJob
            {
                Grid = _grid,
                PendingChanges = _pendingChanges,
                CellLookup = SystemAPI.GetComponentLookup<SpatialCellComponent>(),
                CleanupLookup = SystemAPI.GetComponentLookup<SpatialHashCleanup>()                
            }.Schedule(detectCellsHandle);

            var clearHandle = new ClearCollisionBuffersJob()
                .ScheduleParallel(applyHandle);

            var detectCollisionsHandle = new DetectCollisionsJob
            {
                Grid = _grid,
                CollisionPairs = _collisionPairs.AsParallelWriter(),
                ColliderLookup = SystemAPI.GetBufferLookup<ColliderShapeBufferElement>(true),
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                CellLookup = SystemAPI.GetComponentLookup<SpatialCellComponent>(true),
                MaskLookup = SystemAPI.GetComponentLookup<CollisionLayerComponent>(true),                
            }.ScheduleParallel(clearHandle);

            var writeHandle = new WriteCollisionEventsJob
            {
                CollisionPairs = _collisionPairs,
                ColliderLookup = SystemAPI.GetBufferLookup<ColliderShapeBufferElement>(true),
                BufferLookup = SystemAPI.GetBufferLookup<CollisionEventBufferElement>(),
                MaskLookup = SystemAPI.GetComponentLookup<CollisionLayerComponent>(true)                
            }.Schedule(detectCollisionsHandle);

            state.Dependency = writeHandle;
        }

        [BurstCompile]
        private partial struct DetectCellChangesJob : IJobEntity
        {
            public float CellSize;
            public NativeQueue<CellChange>.ParallelWriter PendingChanges;

            private void Execute(Entity entity, in LocalTransform transform, in SpatialCellComponent cell)
            {
                SpatialHashUtil.WorldToCell(transform.Position, CellSize, out var newCell);

                if (!cell.IsRegistered)
                {
                    PendingChanges.Enqueue(new CellChange
                    {
                        Entity = entity,
                        NewCell = newCell,
                        IsNew = true
                    });
                }
                else if (!newCell.Equals(cell.Cell))
                {
                    PendingChanges.Enqueue(new CellChange
                    {
                        Entity = entity,
                        OldCell = cell.Cell,
                        NewCell = newCell,
                        IsNew = false
                    });
                }
            }
        }

        [BurstCompile]
        private struct ApplyGridChangesJob : IJob
        {
            public NativeParallelMultiHashMap<int, Entity> Grid;
            public NativeQueue<CellChange> PendingChanges;
            public ComponentLookup<SpatialCellComponent> CellLookup;
            public ComponentLookup<SpatialHashCleanup> CleanupLookup;

            public void Execute()
            {
                while (PendingChanges.TryDequeue(out var change))
                {
                    if (!change.IsNew)
                        SpatialHashUtil.RemoveEntityFromGrid(ref Grid, change.Entity, change.OldCell);

                    SpatialHashUtil.HashCell(change.NewCell, out int newCellHash);
                    Grid.Add(newCellHash, change.Entity);

                    var existing = CellLookup[change.Entity];
                    CellLookup[change.Entity] = new SpatialCellComponent
                    {
                        Cell = change.NewCell,
                        IsRegistered = true,
                        BoundingRadius = existing.BoundingRadius
                    };

                    if (CleanupLookup.HasComponent(change.Entity))
                        CleanupLookup[change.Entity] = new SpatialHashCleanup { LastCell = change.NewCell };
                }
            }
        }

        [BurstCompile]
        private partial struct ClearCollisionBuffersJob : IJobEntity
        {
            private void Execute(ref DynamicBuffer<CollisionEventBufferElement> buffer)
            {
                buffer.Clear();
            }
        }

        [BurstCompile]
        private partial struct DetectCollisionsJob : IJobEntity
        {
            [ReadOnly] public NativeParallelMultiHashMap<int, Entity> Grid;
            [ReadOnly] public BufferLookup<ColliderShapeBufferElement> ColliderLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<CollisionLayerComponent> MaskLookup;
            [ReadOnly] public ComponentLookup<SpatialCellComponent> CellLookup;
            public NativeQueue<CollisionPair>.ParallelWriter CollisionPairs;

            private void Execute(
                Entity entity,
                in LocalTransform transform,
                in DynamicBuffer<ColliderShapeBufferElement> shapes,
                in SpatialCellComponent cell,
                in CollisionLayerComponent mask)
            {
                if (!cell.IsRegistered) return;

                for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    SpatialHashUtil.HashCell(cell.Cell + new int3(x, y, z), out int hash);
                    if (!Grid.TryGetFirstValue(hash, out var other, out var it))
                        continue;

                    do
                    {
                        if (other != entity && other.Index < entity.Index &&
                            MaskLookup.TryGetComponent(other, out var otherMask) &&
                            (mask.CollidesWithMask & otherMask.Layer) != 0 &&
                            (otherMask.CollidesWithMask & mask.Layer) != 0)
                        {
                            var otherTransform = TransformLookup[other];
                            var otherCell = CellLookup[other];
                            float3 diff = transform.Position - otherTransform.Position;
                            float combinedR = cell.BoundingRadius + otherCell.BoundingRadius;

                            if (math.dot(diff, diff) <= combinedR * combinedR)
                            {
                                var otherShapes = ColliderLookup[other];
                                bool hit = false;
                                for (int i = 0; i < shapes.Length && !hit; i++)
                                {
                                    var shapeA = shapes[i];
                                    for (int j = 0; j < otherShapes.Length && !hit; j++)
                                    {
                                        var shapeB = otherShapes[j];
                                        if (ShapeOverlapUtil.Overlaps(
                                            in shapeA, transform.Position, transform.Rotation,
                                            in shapeB, otherTransform.Position, otherTransform.Rotation))
                                        {
                                            CollisionPairs.Enqueue(new CollisionPair { A = entity, B = other });
                                            hit = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    while (Grid.TryGetNextValue(out other, ref it));
                }
            }
        }

        [BurstCompile]
        private struct WriteCollisionEventsJob : IJob
        {
            [ReadOnly] public BufferLookup<ColliderShapeBufferElement> ColliderLookup;
            [ReadOnly] public ComponentLookup<CollisionLayerComponent> MaskLookup;

            public BufferLookup<CollisionEventBufferElement> BufferLookup;
            public NativeQueue<CollisionPair> CollisionPairs;

            public void Execute()
            {
                while (CollisionPairs.TryDequeue(out var pair))
                {
                    if (!ColliderLookup.TryGetBuffer(pair.A, out var shapesA) || 
                        !ColliderLookup.TryGetBuffer(pair.B, out var shapesB)
                    ) continue;

                    if (shapesA.Length == 0 || shapesB.Length == 0) continue;

                    var layerA = MaskLookup[pair.A].Layer;
                    var layerB = MaskLookup[pair.B].Layer;

                    if (BufferLookup.HasBuffer(pair.A))
                        BufferLookup[pair.A].Add(new CollisionEventBufferElement { Other = pair.B, OtherLayer = layerB });

                    if (BufferLookup.HasBuffer(pair.B))
                        BufferLookup[pair.B].Add(new CollisionEventBufferElement { Other = pair.A, OtherLayer = layerA });
                }
            }
        }

        private void RegisterNewColliders(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var (radius, entity) in SystemAPI
                .Query<RefRO<BoundingRadiusComponent>>()
                .WithAll<ColliderShapeBufferElement,CollisionLayerComponent>()
                .WithNone<SpatialCellComponent>()
                .WithEntityAccess())
            {
                ecb.AddComponent(entity, new SpatialCellComponent
                {
                    IsRegistered = false,
                    BoundingRadius = radius.ValueRO.Value,
                });
                ecb.AddComponent(entity, new SpatialHashCleanup { LastCell = default });
                ecb.AddBuffer<CollisionEventBufferElement>(entity);
            }
        }

        private void RemoveStaleColliders(ref SystemState state, EntityCommandBuffer ecb)
        {
            foreach (var (cleanup, entity) in SystemAPI
                .Query<RefRO<SpatialHashCleanup>>()
                .WithNone<ColliderShapeBufferElement>()
                .WithEntityAccess())
            {
                SpatialHashUtil.RemoveEntityFromGrid(ref _grid, entity, cleanup.ValueRO.LastCell);
                ecb.RemoveComponent<SpatialHashCleanup>(entity);
            }
        }
    }
}