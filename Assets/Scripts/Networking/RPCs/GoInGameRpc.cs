using Unity.Collections;
using Unity.NetCode;

namespace Assets.Scripts.Networking.RPCs
{
    public struct GoInGameRpc : IRpcCommand
    {
        public FixedString128Bytes MatchId;
        public byte MatchSlotIndex;
        public byte Team;
        public int AttackingPlanetId;
        public int BackendPlayerId;
        public int FactionId;
        public FixedString128Bytes Username;
    }
}
