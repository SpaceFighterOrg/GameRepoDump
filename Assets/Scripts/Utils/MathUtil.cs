using Unity.Mathematics;

namespace Assets.Scripts.Utils
{
    public static class MathUtil
    {
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            float diff = target - current;
            if (math.abs(diff) <= maxDelta) return target;
            return current + math.sign(diff) * maxDelta;
        }
    }
}
