using Assets.Scripts.ECS.Components;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ConnectToServerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            bool shouldConnect = false;
            NetworkEndpoint connectEndpoint = default;

            foreach (var (request, entity) in SystemAPI.Query<RefRO<ConnectToServerRequest>>().WithEntityAccess())
            {
                connectEndpoint = NetworkEndpoint.Parse(request.ValueRO.Ip.ToString(), request.ValueRO.Port);
                shouldConnect = true;
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);

            if (shouldConnect)
                SystemAPI.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(state.EntityManager, connectEndpoint);
        }
    }
}
