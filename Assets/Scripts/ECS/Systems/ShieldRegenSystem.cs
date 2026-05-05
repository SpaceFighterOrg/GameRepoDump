using Assets.Scripts.Networking.RPCs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Systems
{

    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ShieldRegenSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var connectionByNetId = new NativeHashMap<int, Entity>(16, Allocator.Temp);
            foreach (var (networkId, connEntity) in SystemAPI.Query<RefRO<NetworkId>>()
                .WithAll<NetworkStreamInGame>()
                .WithEntityAccess())
            {
                connectionByNetId[networkId.ValueRO.Value] = connEntity;
            }

            foreach (var (shield, health, owner) in SystemAPI
                .Query<RefRW<ShieldComponent>, RefRO<HealthComponent>, RefRO<GhostOwner>>()
                .WithAll<Simulate>())
            {
                if (shield.ValueRO.TimeSinceLastHit < shield.ValueRO.RegenRate) continue;

                var newValue = shield.ValueRO.Value + shield.ValueRO.RegenValue;
                if (newValue > shield.ValueRO.MaxValue)
                {
                    newValue = shield.ValueRO.MaxValue;
                    continue;
                }

                shield.ValueRW.Value = newValue;

                if (!connectionByNetId.TryGetValue(owner.ValueRO.NetworkId, out var conn)) continue;

                var rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new ShipHealthUpdateRPC
                {
                    Health = health.ValueRO.Value,
                    MaxHealth = health.ValueRO.MaxValue,
                    Shield = shield.ValueRO.Value,
                    MaxShield = shield.ValueRO.MaxValue
                });
                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = conn });
            }

            connectionByNetId.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}