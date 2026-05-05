using Assets.Scripts.ECS.Components;
using Assets.Scripts.MVVM.ViewModels;
using Assets.Scripts.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class RadarHudSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            byte localTeam = SessionManager.Team;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (hudRef, team, entity) in
                SystemAPI.Query<HudMarkerReferenceComponent, RefRO<TeamComponent>>()
                         .WithAll<HudColorPending>()
                         .WithEntityAccess())
            {
                hudRef.Hud.SetTeamColor(localTeam == team.ValueRO.Value);
                ecb.RemoveComponent<HudColorPending>(entity);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();

            float3 localPlayerPos = float3.zero;
            foreach (var (_, ltw) in
                SystemAPI.Query<RefRO<GhostOwnerIsLocal>, RefRO<LocalToWorld>>())
            {
                localPlayerPos = ltw.ValueRO.Position;
                break;
            }

            foreach (var (hudRef, ltw, username, velocity) in
                SystemAPI.Query<
                    HudMarkerReferenceComponent, 
                    RefRO<LocalToWorld>, 
                    RefRO<UsernameComponent>,
                    RefRO<VelocityComponent>>())
            {
                if (hudRef.Hud == null) continue;

                var pos = ltw.ValueRO.Position;
                float distance = math.distance(localPlayerPos, pos);

                // scalar speed x forward direction
                float3 forward = ltw.ValueRO.Forward;
                Vector3 worldVelocity = new Vector3(
                    forward.x * velocity.ValueRO.Velocity,
                    forward.y * velocity.ValueRO.Velocity,
                    forward.z * velocity.ValueRO.Velocity
                );

                Vector3 targetWorldPos = new Vector3(pos.x, pos.y, pos.z);
                Vector3 firerWorldPos = new Vector3(localPlayerPos.x, localPlayerPos.y, localPlayerPos.z);

                Vector3 predictionLocalOffset = PredictWidgetLocalOffset(
                    targetWorldPos,
                    worldVelocity,
                    firerWorldPos,
                    hudRef.Hud.GetTransform()
                );

                hudRef.Hud.SetPosition(new Vector3(pos.x, pos.y, pos.z));
                hudRef.Hud.SetData(username.ValueRO.Username.ToString(), distance, predictionLocalOffset);
            }

            // cleanup for destroyed ships
            var manager = HudMarkerManager.Instance;
            if (manager != null)
            {
                var despawned = SystemAPI.QueryBuilder()
                    .WithAll<HudMarkerReferenceComponent>()
                    .WithNone<GhostOwner>()
                    .Build()
                    .ToEntityArray(Allocator.Temp);

                foreach (var entity in despawned)
                {
                    manager.Remove(entity);
                    EntityManager.RemoveComponent<HudMarkerReferenceComponent>(entity);
                }

                despawned.Dispose();
            }
        }

        private static Vector3 PredictWidgetLocalOffset(
            Vector3 targetPosition,
            Vector3 targetVelocity,
            Vector3 firerPosition,
            Transform markerTransform,
            float localDisplayRadius = 0.5f)
        {
            float projectileSpeed = SessionManager.LatestFiredProjectileSpeed;

            Vector3 displacement = targetPosition - firerPosition;

            float a = projectileSpeed * projectileSpeed - Vector3.Dot(targetVelocity, targetVelocity);
            float b = -2f * Vector3.Dot(displacement, targetVelocity);
            float c = -Vector3.Dot(displacement, displacement);

            Vector3 interceptWorld;

            if (Mathf.Abs(a) < 1e-6f)
            {
                float tLinear = Mathf.Abs(b) > 1e-6f ? -c / b : -1f;
                interceptWorld = tLinear > 0f ? targetPosition + targetVelocity * tLinear : targetPosition;
            }
            else
            {
                float disc = b * b - 4f * a * c;
                if (disc < 0f)
                {
                    interceptWorld = targetPosition;
                }
                else
                {
                    float sqrtDisc = Mathf.Sqrt(disc);
                    float t1 = (-b + sqrtDisc) / (2f * a);
                    float t2 = (-b - sqrtDisc) / (2f * a);

                    float t = -1f;
                    if (t1 > 0f && t2 > 0f) t = Mathf.Min(t1, t2);
                    else if (t1 > 0f) t = t1;
                    else if (t2 > 0f) t = t2;

                    interceptWorld = t > 0f ? targetPosition + targetVelocity * t : targetPosition;
                }
            }

            // convert the world-space intercept into the marker's local space,
            Vector3 localIntercept = markerTransform.InverseTransformPoint(interceptWorld);
            Vector3 localDirection = localIntercept.normalized;
            return localDirection * localDisplayRadius;
        }

    }
}