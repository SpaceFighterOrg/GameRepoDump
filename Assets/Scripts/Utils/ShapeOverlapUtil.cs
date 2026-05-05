using Assets.Scripts.ECS.Buffers;
using Assets.Scripts.ECS.Components.Collision;
using Unity.Mathematics;

namespace Assets.Scripts.Utils
{
    public static class ShapeOverlapUtil
    {
        public static bool Overlaps(
            in ColliderShapeBufferElement a, float3 posA, quaternion rotA,
            in ColliderShapeBufferElement b, float3 posB, quaternion rotB)
        {
            float3 worldPosA = posA + math.rotate(rotA, a.LocalOffset);
            float3 worldPosB = posB + math.rotate(rotB, b.LocalOffset);
            quaternion worldRotA = math.mul(rotA, a.LocalRotation);
            quaternion worldRotB = math.mul(rotB, b.LocalRotation);

            if (a.ShapeType == ColliderShapeType.Sphere)
            {
                if (b.ShapeType == ColliderShapeType.Sphere)
                    return SphereSphere(worldPosA, a.Params.x, worldPosB, b.Params.x);
                if (b.ShapeType == ColliderShapeType.Capsule)
                    return SphereCapsule(worldPosA, a.Params.x, worldPosB, worldRotB, b.Params.x, b.Params.y);
                if (b.ShapeType == ColliderShapeType.Box)
                    return SphereBox(worldPosA, a.Params.x, worldPosB, worldRotB, b.Params);
            }
            else if (a.ShapeType == ColliderShapeType.Capsule)
            {
                if (b.ShapeType == ColliderShapeType.Sphere)
                    return SphereCapsule(worldPosB, b.Params.x, worldPosA, worldRotA, a.Params.x, a.Params.y);
                if (b.ShapeType == ColliderShapeType.Capsule)
                    return CapsuleCapsule(worldPosA, worldRotA, a.Params.x, a.Params.y, worldPosB, worldRotB, b.Params.x, b.Params.y);
                if (b.ShapeType == ColliderShapeType.Box)
                    return CapsuleBox(worldPosA, worldRotA, a.Params.x, a.Params.y, worldPosB, worldRotB, b.Params);
            }
            else if (a.ShapeType == ColliderShapeType.Box)
            {
                if (b.ShapeType == ColliderShapeType.Sphere)
                    return SphereBox(worldPosB, b.Params.x, worldPosA, worldRotA, a.Params);
                if (b.ShapeType == ColliderShapeType.Capsule)
                    return CapsuleBox(worldPosB, worldRotB, b.Params.x, b.Params.y, worldPosA, worldRotA, a.Params);
                if (b.ShapeType == ColliderShapeType.Box)
                    return BoxBox(worldPosA, worldRotA, a.Params, worldPosB, worldRotB, b.Params);
            }
            return false;
        }

        private static bool SphereSphere(float3 posA, float rA, float3 posB, float rB)
        {
            float3 diff = posB - posA;
            float combinedR = rA + rB;
            return math.dot(diff, diff) < combinedR * combinedR;
        }

        private static bool SphereCapsule(
            float3 spherePos, float sphereR,
            float3 capsulePos, quaternion capsuleRot, float capsuleR, float capsuleHalfH)
        {
            float3 axis = math.rotate(capsuleRot, math.up());
            float3 base_ = capsulePos - axis * capsuleHalfH;
            float3 tip = capsulePos + axis * capsuleHalfH;
            float3 closest = ClosestPointOnSegment(spherePos, base_, tip);
            float3 diff = spherePos - closest;
            float combinedR = sphereR + capsuleR;
            return math.dot(diff, diff) < combinedR * combinedR;
        }

        private static bool SphereBox(
            float3 spherePos, float sphereR,
            float3 boxPos, quaternion boxRot, float3 halfExtents)
        {
            float3 local = math.rotate(math.conjugate(boxRot), spherePos - boxPos);
            float3 clamped = math.clamp(local, -halfExtents, halfExtents);
            float3 diff = local - clamped;
            return math.dot(diff, diff) < sphereR * sphereR;
        }

        private static bool CapsuleCapsule(
            float3 posA, quaternion rotA, float rA, float halfHA,
            float3 posB, quaternion rotB, float rB, float halfHB)
        {
            float3 axisA = math.rotate(rotA, math.up());
            float3 axisB = math.rotate(rotB, math.up());
            ClosestPointsOnSegments(
                posA - axisA * halfHA, posA + axisA * halfHA,
                posB - axisB * halfHB, posB + axisB * halfHB,
                out float3 ptA, out float3 ptB
            );
            float3 diff = ptA - ptB;
            float combinedR = rA + rB;
            return math.dot(diff, diff) < combinedR * combinedR;
        }

        private static bool BoxBox(
            float3 posA, quaternion rotA, float3 halfA,
            float3 posB, quaternion rotB, float3 halfB)
        {
            float3x3 rA = new float3x3(rotA);
            float3x3 rB = new float3x3(rotB);
            float3 t = posB - posA;

            for (int i = 0; i < 3; i++)
                if (SeparatedOnAxis(rA[i], in t, in rA, in rB, in halfA, in halfB)) return false;
            for (int i = 0; i < 3; i++)
                if (SeparatedOnAxis(rB[i], in t, in rA, in rB, in halfA, in halfB)) return false;

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    float3 axis = math.cross(rA[i], rB[j]);
                    float lenSq = math.dot(axis, axis);
                    if (lenSq < 1e-6f) continue;
                    if (SeparatedOnAxis(axis * math.rsqrt(lenSq), in t, in rA, in rB, in halfA, in halfB)) return false;
                }
            return true;
        }

        private static bool CapsuleBox(
            float3 capsulePos, quaternion capsuleRot, float capsuleR, float capsuleHalfH,
            float3 boxPos, quaternion boxRot, float3 halfExtents)
        {
            float3 axis = math.rotate(capsuleRot, math.up());
            float3 base_ = capsulePos - axis * capsuleHalfH;
            float3 tip = capsulePos + axis * capsuleHalfH;

            quaternion invBoxRot = math.conjugate(boxRot);
            float3 localBase = math.rotate(invBoxRot, base_ - boxPos);
            float3 localTip = math.rotate(invBoxRot, tip - boxPos);

            float3 closest = ClosestPointOnSegmentToAABB(localBase, localTip, halfExtents);
            float3 clampedToBox = math.clamp(closest, -halfExtents, halfExtents);
            float3 diff = closest - clampedToBox;
            return math.dot(diff, diff) < capsuleR * capsuleR;
        }

        private static float3 ClosestPointOnSegment(float3 point, float3 a, float3 b)
        {
            float3 ab = b - a;
            float denom = math.dot(ab, ab);
            if (denom < 1e-10f) return a;
            float t = math.clamp(math.dot(point - a, ab) / denom, 0f, 1f);
            return a + t * ab;
        }

        private static void ClosestPointsOnSegments(
            float3 a0, float3 a1, float3 b0, float3 b1,
            out float3 closestA, out float3 closestB)
        {
            float3 d1 = a1 - a0;
            float3 d2 = b1 - b0;
            float3 r = a0 - b0;
            float a = math.dot(d1, d1);
            float e = math.dot(d2, d2);
            float f = math.dot(d2, r);
            float s, t;

            if (a < 1e-6f && e < 1e-6f) { s = t = 0f; }
            else if (a < 1e-6f) { s = 0f; t = math.clamp(f / e, 0f, 1f); }
            else
            {
                float c = math.dot(d1, r);
                if (e < 1e-6f)
                {
                    t = 0f;
                    s = math.clamp(-c / a, 0f, 1f);
                }
                else
                {
                    float b = math.dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom > 1e-6f ? math.clamp((b * f - c * e) / denom, 0f, 1f) : 0f;
                    t = math.clamp((b * s + f) / e, 0f, 1f);
                    s = math.clamp((b * t - c) / a, 0f, 1f);
                }
            }
            closestA = a0 + s * d1;
            closestB = b0 + t * d2;
        }

        private static float3 ClosestPointOnSegmentToAABB(float3 p0, float3 p1, float3 half)
        {
            float3 clamp0 = math.clamp(p0, -half, half);
            float3 clamp1 = math.clamp(p1, -half, half);
            float d0 = math.dot(p0 - clamp0, p0 - clamp0);
            float d1 = math.dot(p1 - clamp1, p1 - clamp1);
            if (math.abs(d0 - d1) < 1e-6f)
                return (p0 + p1) * 0.5f;
            return d0 < d1 ? p0 : p1;
        }

        private static bool SeparatedOnAxis(
            float3 axis, in float3 t,
            in float3x3 rA, in float3x3 rB,
            in float3 halfA, in float3 halfB)
        {
            float projT = math.abs(math.dot(t, axis));
            float projA = halfA.x * math.abs(math.dot(rA[0], axis))
                         + halfA.y * math.abs(math.dot(rA[1], axis))
                         + halfA.z * math.abs(math.dot(rA[2], axis));
            float projB = halfB.x * math.abs(math.dot(rB[0], axis))
                         + halfB.y * math.abs(math.dot(rB[1], axis))
                         + halfB.z * math.abs(math.dot(rB[2], axis));
            return projT > projA + projB;
        }
    }
}