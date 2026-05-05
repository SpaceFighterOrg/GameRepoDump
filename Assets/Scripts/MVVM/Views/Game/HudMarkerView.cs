using Assets.Scripts.Interfaces;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.MVVM.Views.Game
{
    public class HudMarkerView : MonoBehaviour, IHudMarker
    {
        [Header("Colored Children")]
        [SerializeField] private SpriteRenderer _radarOutline;
        [SerializeField] private SpriteRenderer _predictionOutline;
        [SerializeField] private TextMeshPro _distanceLabel;
        [SerializeField] private TextMeshPro _nameLabel;

        [Header("Team Colors")]
        [SerializeField] private Color _friendlyColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color _hostileColor = new Color(1f, 0.25f, 0.2f);

        private Camera _cam;
        [SerializeField] private float _zoomLabelMultiplier = 0.07f;

        public void SetTeamColor(bool isFriendly)
        {
            Color c = isFriendly ? _friendlyColor : _hostileColor;
            _radarOutline.color = c;
            _predictionOutline.color = c;
            _distanceLabel.color = c;
            _nameLabel.color = c;
        }

        public void SetPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        public void SetData(string playerName, float distance, Vector3 predictionWidgetPosition)
        {
            _nameLabel.text = playerName;
            _distanceLabel.text = $"{distance:F0} u";
            _predictionOutline.transform.localPosition = predictionWidgetPosition;
        }

        public Transform GetTransform()
        {
            return transform;
        }

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (_cam == null) return;

            float cameraDiff = Mathf.Abs(_cam.transform.position.z - transform.position.z) * _zoomLabelMultiplier;
            transform.localScale = Vector3.one * cameraDiff;
            transform.rotation = _cam.transform.rotation;
        }
    }
}