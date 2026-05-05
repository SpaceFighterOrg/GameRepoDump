using Assets.Scripts.MVVM.Models.WeaponTypes;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MVVM.Models.Ships
{
    [CreateAssetMenu(fileName = "ShipData", menuName = "Scriptable Objects/ShipData")]
    public class ShipData : ScriptableObject
    {
        public int MaxHealth;
        public int MaxSpeed;
        public int Acceleration;
        public int Deceleration;
        public int MaxRotationSpeed;
        public int MaxShield;
        public int ShieldRechargeRate;
        public List<WeaponType> Weapons;
    }
}