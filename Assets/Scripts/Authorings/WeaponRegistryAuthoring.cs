#if !UNITY_SERVER || UNITY_EDITOR

using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.MVVM.Models.WeaponTypes;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public class WeaponRegistryAuthoring : MonoBehaviour
    {
        public class Baker : Baker<WeaponRegistryAuthoring>
        {
            public override void Bake(WeaponRegistryAuthoring authoring)
            {
#if UNITY_EDITOR
                var guids = AssetDatabase.FindAssets("t:WeaponType");
                var weapons = new WeaponType[guids.Length];
                int maxId = 0;

                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    weapons[i] = AssetDatabase.LoadAssetAtPath<WeaponType>(path);
                    if (weapons[i] != null && weapons[i].Id > maxId)
                        maxId = weapons[i].Id;
                }

                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<WeaponRegistryBufferElement>(entity);

                for (int i = 0; i <= maxId; i++)
                    buffer.Add(default);

                foreach (var weapon in weapons)
                {
                    if (weapon == null) continue;

                    buffer[weapon.Id] = new WeaponRegistryBufferElement
                    {
                        WeaponTypeId = weapon.Id,
                        ProjectileSpeed = weapon.ProjectileType.Speed,
                        ProjectileLifetime = weapon.ProjectileType.Lifetime,
                        ProjectileDamage = weapon.ProjectileType.Damage,
                        ProjectileDamageType = weapon.ProjectileType.DamageType,
                        ClientProjectilePrefab = GetEntity(
                            weapon.ProjectileType.ClientPrefab,
                            TransformUsageFlags.Dynamic
                        ),
                        ServerProjectilePrefab = GetEntity(
                            weapon.ProjectileType.ServerPrefab,
                            TransformUsageFlags.Dynamic
                        )
                    };
                }
#endif
            }
        }
    }
}
#endif