using UnityEngine;
namespace Assets.Scripts.MVVM.Views.GalaxyMap
{
    public class PlanetLabelView : ViewBase
    {
        [SerializeField] private float _zoomLabelMultiplier = 1f;
        private Transform _refPoint;
        private Camera _cam;

        void Start()
        {
            _cam = Camera.main;
            _refPoint = transform.parent.parent;
        }

        void LateUpdate()
        {
            float cameraDiff = Mathf.Abs(_cam.transform.position.z - _refPoint.position.z) * _zoomLabelMultiplier;
            transform.localScale = new Vector3(cameraDiff, cameraDiff, cameraDiff);
        }
    }
}