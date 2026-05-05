using Unity.Entities;

public struct HealthComponent : IComponentData
{
    public int Value;
    public int MaxValue;

    public int IonDisableThreshold;
    public float IonDisableDuration;
    public int IonDamageTaken;
}
