using TMPro;
using UnityEngine;

namespace Assets.Scripts.MVVM
{
    public class DistanceWidgetView : MonoBehaviour
    {
        [SerializeField] private float _zoomLabelMultiplier = 1f;
        private Transform _refPoint;
        private Camera _cam;
        private TextMeshPro _text;

        void Start()
        {
            _cam = Camera.main;
            _refPoint = transform;
            _text = GetComponent<TextMeshPro>();
        }

        void LateUpdate()
        {
            _text.text = $"{(transform.position - _cam.transform.position).magnitude} u";
            float cameraDiff = Mathf.Abs(_cam.transform.position.z - _refPoint.position.z) * _zoomLabelMultiplier;
            transform.localScale = new Vector3(cameraDiff, cameraDiff, cameraDiff);
            transform.LookAt(Camera.main.transform.position);
        }
    }
}
