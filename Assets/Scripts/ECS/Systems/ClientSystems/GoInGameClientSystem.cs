using Assets.Scripts.Networking.RPCs;
using Assets.Scripts.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct GoInGameClientSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithNone<NetworkStreamInGame>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<NetworkStreamInGame>(entity);

                var rpcEntity = ecb.CreateEntity();
                ecb.AddComponent(rpcEntity, new GoInGameRpc
                {
                    MatchId = new FixedString128Bytes(SessionManager.MatchId ?? string.Empty),
                    MatchSlotIndex = SessionManager.MatchSlotIndex,
                    Team = SessionManager.Team,
                    AttackingPlanetId = SessionManager.AttackingPlanetId,
                    BackendPlayerId = SessionManager.UserId,
                    FactionId = SessionManager.FactionId,
                    Username = new FixedString128Bytes(SessionManager.Username ?? string.Empty)
                });
                ecb.AddComponent<SendRpcCommandRequest>(rpcEntity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
