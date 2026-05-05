using Unity.NetCode;

namespace Assets.Scripts.Networking.RPCs
{
    public struct LiveLostRPC : IRpcCommand
    {
        public byte TeamThatLost;
        public int Team1LivesRemaining;
        public int Team2LivesRemaining;
    }
}
