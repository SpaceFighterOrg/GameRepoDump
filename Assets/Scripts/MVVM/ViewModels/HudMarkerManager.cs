using Assets.Scripts.Interfaces;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.MVVM.ViewModels
{
    public class HudMarkerManager : MonoBehaviour
    {
        public static HudMarkerManager Instance { get; private set; }

        [SerializeField] private GameObject _hudPrefab;

        private readonly Dictionary<Entity, IHudMarker> _hudMap = new();

        private void Awake() => Instance = this;

        public IHudMarker GetOrCreate(Entity entity, World world)
        {
            if (_hudMap.TryGetValue(entity, out var existing))
                return existing;

            var go = Instantiate(_hudPrefab);
            IHudMarker marker = null;
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb is IHudMarker m) { marker = m; break; }
            }

            _hudMap[entity] = marker;
            return marker;
        }

        public void Remove(Entity entity)
        {
            if (_hudMap.TryGetValue(entity, out var marker))
            {
                if (marker is MonoBehaviour mb) Destroy(mb.gameObject);
                _hudMap.Remove(entity);
            }
        }
    }
}
