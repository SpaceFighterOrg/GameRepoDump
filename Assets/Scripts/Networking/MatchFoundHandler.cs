using Assets.Scripts.ECS.Components;
using Assets.Scripts.Utils;
using Backend.Common.DTOs.Matchmaking;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Networking
{
    public class MatchFoundHandler : MonoBehaviour
    {
        private const string GameSceneName = "MainScene";

        private void Awake()
        {
            if (FindObjectsByType<MatchFoundHandler>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            MatchmakingHubService.OnMatchFound += HandleMatchFound;
        }

        private void OnDisable()
        {
            MatchmakingHubService.OnMatchFound -= HandleMatchFound;
        }

        private void HandleMatchFound(MatchmakeResultDTO result)
        {
            StoreSessionData(result);
            StartCoroutine(LoadSceneThenConnect(result.ServerIp, (ushort)result.ServerPort));
        }

        private IEnumerator LoadSceneThenConnect(string ip, ushort port)
        {
            yield return SceneManager.LoadSceneAsync(GameSceneName);
            Debug.Log("[MatchFoundHandler] Scene loaded. Waiting for ClientWorld...");

            yield return new WaitUntil(() =>
            {
                var w = ClientServerBootstrap.ClientWorld;
                return w != null && w.IsCreated;
            });

            Debug.Log("[MatchFoundHandler] ClientWorld ready. Waiting for ghost prefab entities...");

            float deadline = Time.realtimeSinceStartup + 5f;
            yield return new WaitUntil(() =>AreLocalGhostPrefabsLoaded() || Time.realtimeSinceStartup >= deadline);

            if (AreLocalGhostPrefabsLoaded())
                Debug.Log($"[MatchFoundHandler] Ghost prefabs ready — connecting to {ip}:{port}.");
            else
                Debug.LogWarning($"[MatchFoundHandler] Ghost prefabs not found after timeout — connecting anyway to {ip}:{port}.");

            ConnectToGameServer(ip, port);
        }

        private static bool AreLocalGhostPrefabsLoaded()
        {
            var world = ClientServerBootstrap.ClientWorld;
            if (world == null || !world.IsCreated) return false;
            var em = world.EntityManager;
            var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<GhostType>() },
                Options = EntityQueryOptions.IncludePrefab
            });
            bool ready = !query.IsEmpty;
            query.Dispose();
            return ready;
        }

        private static void StoreSessionData(MatchmakeResultDTO result)
        {
            SessionManager.MatchId = result.ServerMatchId;
            SessionManager.MatchSlotIndex = (byte)result.MatchSlotIndex;
            SessionManager.ServerIp = result.ServerIp;
            SessionManager.ServerPort = result.ServerPort;

            // Determine which team this player belongs to
            byte team = 2;
            foreach (var entry in result.Team1)
            {
                foreach (var player in entry.Entries)
                {
                    if (player.Id == SessionManager.UserId)
                    {
                        team = 1;
                        SessionManager.AttackingPlanetId = player.AttackingPlanetId;
                        SessionManager.DefendingPlanetId = player.DefendingPlanetId;
                        SessionManager.Username = player.Username;
                        break;
                    }
                }
                if (team == 1) break;
            }

            if (team == 2)
            {
                foreach (var entry in result.Team2)
                {
                    foreach (var player in entry.Entries)
                    {
                        if (player.Id == SessionManager.UserId)
                        {
                            SessionManager.AttackingPlanetId = player.AttackingPlanetId;
                            SessionManager.DefendingPlanetId = player.DefendingPlanetId;
                            SessionManager.Username = player.Username;
                            break;
                        }
                    }
                }
            }

            SessionManager.Team = team;
        }

        private static void ConnectToGameServer(string ip, ushort port)
        {
            var clientWorld = ClientServerBootstrap.ClientWorld;
            if (clientWorld == null)
            {
                Debug.LogError("[MatchFoundHandler] No client world found.");
                return;
            }

            var em = clientWorld.EntityManager;
            var entity = em.CreateEntity(typeof(ConnectToServerRequest));
            em.SetComponentData(entity, new ConnectToServerRequest
            {
                Ip = new FixedString64Bytes(ip),
                Port = port
            });

            Debug.Log($"[MatchFoundHandler] Connecting to {ip}:{port}");
        }
    }
}
