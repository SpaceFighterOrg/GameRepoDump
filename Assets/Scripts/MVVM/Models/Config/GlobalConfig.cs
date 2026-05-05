using Assets.Scripts.MVVM.Models.Ships;
using UnityEngine;

namespace Assets.Scripts.MVVM.Models.Config
{
    [CreateAssetMenu(fileName = "GlobalConfig", menuName = "Scriptable Objects/Global Config")]
    public class GlobalConfig : ScriptableObject
    {
        public ShipData playerShip;
    }
}