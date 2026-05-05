using UnityEngine;

namespace Assets.Scripts.MVVM
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineConnector : MonoBehaviour
    {
        [Header("Targets")]
        public Transform pointA;
        public Transform pointB;

        [Header("Line Settings")]
        public float startWidth = 0.05f;
        public float endWidth = 0.05f;
        public Material lineMaterial;

        private LineRenderer _lineRenderer;

        void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;

            _lineRenderer.startWidth = startWidth;
            _lineRenderer.endWidth = endWidth;

            if (lineMaterial != null)
                _lineRenderer.material = lineMaterial;
        }

        void LateUpdate()
        {
            if (pointA == null || pointB == null)
            {
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;
            _lineRenderer.SetPosition(0, pointA.position);
            _lineRenderer.SetPosition(1, pointB.position);
        }
    }

}
