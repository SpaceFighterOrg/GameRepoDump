using Assets.Scripts.ECS.Components.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class CameraFollowSystem : SystemBase
    {
        private Camera camera;

        private static readonly float3 LocalOffset = new(0, 0.4542141f, 0.34f);

        protected override void OnCreate()
        {
            camera = Camera.main;
        }

        protected override void OnUpdate()
        {
            foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<PlayerShipTag, PredictedGhost>())
            {
                if (camera == null)
                    camera = Camera.main;

                if (camera == null)
                    return;

                var shipRot = transform.ValueRO.Rotation;
                var shipPos = transform.ValueRO.Position;
                float3 worldOffset = math.rotate(shipRot, LocalOffset);

                camera.transform.position = (Vector3)(shipPos + worldOffset);
                camera.transform.rotation = new Quaternion(
                    transform.ValueRO.Rotation.value.x,
                    transform.ValueRO.Rotation.value.y,
                    transform.ValueRO.Rotation.value.z,
                    transform.ValueRO.Rotation.value.w
                    );
                break;
            }
        }
    }
}