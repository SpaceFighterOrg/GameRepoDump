using UnityEngine;

[CreateAssetMenu(fileName = "FactionConfig", menuName = "Scriptable Objects/Faction Config")]
public class FactionConfig : ScriptableObject
{
    public int FactionId;
    public string FactionName;
    public Color Color;
    public Sprite Icon;
}