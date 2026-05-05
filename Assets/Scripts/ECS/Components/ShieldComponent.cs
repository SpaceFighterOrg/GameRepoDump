using Unity.Entities;

public struct ShieldComponent : IComponentData
{
    public int Value;
    public float TimeSinceLastHit;
    public int MaxValue;
    public int RegenRate;
    public int RegenValue;
}
