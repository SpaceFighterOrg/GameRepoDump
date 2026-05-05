using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class CameraAdjuster : MonoBehaviour
    {
        public Vector3 startPos;
        public Quaternion startRot;

        void Awake()
        {
            startPos = transform.position;
            startRot = transform.rotation;
        }

        void OnDisable()
        {
            transform.position = startPos;
            transform.rotation = startRot;
        }
    }
}