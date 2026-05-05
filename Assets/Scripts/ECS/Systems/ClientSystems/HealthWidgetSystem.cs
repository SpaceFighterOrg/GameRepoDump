using Assets.Scripts.MVVM;
using Assets.Scripts.Networking.RPCs;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct HealthWidgetSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (rpc, entity) in SystemAPI
                .Query<RefRO<ShipHealthUpdateRPC>>()
                .WithAll<ReceiveRpcCommandRequest>()
                .WithEntityAccess())
            {
                ShipHealthWidgetView.Instance?.SetValues(
                    rpc.ValueRO.Health,
                    rpc.ValueRO.MaxHealth,
                    rpc.ValueRO.Shield,
                    rpc.ValueRO.MaxShield);

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
