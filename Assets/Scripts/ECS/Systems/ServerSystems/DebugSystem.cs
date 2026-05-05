using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Collision;
using Assets.Scripts.ECS.Components.Projectiles;
using Assets.Scripts.ECS.Components.SpatialHashing;
using Assets.Scripts.ECS.Systems.ServerSystems;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

#if UNITY_EDITOR

[UpdateAfter(typeof(CollisionServerSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ServerCollisionGizmoSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        DrawBoundingSpheres(ref state);
        DrawShapes(ref state);
        DrawProjectiles(ref state);
    }

    private void DrawBoundingSpheres(ref SystemState state)
    {
        foreach (var (transform, cell, layer) in SystemAPI
            .Query<RefRO<LocalTransform>,
                   RefRO<SpatialCellComponent>,
                   RefRO<CollisionLayerComponent>>())
        {
            var pos = transform.ValueRO.Position;
            float r = cell.ValueRO.BoundingRadius;
            var color = layer.ValueRO.Layer switch
            {
                CollisionLayer.Ship => Color.green,
                CollisionLayer.Projectile => Color.red,
                CollisionLayer.Asteroid => Color.yellow,
                _ => Color.white
            };

            DrawSphere(pos, r, color);
        }
    }

    private void DrawShapes(ref SystemState state)
    {
        foreach (var (transform, shapes) in SystemAPI
            .Query<RefRO<LocalTransform>,
                   DynamicBuffer<ColliderShapeBufferElement>>())
        {
            var pos = transform.ValueRO.Position;
            var rot = transform.ValueRO.Rotation;

            foreach (var shape in shapes)
            {
                var worldPos = pos + math.rotate(rot, shape.LocalOffset);
                var worldRot = math.mul(rot, shape.LocalRotation);

                switch (shape.ShapeType)
                {
                    case ColliderShapeType.Sphere:
                        DrawSphere(worldPos, shape.Params.x, Color.cyan);
                        break;

                    case ColliderShapeType.Capsule:
                        DrawCapsule(worldPos, worldRot, shape.Params.x, shape.Params.y, Color.magenta);
                        break;

                    case ColliderShapeType.Box:
                        DrawBox(worldPos, worldRot, shape.Params, Color.blue);
                        break;
                }
            }
        }
    }

    private void DrawProjectiles(ref SystemState state)
    {
        foreach (var (transform, projectile) in SystemAPI
            .Query<RefRO<LocalTransform>, RefRO<ServerProjectileComponent>>())
        {
            var pos = transform.ValueRO.Position;
            float s = 0.3f;
            Debug.DrawLine(
                new Vector3(pos.x - s, pos.y, pos.z),
                new Vector3(pos.x + s, pos.y, pos.z),
                Color.red);
            Debug.DrawLine(
                new Vector3(pos.x, pos.y - s, pos.z),
                new Vector3(pos.x, pos.y + s, pos.z),
                Color.red);
            Debug.DrawLine(
                new Vector3(pos.x, pos.y, pos.z - s),
                new Vector3(pos.x, pos.y, pos.z + s),
                Color.red);

            // Draw velocity direction
            var fwd = math.rotate(transform.ValueRO.Rotation, new float3(0, 0, 1));
            Debug.DrawLine(
                new Vector3(pos.x, pos.y, pos.z),
                new Vector3(pos.x + fwd.x, pos.y + fwd.y, pos.z + fwd.z),
                Color.yellow);
        }
    }

    private static void DrawSphere(float3 center, float radius, Color color)
    {
        int segments = 16;
        float step = math.PI * 2f / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step, a1 = (i + 1) * step;

            Debug.DrawLine(
                new Vector3(center.x + math.cos(a0) * radius, center.y + math.sin(a0) * radius, center.z),
                new Vector3(center.x + math.cos(a1) * radius, center.y + math.sin(a1) * radius, center.z),
                color);
            Debug.DrawLine(
                new Vector3(center.x + math.cos(a0) * radius, center.y, center.z + math.sin(a0) * radius),
                new Vector3(center.x + math.cos(a1) * radius, center.y, center.z + math.sin(a1) * radius),
                color);
            Debug.DrawLine(
                new Vector3(center.x, center.y + math.cos(a0) * radius, center.z + math.sin(a0) * radius),
                new Vector3(center.x, center.y + math.cos(a1) * radius, center.z + math.sin(a1) * radius),
                color);
        }
    }

    private static void DrawCapsule(float3 center, quaternion rot, float radius, float halfHeight, Color color)
    {
        float3 up = math.rotate(rot, new float3(0, 1, 0));
        float3 top = center + up * halfHeight;
        float3 bot = center - up * halfHeight;

        DrawSphere(top, radius, color);
        DrawSphere(bot, radius, color);

        float3 right = math.rotate(rot, new float3(1, 0, 0)) * radius;
        float3 forward = math.rotate(rot, new float3(0, 0, 1)) * radius;

        Debug.DrawLine(
            new Vector3(top.x + right.x, top.y + right.y, top.z + right.z),
            new Vector3(bot.x + right.x, bot.y + right.y, bot.z + right.z),
            color);
        Debug.DrawLine(
            new Vector3(top.x - right.x, top.y - right.y, top.z - right.z),
            new Vector3(bot.x - right.x, bot.y - right.y, bot.z - right.z),
            color);
        Debug.DrawLine(
            new Vector3(top.x + forward.x, top.y + forward.y, top.z + forward.z),
            new Vector3(bot.x + forward.x, bot.y + forward.y, bot.z + forward.z),
            color);
        Debug.DrawLine(
            new Vector3(top.x - forward.x, top.y - forward.y, top.z - forward.z),
            new Vector3(bot.x - forward.x, bot.y - forward.y, bot.z - forward.z),
            color);
    }

    private static void DrawBox(float3 center, quaternion rot, float3 halfExtents, Color color)
    {
        float3[] corners = new float3[8];
        int idx = 0;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    corners[idx++] = center + math.rotate(rot,
                        new float3(x * halfExtents.x, y * halfExtents.y, z * halfExtents.z));

        // 12 edges
        int[,] edges =
        {
            {0,1},{1,3},{3,2},{2,0}, 
            {4,5},{5,7},{7,6},{6,4},
            {0,4},{1,5},{2,6},{3,7}
        };

        for (int i = 0; i < 12; i++)
        {
            var a = corners[edges[i, 0]];
            var b = corners[edges[i, 1]];
            Debug.DrawLine(
                new Vector3(a.x, a.y, a.z),
                new Vector3(b.x, b.y, b.z),
                color);
        }
    }
}
#endif