using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Spawners;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public class AsteroidRegistryAuthoring : MonoBehaviour
    {
        [SerializeField] private float _scaleMin = 0.5f;
        [SerializeField] private float _scaleMax = 2f;
        [SerializeField] private float _spawnRadius = 50f;
        [SerializeField] private int _count = 50;
        [SerializeField] private byte _matchSlotIndex = 0;

        [SerializeField] private List<GameObject> _prefabs;

        public class Baker : BakerBase<AsteroidRegistryAuthoring>
        {
            public override void Bake(AsteroidRegistryAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AsteroidSpawnerComponent
                {
                    ScaleMin = authoring._scaleMin,
                    ScaleMax = authoring._scaleMax,
                    SpawnRadius = authoring._spawnRadius,
                    Count = authoring._count,
                    Seed = (uint)System.Math.Abs(authoring.GetInstanceID()),
                    MatchSlotIndex = authoring._matchSlotIndex,
                });

                var buffer = AddBuffer<AsteroidRegistryBufferElement>(entity);
                foreach (var prefab in authoring._prefabs)
                {
                    buffer.Add(new AsteroidRegistryBufferElement
                    {
                        Prefab = GetEntity(prefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }
}
