using Assets.Scripts.MVVM.Models.Projectiles;
using UnityEngine;

namespace Assets.Scripts.MVVM.Models.WeaponTypes
{
    [CreateAssetMenu(fileName = "WeaponType", menuName = "Scriptable Objects/Weapon Type")]
    public class WeaponType : ScriptableObject
    {
        public int Id;
        public ProjectileType ProjectileType;
        public float FireRate;
    }
}