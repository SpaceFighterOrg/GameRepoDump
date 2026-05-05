using Assets.Scripts.Authorings;
using Assets.Scripts.ECS.Components.Collision;
using Unity.Entities;
using UnityEngine;

public class DebugAuthoring : MonoBehaviour
{
    class Baker : BakerBase<DebugAuthoring>
    {
        public override void Bake(DebugAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new HealthComponent { Value = 100 });
            AddColliders(entity, authoring, CollisionLayer.Asteroid, CollisionLayers.AsteroidCollisionMask);
        }
    }
}
