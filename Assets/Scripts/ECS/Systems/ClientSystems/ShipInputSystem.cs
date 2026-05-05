using Assets.Scripts.Controllers;
using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Input;
using Assets.Scripts.ECS.Components.Tags;
using Assets.Scripts.MVVM.ViewModels;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Assets.Scripts.ECS.Systems.ClientSystems
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct ShipInputSystem : ISystem
    {
        private bool _warnedNoTarget;

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<CommandTarget>(out var commandTarget))
            {
                if (!_warnedNoTarget)
                {
                    Debug.LogWarning("[ShipInput] No CommandTarget singleton found.");
                    _warnedNoTarget = true;
                }
                return;
            }

            if (commandTarget.targetEntity == Entity.Null)
            {
                if (!_warnedNoTarget)
                {
                    Debug.LogWarning("[ShipInput] CommandTarget entity is null — ship not yet assigned.");
                    _warnedNoTarget = true;
                }
                return;
            }

            _warnedNoTarget = false;

            if (!SystemAPI.HasComponent<ShipInputComponent>(commandTarget.targetEntity))
            {
                Debug.LogWarning("CommandTarget entity does not have ShipInputComponent.");
                return;
            }

            if (SystemAPI.HasComponent<DeadTag>(commandTarget.targetEntity))
            {
                var deadInput = SystemAPI.GetComponentRW<ShipInputComponent>(commandTarget.targetEntity);
                deadInput.ValueRW = default;
                return;
            }

            if (!SystemAPI.TryGetSingleton<GyroConfigComponent>(out var gyroConfig))
                return;


            var gyroInput = GyroSpaceshipController.GyroInput;
            var throttle = ThrottleViewModel.Throttle;
            var weapons = SystemAPI.GetBuffer<WeaponBufferElement>(commandTarget.targetEntity);

            var input = SystemAPI.GetComponentRW<ShipInputComponent>(commandTarget.targetEntity);
            input.ValueRW.Pitch = math.clamp(gyroInput.x / gyroConfig.MaxPitch, -1f, 1f);
            input.ValueRW.Yaw = math.clamp(gyroInput.y / gyroConfig.MaxYaw, -1f, 1f);
            input.ValueRW.Roll = math.clamp(gyroInput.z / gyroConfig.MaxRoll, -1f, 1f);
            input.ValueRW.Throttle = math.clamp(ThrottleViewModel.Throttle, 0f, 1f);

            input.ValueRW.WeaponFireMask = 0;

            for (byte i = 0; i < WeaponPanelViewModel.WeaponCount; i++)
            {
                if (WeaponPanelViewModel.IsWeaponFired(i))
                    input.ValueRW.WeaponFireMask |= (byte)(1 << i);
            }

        }
    }
}