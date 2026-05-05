using Backend.Common.DTOs.Map;
using GenericEventSystem.EventData;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.MVVM.Views.GalaxyMap
{
    public class GalaxyMapView : ViewBase
    {
        [Header("References")]
        public FactionRegistry factionRegistry;

        [Header("Prefabs")]
        public PlanetView planetPrefab;
        public ConnectionView connectionPrefab;

        private Dictionary<int, PlanetView> _spawnedPlanets = new();

        public void HandlePlanetsChangedEvent(EventData eventData)
        {
            var data = eventData as GalaxyChangedEventData;
            DrawPlanets(data.Galaxy);
        }

        public void DrawPlanets(GalaxyViewDTO dto)
        {
            ClearAll();

            foreach (var planet in dto.Planets)
            {
                var faction = factionRegistry.Get(planet.FactionId);
                var view = Instantiate(planetPrefab, transform);
                view.Bind(planet);
                _spawnedPlanets[planet.Id] = view;
            }

            var drawnConnections = new HashSet<(int, int)>();
            foreach (var planet in dto.Planets)
            {
                foreach (var targetId in planet.ConnectedPlanetIds)
                {
                    var key = planet.Id < targetId ? (planet.Id, targetId) : (targetId, planet.Id);
                    if (drawnConnections.Contains(key)) continue;

                    if (_spawnedPlanets.TryGetValue(targetId, out var target))
                    {
                        var connection = Instantiate(connectionPrefab, transform);
                        connection.Bind(_spawnedPlanets[planet.Id], target);
                        drawnConnections.Add(key);
                    }
                }
            }
        }

        private void ClearAll()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            _spawnedPlanets.Clear();
        }
    }
}