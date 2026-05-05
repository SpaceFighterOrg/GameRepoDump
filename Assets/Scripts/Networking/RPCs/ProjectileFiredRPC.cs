using Unity.Mathematics;
using Unity.NetCode;

namespace Assets.Scripts.Networking.RPCs
{
    public struct ProjectileFiredRPC : IRpcCommand
    {
        public float3 Position;
        public quaternion Rotation;

        public int FiringNetworkId;
        public int WeaponTypeId;

        public uint ProjectileId;
    }
}