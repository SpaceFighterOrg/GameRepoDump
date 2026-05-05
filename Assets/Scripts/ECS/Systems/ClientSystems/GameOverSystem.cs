using Assets.Scripts.MVVM;
using Assets.Scripts.Networking.RPCs;
using Assets.Scripts.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GameOverSystem : ISystem
    {
        private float _gameOverTimer;
        private bool _waitingToLoad;

        public void OnCreate(ref SystemState state)
        {
            _waitingToLoad = false;
            _gameOverTimer = 0f;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_waitingToLoad)
            {
                _gameOverTimer -= SystemAPI.Time.DeltaTime;

                if (_gameOverTimer <= 0f)
                {
                    _waitingToLoad = false;

                    var ecb = new EntityCommandBuffer(Allocator.Temp);
                    foreach (var (_, connEntity) in SystemAPI.Query<RefRO<NetworkId>>()
                        .WithAll<NetworkStreamInGame>()
                        .WithEntityAccess())
                    {
                        ecb.AddComponent<NetworkStreamRequestDisconnect>(connEntity);
                    }
                    ecb.Playback(state.EntityManager);
                    ecb.Dispose();

                    var op = SceneManager.LoadSceneAsync("GalaxyScene");
                    op.completed += _ => NavigationManager.Instance?.ReturnToInitial();
                }

                return;
            }

            var rpcEcb = new EntityCommandBuffer(Allocator.Temp);
            bool hasGameOver = false;
            GameOverRPC received = default;

            foreach (var (rpc, entity) in SystemAPI.Query<RefRO<GameOverRPC>>()
                .WithAll<ReceiveRpcCommandRequest>()
                .WithEntityAccess())
            {
                received = rpc.ValueRO;
                hasGameOver = true;
                rpcEcb.DestroyEntity(entity);
            }

            rpcEcb.Playback(state.EntityManager);
            rpcEcb.Dispose();

            Debug.Log($"GameOverSystem: hasGameOver={hasGameOver}");

            if (!hasGameOver) return;

            Debug.Log(received.WinningTeam == SessionManager.Team ? "Victory!" : "Defeat!");
            GameOverView.Instance.SetText(received.WinningTeam == SessionManager.Team ? "Victory!" : "Defeat!");

            _gameOverTimer = 5f;
            _waitingToLoad = true;
        }

        public void OnDestroy(ref SystemState state) { }
    }
}