using System;
using Unity.Entities;

namespace Assets.Scripts.ECS.Components.Collision
{
    [Flags]
    public enum CollisionLayer : byte
    {
        None = 0,
        Projectile = 1 << 0,
        Ship = 1 << 1,
        Asteroid = 1 << 2,
    }

    public struct CollisionLayerComponent : IComponentData
    {
        public CollisionLayer Layer;
        public CollisionLayer CollidesWithMask;
    }

    public static class CollisionLayers
    {
        public static CollisionLayer ShipCollisionMask => (CollisionLayer.Projectile | CollisionLayer.Asteroid | CollisionLayer.Ship);
        public static CollisionLayer ProjectileCollisionMask => (CollisionLayer.Ship | CollisionLayer.Asteroid);
        public static CollisionLayer AsteroidCollisionMask => (CollisionLayer.Ship | CollisionLayer.Projectile | CollisionLayer.Asteroid);
    }
}