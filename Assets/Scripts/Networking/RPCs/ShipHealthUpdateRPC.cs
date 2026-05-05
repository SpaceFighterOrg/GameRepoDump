using Unity.NetCode;

namespace Assets.Scripts.Networking.RPCs
{
    public struct ShipHealthUpdateRPC : IRpcCommand
    {
        public int Health;
        public int MaxHealth;
        public int Shield;
        public int MaxShield;
    }
}
