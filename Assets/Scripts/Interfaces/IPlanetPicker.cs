using UnityEngine;

namespace Assets.Scripts.Interfaces
{
    public interface IPlanetPicker
    {
        int GetPlanetIdAtWorldPos(Vector3 worldPos);
    }
}
