using Assets.Scripts.ECS.Components.Input;
using Assets.Scripts.MVVM.Models.Config;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public class GyroConfigAuthoring : MonoBehaviour
    {
        public GyroConfig config;

        class Baker : BakerBase<GyroConfigAuthoring>
        {
            public override void Bake(GyroConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new GyroConfigComponent()
                {
                    MaxPitch = authoring.config.MaxPitch,
                    MaxRoll = authoring.config.MaxRoll,
                    MaxYaw = authoring.config.MaxYaw,
                    MinPitch = authoring.config.MinPitch,
                    MinRoll = authoring.config.MinRoll,
                    MinYaw = authoring.config.MinYaw,
                    PitchDeadzone = authoring.config.PitchDeadzone,
                    RollDeadzone = authoring.config.RollDeadzone,
                    YawDeadzone = authoring.config.YawDeadzone,
                });
            }
        }
    }
}