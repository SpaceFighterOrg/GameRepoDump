using Assets.Scripts.ECS.Components;
using Assets.Scripts.Interfaces;
using Assets.Scripts.MVVM.ViewModels;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientHudWiringSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var manager = HudMarkerManager.Instance;
            if (manager == null) return;

            // collect unwired entities
            var unwired = SystemAPI.QueryBuilder()
                .WithAll<GhostOwner>()
                .WithNone<HudMarkerReferenceComponent>()
                .Build()
                .ToEntityArray(Allocator.Temp);

            foreach (var entity in unwired)
            {
                var hudMarker = manager.GetOrCreate(entity, World);
                if (hudMarker == null) continue;

                EntityManager.AddComponentObject(entity, new HudMarkerReferenceComponent { Hud = hudMarker });
                EntityManager.AddComponent<HudColorPending>(entity);
                Debug.Log($"[Wiring] Successfully wired {entity}");
            }

            unwired.Dispose();

            foreach (var (_, entity) in SystemAPI
                .Query<HudMarkerReferenceComponent>()
                .WithNone<GhostOwner>()
                .WithEntityAccess())
            {
                manager.Remove(entity);
                EntityManager.RemoveComponent<HudMarkerReferenceComponent>(entity);
            }

        }
    }
}
