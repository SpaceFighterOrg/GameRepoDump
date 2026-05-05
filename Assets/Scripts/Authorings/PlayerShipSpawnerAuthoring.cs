using Assets.Scripts.ECS.Components;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public class PlayerShipSpawnerAuthoring : MonoBehaviour
    {
        public GameObject ShipPrefab;

        class Baker : Baker<PlayerShipSpawnerAuthoring>
        {
            public override void Bake(PlayerShipSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new PlayerShipSpawnerComponent
                {
                    Prefab = GetEntity(authoring.ShipPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}