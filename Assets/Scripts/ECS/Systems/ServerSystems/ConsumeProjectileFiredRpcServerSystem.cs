using Assets.Scripts.Networking.RPCs;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ConsumeProjectileFiredRpcServerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (_, entity) in SystemAPI
                .Query<RefRO<ProjectileFiredRPC>>()
                .WithAll<ReceiveRpcCommandRequest>()
                .WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}