using Unity.Entities;

public struct VelocityComponent : IComponentData
{
    public float Velocity;
    public float MaxVelocity;

    public float Acceleration;
    public float Deceleration;
}
