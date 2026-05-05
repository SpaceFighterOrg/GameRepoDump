using System.Collections.Generic;

namespace Assets.Scripts.ECS.Matchmaking
{
    public class MatchSlotState
    {
        public byte SlotIndex;
        public string MatchId;
        public bool IsActive;

        public int Team1LifePool;
        public int Team2LifePool;

        // Planet/faction context is per-player (each player fights for a different planet): PlayerMatchInfo.AttackingPlanetId and .FactionId.
        public Dictionary<uint, PlayerMatchInfo> Players = new();
    }
}
