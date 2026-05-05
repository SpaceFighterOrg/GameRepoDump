using Unity.NetCode;

namespace Assets.Scripts.Networking.RPCs
{
    public struct GameOverRPC : IRpcCommand
    {
        public byte WinningTeam;
        public int ContributionAmount;
        public int AttackingPlanetId;
        public int DefendingPlanetId;
        public int FactionId;
    }
}
