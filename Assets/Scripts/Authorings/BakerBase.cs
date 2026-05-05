using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Collision;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Scripts.Authorings
{
    public abstract class BakerBase<TAuthoringType> : Baker<TAuthoringType> where TAuthoringType : Component
    {
        protected void AddColliders(Entity entity, TAuthoringType authoring, CollisionLayer layer, CollisionLayer collidesWithMask)
        {
            var collisionBuffer = AddBuffer<ColliderShapeBufferElement>(entity);
            AddComponent(entity, new CollisionLayerComponent
            {
                Layer = layer,
                CollidesWithMask = collidesWithMask
            });

            float boundingRadius = 0;

            foreach (var col in authoring.GetComponentsInChildren<BoxCollider>())
            {
                var localPos = (float3)authoring.transform.InverseTransformPoint(col.transform.position);
                var localRot = Quaternion.Inverse(authoring.transform.rotation) * col.transform.rotation;
                var halfExtents = (float3)col.size * 0.5f;

                collisionBuffer.Add(new ColliderShapeBufferElement
                {
                    ShapeType = ColliderShapeType.Box,
                    LocalOffset = localPos,
                    LocalRotation = localRot,
                    Params = halfExtents
                });

                boundingRadius = math.max(boundingRadius,
                    math.length(localPos) + math.length(halfExtents));
            }

            foreach (var col in authoring.GetComponentsInChildren<SphereCollider>())
            {
                var localPos = (float3)authoring.transform.InverseTransformPoint(col.transform.position);
                var localRot = Quaternion.Inverse(authoring.transform.rotation) * col.transform.rotation;
                float radius = col.radius * col.transform.lossyScale.x / authoring.transform.lossyScale.x;

                collisionBuffer.Add(new ColliderShapeBufferElement
                {
                    ShapeType = ColliderShapeType.Sphere,
                    LocalOffset = localPos,
                    LocalRotation = localRot,
                    Params = new float3(radius, 0f, 0f)
                });

                boundingRadius = math.max(boundingRadius,
                    math.length(localPos) + radius);
            }

            foreach (var col in authoring.GetComponentsInChildren<CapsuleCollider>())
            {
                var localPos = (float3)authoring.transform.InverseTransformPoint(col.transform.position);
                var capsuleUp = col.direction switch
                {
                    0 => Quaternion.FromToRotation(Vector3.right, Vector3.up),
                    2 => Quaternion.FromToRotation(Vector3.forward, Vector3.up),
                    _ => Quaternion.identity
                };
                var localRot = Quaternion.Inverse(authoring.transform.rotation)
                             * col.transform.rotation
                             * capsuleUp;

                float radius = col.radius * col.transform.lossyScale.x / authoring.transform.lossyScale.x;
                float halfHeight = math.max(
                    col.height * col.transform.lossyScale.y / authoring.transform.lossyScale.y * 0.5f,
                    radius);

                collisionBuffer.Add(new ColliderShapeBufferElement
                {
                    ShapeType = ColliderShapeType.Capsule,
                    LocalOffset = localPos,
                    LocalRotation = localRot,
                    Params = new float3(radius, halfHeight, 0f)
                });

                boundingRadius = math.max(boundingRadius,
                    math.length(localPos) + radius + halfHeight);
            }

            AddComponent(entity, new BoundingRadiusComponent { Value = boundingRadius });
        }
    }
}