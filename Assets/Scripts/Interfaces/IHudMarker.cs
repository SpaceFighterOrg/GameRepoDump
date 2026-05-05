using UnityEngine;

namespace Assets.Scripts.Interfaces
{
    public interface IHudMarker
    {
        void SetTeamColor(bool isFriendly);
        void SetPosition(Vector3 worldPosition);
        void SetData(string playerName, float distance, Vector3 predictionWidgetPosition);
        Transform GetTransform();
    }
}
