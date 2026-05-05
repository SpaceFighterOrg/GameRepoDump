using Assets.Scripts.Utils;
using GenericEventSystem.EventData;
using UnityEngine;

namespace Assets.Scripts.MVVM.Views.GalaxyMap
{
    [RequireComponent(typeof(LineRenderer))]
    public class ConnectionView : ViewBase
    {
        private LineRenderer _line;
        private int _fromPlanetId;
        private int _toPlanetId;
        [SerializeField] private float _lineWidth = 0.7f;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.startWidth = _lineWidth;
            _line.endWidth = _lineWidth;
        }

        public void Bind(PlanetView from, PlanetView to)
        {
            _fromPlanetId = from.PlnaetId;
            _toPlanetId = to.PlnaetId;

            _line.SetPosition(0, from.transform.position);
            _line.SetPosition(1, to.transform.position);

            UpdateConnectionColor(SessionManager.LocationPlanetId);
        }

        public void HandlePlanetChanged(EventData data)
        {
            var planetChangedData = data as PlanetIdEventData;
            UpdateConnectionColor(planetChangedData.PlanetId);
        }

        private void UpdateConnectionColor(int planetId)
        {
            if (planetId == _fromPlanetId || planetId == _toPlanetId)
                _line.material.SetColor("_BaseColor", Color.red);
            else
                _line.material.SetColor("_BaseColor", Color.white);
        }
    }
}