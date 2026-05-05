using Unity.Mathematics;
using Unity.NetCode;

namespace Assets.Scripts.ECS.Components.Input
{
    public struct ShipInputComponent : IInputComponentData
    {
        public short YawQ;
        public short PitchQ;
        public short RollQ;
        public byte ThrottleQ;
        public byte WeaponFireMask;

        public float Yaw
        {
            get => YawQ / 32767f;
            set => YawQ = (short)math.clamp(value * 32767f, short.MinValue, short.MaxValue);
        }

        public float Pitch
        {
            get => PitchQ / 32767f;
            set => PitchQ = (short)(math.clamp(value, -1f, 1f) * 32767f);
        }

        public float Roll
        {
            get => RollQ / 32767f;
            set => RollQ = (short)(math.clamp(value, -1f, 1f) * 32767f);
        }

        public float Throttle
        {
            get => ThrottleQ / 255f;
            set => ThrottleQ = (byte)(math.clamp(value, 0f, 1f) * 255f);
        }
    }
}