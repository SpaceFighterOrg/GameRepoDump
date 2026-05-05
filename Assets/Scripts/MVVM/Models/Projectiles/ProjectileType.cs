using Assets.Scripts.MVVM.Models.DamageTypes;
using UnityEngine;

namespace Assets.Scripts.MVVM.Models.Projectiles
{
    [CreateAssetMenu(fileName = "ProjectileType", menuName = "Scriptable Objects/Projectile Type")]
    public class ProjectileType : ScriptableObject
    {
        public GameObject ClientPrefab;
        public GameObject ServerPrefab;

        public Sprite Icon;
        public Color IconColor;

        public float Speed;
        public int Lifetime;

        public int Damage;
        public DamageType DamageType;
    }
}