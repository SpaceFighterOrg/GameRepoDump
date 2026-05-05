using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.ECS.Systems.ClientSystems;
using Assets.Scripts.Networking.RPCs;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateAfter(typeof(ProjectileSpawnClientSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ProjectileDestroyClientSystem : ISystem
{
    private NativeHashMap<uint, Entity> _projectileMap;

    public void OnCreate(ref SystemState state)
    {
        _projectileMap = new NativeHashMap<uint, Entity>(256, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        if (_projectileMap.IsCreated)
            _projectileMap.Dispose();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        RegisterNewVisuals(ref state, ref ecb);
        ProcessDestroyRPCs(ref state, ref ecb);
        CleanupStaleEntries(ref state);

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void RegisterNewVisuals(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        foreach (var (projectile, entity) in SystemAPI
            .Query<RefRO<ClientProjectileComponent>>()
            .WithNone<ProjectileTrackedTag>()
            .WithEntityAccess())
        {
            if (projectile.ValueRO.NetworkId == 0) continue;
            _projectileMap.TryAdd(projectile.ValueRO.NetworkId, entity);
            ecb.AddComponent<ProjectileTrackedTag>(entity);
        }
    }

    private void ProcessDestroyRPCs(ref SystemState state, ref EntityCommandBuffer ecb)
    {
        foreach (var (rpc, _, rpcEntity) in SystemAPI
            .Query<RefRO<ProjectileDestroyedRPC>,
                   RefRO<ReceiveRpcCommandRequest>>()
            .WithEntityAccess())
        {
            ecb.DestroyEntity(rpcEntity);

            if (!_projectileMap.TryGetValue(rpc.ValueRO.ProjectileId, out var visual))
                continue;

            _projectileMap.Remove(rpc.ValueRO.ProjectileId);
            ecb.DestroyEntity(visual);

            TrySpawnHitEffect(ref state, ref ecb, rpc.ValueRO);
        }
    }

    private void TrySpawnHitEffect(
        ref SystemState state,
        ref EntityCommandBuffer ecb,
        in ProjectileDestroyedRPC rpc)
    {
        if(rpc.DestroyReason == ProjectileDestroyReason.Lifetime)
            return;

        if (!SystemAPI.TryGetSingleton<HitEffectRegistryComponent>(out var registry))
            return;

        var prefab = registry.GetHitEffectType(rpc.DamageType);

        if (prefab == Entity.Null)
            return;

        var effect = ecb.Instantiate(prefab);
        ecb.SetComponent(effect, LocalTransform.FromPosition(rpc.Position));
    }

    private void CleanupStaleEntries(ref SystemState state)
    {
        foreach (var (projectile, entity) in SystemAPI
            .Query<RefRO<ClientProjectileComponent>>()
            .WithAll<ProjectileTrackedTag>()
            .WithEntityAccess())
        {
            if (SystemAPI.Exists(entity))
                continue;

            _projectileMap.Remove(projectile.ValueRO.NetworkId);
        }
    }
}