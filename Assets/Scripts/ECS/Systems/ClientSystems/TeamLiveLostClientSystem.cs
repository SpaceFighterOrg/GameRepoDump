using Assets.Scripts.MVVM;
using Assets.Scripts.Networking.RPCs;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct TeamLiveLostClientSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (rpc, entity) in SystemAPI
                .Query<RefRO<LiveLostRPC>>()
                .WithAll<ReceiveRpcCommandRequest>()
                .WithEntityAccess())
            {
                TeamCounterWidgetView.Instance?.SetLiveCounts(
                    rpc.ValueRO.Team1LivesRemaining,
                    rpc.ValueRO.Team2LivesRemaining);

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
