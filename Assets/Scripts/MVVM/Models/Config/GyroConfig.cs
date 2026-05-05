using UnityEngine;

namespace Assets.Scripts.MVVM.Models.Config
{
    [CreateAssetMenu(fileName = "GyroConfig", menuName = "Scriptable Objects/Gyro Config")]
    public class GyroConfig : ScriptableObject
    {
        public float MaxPitch = 30.0f;
        public float MinPitch = -30.0f;

        public float MaxYaw = 30.0f;
        public float MinYaw = -30.0f;

        public float MaxRoll = 30.0f;
        public float MinRoll = -30.0f;

        public float PitchDeadzone = 2f;
        public float YawDeadzone = 2f;
        public float RollDeadzone = 2f;
    }

}