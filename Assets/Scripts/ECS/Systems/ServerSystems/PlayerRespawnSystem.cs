using Assets.Scripts.ECS.Components;
using Assets.Scripts.ECS.Components.Spawners;
using Assets.Scripts.ECS.Components.Tags;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlayerRespawnSystem : ISystem
    {
        private const double RespawnDelay = 5.0;

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var elapsed = SystemAPI.Time.ElapsedTime;

            float3 team1Center = float3.zero;
            float team1Radius = 0f;
            float3 team2Center = float3.zero;
            float team2Radius = 0f;

            foreach (var (sp, lt, team) in SystemAPI.Query<RefRO<TeamSpawnPointComponent>, RefRO<LocalTransform>, RefRO<TeamComponent>>())
            {
                if (team.ValueRO.Value == 1)
                {
                    team1Center = lt.ValueRO.Position;
                    team1Radius = sp.ValueRO.Radius;
                }
                else if (team.ValueRO.Value == 2)
                {
                    team2Center = lt.ValueRO.Position;
                    team2Radius = sp.ValueRO.Radius;
                }
            }

            foreach (var (death, health, shield, transform, entity) in SystemAPI.Query<
                RefRO<PlayerDeathComponent>,
                RefRW<HealthComponent>,
                RefRW<ShieldComponent>,
                RefRW<LocalTransform>>()
                .WithAll<DeadTag, DeathProcessedTag, PlayerShipTag>()
                .WithEntityAccess())
            {
                if (elapsed - death.ValueRO.TimeOfDeath < RespawnDelay)
                    continue;

                bool isTeam1 = SystemAPI.GetComponent<TeamComponent>(entity).Value == 1;
                float3 center = isTeam1 ? team1Center : team2Center;
                float radius = isTeam1 ? team1Radius : team2Radius;

                var rng = new Random((uint)entity.Index * 2654435761u + (uint)(elapsed * 1000) + 1u);
                float3 spawnPos = center + rng.NextFloat3Direction() * rng.NextFloat(0f, radius);

                transform.ValueRW.Position = spawnPos;
                health.ValueRW.Value = health.ValueRO.MaxValue;
                shield.ValueRW.Value = shield.ValueRO.MaxValue;
                shield.ValueRW.TimeSinceLastHit = 0f;

                ecb.RemoveComponent<DeadTag>(entity);
                ecb.RemoveComponent<DeathProcessedTag>(entity);
                ecb.RemoveComponent<PlayerDeathComponent>(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
