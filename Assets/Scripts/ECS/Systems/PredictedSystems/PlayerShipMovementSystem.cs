using Assets.Scripts.ECS.Components.Input;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.Utils;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Assets.Scripts.ECS.Systems.PredictedSystems
{
    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerShipMovementSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (transform, input, velocity, rotVelocity) in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<ShipInputComponent>,
                RefRW<VelocityComponent>,
                RefRO<RotationVelocityComponent>
            >().WithAll<PlayerShipTag, Simulate>().WithNone<DeadTag>())
            {
                float targetVelocity = input.ValueRO.Throttle * velocity.ValueRO.MaxVelocity;
                float rate = targetVelocity > velocity.ValueRO.Velocity ? velocity.ValueRO.Acceleration : velocity.ValueRO.Deceleration;
                velocity.ValueRW.Velocity = MathUtil.MoveTowards(velocity.ValueRO.Velocity, targetVelocity, rate * dt);

                float rotRad = math.radians(rotVelocity.ValueRO.Value) * dt;
                var delta = quaternion.EulerXYZ(new float3(
                    input.ValueRO.Pitch * rotRad,
                    input.ValueRO.Yaw * rotRad,
                    input.ValueRO.Roll * rotRad
                ));

                transform.ValueRW.Rotation = math.mul(transform.ValueRO.Rotation, delta);
                transform.ValueRW.Position += velocity.ValueRO.Velocity * dt * transform.ValueRO.Forward();
            }
        }
    }
}