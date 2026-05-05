using Assets.Scripts.Authorings;
using Assets.Scripts.ECS.Components.Collision;
using Assets.Scripts.ECS.Components.Projectiles;
using Unity.Entities;
using UnityEngine;

public class ServerProjectilePrefabAuthoring : MonoBehaviour
{
    class Baker : BakerBase<ServerProjectilePrefabAuthoring>
    {
        public override void Bake(ServerProjectilePrefabAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddColliders(entity, authoring, CollisionLayer.Projectile, CollisionLayers.ProjectileCollisionMask);    
            AddComponent(entity, new ServerProjectileComponent());
        }
    }
}
