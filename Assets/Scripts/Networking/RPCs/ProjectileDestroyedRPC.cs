using Assets.Scripts.MVVM.Models.DamageTypes;
using Unity.Mathematics;
using Unity.NetCode;

namespace Assets.Scripts.Networking.RPCs
{
    public struct ProjectileDestroyedRPC : IRpcCommand
    {
        public uint ProjectileId;
        public float3 Position;
        public DamageType DamageType;
        public ProjectileDestroyReason DestroyReason;
    }

    public enum ProjectileDestroyReason : byte
    {
        Hit,
        Lifetime
    }
}
