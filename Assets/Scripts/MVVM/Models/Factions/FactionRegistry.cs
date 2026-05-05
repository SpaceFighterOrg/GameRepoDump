using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "FactionRegistry", menuName = "Scriptable Objects/Faction Registry")]
public class FactionRegistry : ScriptableObject
{
    public List<FactionConfig> Factions;

    public FactionConfig Get(int factionId) =>
        Factions.FirstOrDefault(f => f.FactionId == factionId);
}
