using Assets.Scripts.Interfaces;
using GenericEventSystem;
using GenericEventSystem.EventData;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Controllers
{

    public class ProvinceSelectionController : ControllerBase
    {
        [Header("References")]
        [SerializeField] private MonoBehaviour _planetPickerSource;
        [SerializeField] EventDefinition _planetSelectedChannel;

        private IPlanetPicker _planetPicker;

        [Header("Settings")]
        [Tooltip("The Z depth of the flat plane the map geometry lives on in world space.")]
        [SerializeField] private float _worldPlaneZ = 0f;

        [Tooltip("Camera used for raycasting. Defaults to Camera.main if unset.")]
        [SerializeField] private Camera _camera;

        private int _lastSelectedPlanetId = -1;
        private Plane _mapPlane;

        private void Awake()
        {
            _planetPicker = _planetPickerSource as IPlanetPicker;
            if (_camera == null) _camera = Camera.main;
            RebuildPlane();
        }

        private void Update()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                if (IsPointerOverUIToolkit(mousePos)) return;
                TrySelect(mousePos);
            }
#else
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                if (IsPointerOverUIToolkit(Input.GetTouch(0).position)) return;
                TrySelect(Input.GetTouch(0).position);
            }
#endif
        }

        private void TrySelect(Vector2 screenPos)
        {
            if (!TryGetWorldPos(screenPos, out Vector3 worldPos)) return;

            int planetId = _planetPicker?.GetPlanetIdAtWorldPos(worldPos) ?? -1;

            if (planetId == _lastSelectedPlanetId)
            {
                _planetSelectedChannel.Raise(new PlanetIdEventData { PlanetId = -1 });
                _lastSelectedPlanetId = -1;
            }
            else
            {
                _planetSelectedChannel.Raise(new PlanetIdEventData { PlanetId = planetId });
                _lastSelectedPlanetId = planetId;
            }
        }

        private bool TryGetWorldPos(Vector2 screenPos, out Vector3 worldPos)
        {
            Ray ray = _camera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));

            if (_mapPlane.Raycast(ray, out float enter))
            {
                worldPos = ray.GetPoint(enter);
                return true;
            }

            worldPos = Vector3.zero;
            return false;
        }

        public void SetWorldPlaneZ(float z)
        {
            _worldPlaneZ = z;
            RebuildPlane();
        }

        private void RebuildPlane()
        {
            _mapPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, _worldPlaneZ));
        }
    }
}