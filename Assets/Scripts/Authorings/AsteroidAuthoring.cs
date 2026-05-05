using Assets.Scripts.ECS.Components.Collision;
using Assets.Scripts.ECS.Components.Tags;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public class AsteroidAuthoring : MonoBehaviour
    {
        public class Baker : BakerBase<AsteroidAuthoring>
        {
            public override void Bake(AsteroidAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.Renderable);
                AddComponent(entity, new AsteroidTag());
                AddColliders(entity, authoring, CollisionLayer.Asteroid, CollisionLayers.AsteroidCollisionMask);
            }
        }
    }
}
