using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.ECS.Matchmaking;
using Assets.Scripts.MVVM.Models.DamageTypes;
using Assets.Scripts.Networking.RPCs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.ServerSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CollisionServerSystem))]
    public partial struct ProjectileHitServerSystem : ISystem
    {
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

            TickIonDisabledTimer(ref state, ref ecb);

            foreach (var (collisionEventBuffer, projectile, transform, entity) in SystemAPI
                .Query<DynamicBuffer<CollisionEventBufferElement>,
                       RefRW<ServerProjectileComponent>,
                       RefRO<LocalTransform>>()
                .WithEntityAccess())
            {
                projectile.ValueRW.OwnerGracePeriod -= SystemAPI.Time.DeltaTime;

                if (collisionEventBuffer.IsEmpty) continue;

                Entity hitTarget = Entity.Null;
                for (int i = 0; i < collisionEventBuffer.Length; i++)
                {
                    var candidate = collisionEventBuffer[i];
                    if (projectile.ValueRO.OwnerGracePeriod > 0f &&
                        candidate.Other == projectile.ValueRO.Owner) continue;
                    hitTarget = candidate.Other;
                    break;
                }

                if (hitTarget == Entity.Null) continue;

                ApplyDamage(ref state, ref ecb, hitTarget, in projectile.ValueRO);

                if (SystemAPI.HasComponent<GhostOwner>(hitTarget))
                {
                    int ownerId = SystemAPI.GetComponent<GhostOwner>(hitTarget).NetworkId;
                    if (connectionByNetId.TryGetValue(ownerId, out var ownerConn))
                    {
                        int hp = 0, maxHp = 0, sh = 0, maxSh = 0;
                        if (SystemAPI.HasComponent<HealthComponent>(hitTarget))
                        {
                            var hc = SystemAPI.GetComponent<HealthComponent>(hitTarget);
                            hp = hc.Value; maxHp = hc.MaxValue;
                        }
                        if (SystemAPI.HasComponent<ShieldComponent>(hitTarget))
                        {
                            var sc = SystemAPI.GetComponent<ShieldComponent>(hitTarget);
                            sh = sc.Value; maxSh = sc.MaxValue;
                        }
                        var healthRpc = ecb.CreateEntity();
                        ecb.AddComponent(healthRpc, new ShipHealthUpdateRPC
                        {
                            Health = hp, MaxHealth = maxHp,
                            Shield = sh, MaxShield = maxSh
                        });
                        ecb.AddComponent(healthRpc, new SendRpcCommandRequest { TargetConnection = ownerConn });
                    }
                }

                var rpc = ecb.CreateEntity();
                ecb.AddComponent(rpc, new ProjectileDestroyedRPC
                {
                    Position = transform.ValueRO.Position,
                    DamageType = projectile.ValueRO.DamageType,
                    ProjectileId = projectile.ValueRO.NetworkId,
                    DestroyReason = ProjectileDestroyReason.Hit
                });
                ecb.AddComponent(rpc, new SendRpcCommandRequest());

                ecb.DestroyEntity(entity);
                SaveKillData(ref state, in projectile.ValueRO, ref hitTarget);
            }

            connectionByNetId.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void SaveKillData(ref SystemState state, in ServerProjectileComponent projectile, ref Entity hitTarget)
        {
            int shooterBackendId = 0;
            int victimBackendId = 0;

            if (SystemAPI.HasComponent<GhostOwner>(projectile.Owner))
            {
                uint shooterNetId = (uint)SystemAPI.GetComponent<GhostOwner>(projectile.Owner).NetworkId;
                var shooterInfo = MatchStateRegistry.GetPlayerInfo(shooterNetId);
                if (shooterInfo.HasValue)
                    shooterBackendId = shooterInfo.Value.info.BackendPlayerId;
            }

            if (SystemAPI.HasComponent<GhostOwner>(hitTarget))
            {
                uint victimNetId = (uint)SystemAPI.GetComponent<GhostOwner>(hitTarget).NetworkId;
                var victimInfo = MatchStateRegistry.GetPlayerInfo(victimNetId);
                if (victimInfo.HasValue)
                    victimBackendId = victimInfo.Value.info.BackendPlayerId;
            }

            ApiService.Api.KillsAsync(shooterBackendId, 1);
            ApiService.Api.DeathsAsync(victimBackendId, 1);
        }

        [BurstCompile]
        private void TickIonDisabledTimer(ref SystemState state, ref EntityCommandBuffer ecb)
        {
            foreach (var (ion, entity) in SystemAPI
                    .Query<RefRW<DisabledByIonComponent>>()
                    .WithEntityAccess())
            {
                ion.ValueRW.TimeRemaining -= SystemAPI.Time.DeltaTime;
                if (ion.ValueRW.TimeRemaining <= 0f)
                    ecb.RemoveComponent<DisabledByIonComponent>(entity);
            }
        }

        [BurstCompile]
        private void ApplyDamage(
            ref SystemState state,
            ref EntityCommandBuffer ecb,
            Entity target,
            in ServerProjectileComponent projectile)
        {
            bool isIon = projectile.DamageType == DamageType.Ion;
            int remainingDamage = projectile.Damage;

            if (SystemAPI.HasComponent<ShieldComponent>(target))
            {
                var shield = SystemAPI.GetComponentRW<ShieldComponent>(target);

                if (shield.ValueRO.Value > 0)
                {
                    int shieldDamage = isIon ? remainingDamage * 2 : remainingDamage;
                    int absorbed = math.min(shield.ValueRO.Value, shieldDamage);

                    shield.ValueRW.Value -= absorbed;
                    shield.ValueRW.TimeSinceLastHit = 0f;

                    remainingDamage = isIon ? 0 : math.max(0, remainingDamage - absorbed);
                }
            }

            if (isIon)
            {
                if (!SystemAPI.HasComponent<HealthComponent>(target)) return;

                var health = SystemAPI.GetComponentRW<HealthComponent>(target);
                health.ValueRW.IonDamageTaken += projectile.Damage;

                if (health.ValueRO.IonDamageTaken >= health.ValueRO.IonDisableThreshold)
                {
                    health.ValueRW.IonDamageTaken = 0;

                    if (SystemAPI.HasComponent<DisabledByIonComponent>(target))
                    {
                        var existing = SystemAPI.GetComponentRW<DisabledByIonComponent>(target);
                        existing.ValueRW.TimeRemaining = health.ValueRO.IonDisableDuration;
                    }
                    else
                    {
                        ecb.AddComponent(target, new DisabledByIonComponent
                        {
                            TimeRemaining = health.ValueRO.IonDisableDuration
                        });
                    }
                }
                return;
            }

            if (remainingDamage <= 0) return;

            if (SystemAPI.HasComponent<HealthComponent>(target))
            {
                var health = SystemAPI.GetComponentRW<HealthComponent>(target);
                health.ValueRW.Value = math.max(0, health.ValueRW.Value - remainingDamage);

                if (health.ValueRO.Value <= 0)
                    ecb.DestroyEntity(target);
            }
        }
    }
}