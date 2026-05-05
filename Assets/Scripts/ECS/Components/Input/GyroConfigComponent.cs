using Unity.Entities;

namespace Assets.Scripts.ECS.Components.Input
{
    public struct GyroConfigComponent : IComponentData
    {
        public float MaxPitch;
        public float MinPitch;
        public float MaxYaw;
        public float MinYaw;
        public float MaxRoll;
        public float MinRoll;
        public float PitchDeadzone;
        public float YawDeadzone;
        public float RollDeadzone;
    }
}