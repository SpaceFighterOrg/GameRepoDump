using Assets.Scripts.ECS.Components.Input;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    public partial struct AssignCommandTargetSystem : ISystem
    {
        private double _lastDiagnosticTime;

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<NetworkId>(out var connectionEntity))
                return;

            var commandTarget = SystemAPI.GetComponentRW<CommandTarget>(connectionEntity);

            if (commandTarget.ValueRO.targetEntity != Entity.Null)
                return;

            var localNetworkId = SystemAPI.GetComponent<NetworkId>(connectionEntity).Value;

            int totalGhostOwners = 0;
            int withInput = 0;
            int withOwnerIsLocal = 0;

            foreach (var (ghostOwner, entity) in SystemAPI.Query<RefRO<GhostOwner>>()
                         .WithAll<GhostOwnerIsLocal>()
                         .WithEntityAccess())
            {
                withOwnerIsLocal++;
                commandTarget.ValueRW.targetEntity = entity;
                Debug.Log($"[AssignTarget] Assigned ship {entity} via GhostOwnerIsLocal (localNetId={localNetworkId}).");
                return;
            }

            // fallback
            foreach (var (ghostOwner, entity) in SystemAPI.Query<RefRO<GhostOwner>>()
                         .WithAll<ShipInputComponent>()
                         .WithEntityAccess())
            {
                withInput++;
                if (ghostOwner.ValueRO.NetworkId == localNetworkId)
                {
                    commandTarget.ValueRW.targetEntity = entity;
                    Debug.Log($"[AssignTarget] Assigned ship {entity} via ShipInputComponent fallback (localNetId={localNetworkId}).");
                    return;
                }
            }

            foreach (var (ghostOwner, _) in SystemAPI.Query<RefRO<GhostOwner>>().WithEntityAccess())
            {
                totalGhostOwners++;
            }

            var elapsed = SystemAPI.Time.ElapsedTime;
            if (elapsed - _lastDiagnosticTime > 3.0)
            {
                _lastDiagnosticTime = elapsed;
                Debug.LogWarning($"[AssignTarget] No ship found. localNetId={localNetworkId}, " +
                                 $"GhostOwner entities={totalGhostOwners}, GhostOwnerIsLocal={withOwnerIsLocal}, " +
                                 $"with ShipInputComponent={withInput}.");
            }
        }
    }
}