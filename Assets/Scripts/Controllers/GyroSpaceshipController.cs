using Assets.Scripts.MVVM.Models.Config;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Controllers
{
    public class GyroSpaceshipController : ControllerBase
    {
        [SerializeField] private GyroConfig config;

        private bool gyroEnabled;
        private static Gyroscope gyro;

        private static Quaternion rotFix;
        private static Quaternion calibration;

        public static float3 GyroInput;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);
            gyroEnabled = EnableGyro();
        }

        private bool EnableGyro()
        {
            if (SystemInfo.supportsGyroscope)
            {
                gyro = Input.gyro;
                gyro.enabled = true;
                rotFix = new Quaternion(0, 0, 1, 0);
                Calibrate();
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (!gyroEnabled) return;

            Quaternion deviceRotation = gyro.attitude;
            Quaternion converted = rotFix * new Quaternion(deviceRotation.x, deviceRotation.y, -deviceRotation.z, -deviceRotation.w);
            Quaternion finalRotation = calibration * converted;
            Vector3 euler = finalRotation.eulerAngles;

            var pitch = ApplyDeadzone(Mathf.Clamp(NormalizeAngle(euler.x), config.MinPitch, config.MaxPitch), config.PitchDeadzone);
            var yaw = ApplyDeadzone(Mathf.Clamp(NormalizeAngle(euler.y), config.MinYaw, config.MaxYaw), config.YawDeadzone);
            var roll = ApplyDeadzone(Mathf.Clamp(NormalizeAngle(euler.z), config.MinRoll, config.MaxRoll), config.RollDeadzone);

            GyroInput = new float3(pitch, yaw, roll);
        }

        private float NormalizeAngle(float angle)
        {
            if (angle > 180) angle -= 360;
            return angle;
        }

        public static void Calibrate()
        {
            Quaternion deviceRotation = gyro.attitude;
            Quaternion converted = rotFix * new Quaternion(deviceRotation.x, deviceRotation.y, -deviceRotation.z, -deviceRotation.w);
            calibration = Quaternion.Inverse(converted);
        }

        private float ApplyDeadzone(float value, float deadzone)
        {
            if (Mathf.Abs(value) < deadzone) return 0f;
            return value;
        }
    }
}