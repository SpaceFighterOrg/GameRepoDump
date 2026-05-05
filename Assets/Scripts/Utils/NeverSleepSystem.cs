using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class NeverSleepSystem : MonoBehaviour
    {
        void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}