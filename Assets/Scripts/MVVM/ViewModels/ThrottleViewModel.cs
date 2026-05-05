using Assets.Scripts.ECS.Components.Tags;
using GenericEventSystem;
using GenericEventSystem.EventData;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.MVVM.ViewModels
{
    [RequireComponent(typeof(Slider))]
    public class ThrottleViewModel : ViewModelBase
    {
        public EventDefinition ThrottleChangedChannel;

        [Header("Velocity Indicator")]
        [SerializeField] private RectTransform _velocityDot;

        public static float Throttle { get; private set; }
        private Slider _slider;
        private RectTransform _fillArea;
        private float _sliderWidth;
        private EntityQuery _velocityQuery;

        private void Awake()
        {
#if UNITY_ANDROID || UNITY_IOS
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
#else
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
#endif
        }

        private void Start()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 100f;
            _slider.value = 0f;

            if (_slider.fillRect != null)
            {
                _fillArea = _slider.fillRect.parent.GetComponent<RectTransform>();
                _sliderWidth = _fillArea != null ? _fillArea.rect.width : 0f;
            }

            _slider.onValueChanged.AddListener(OnSliderChanged);

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

           _velocityQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<VelocityComponent>(),
                ComponentType.ReadOnly<PlayerShipTag>(),
                ComponentType.ReadOnly<GhostOwnerIsLocal>());
        }

        private void OnDestroy()
        {
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
            _velocityQuery.Dispose();
        }

        private void Update()
        {
            if (_velocityDot == null) return;

            float velocityNormalized = GetCurrentVelocityNormalized();

            float xPos = Mathf.Lerp(0f, _sliderWidth, velocityNormalized);
            _velocityDot.anchoredPosition = new Vector2(xPos, 0f);
        }

        private void OnSliderChanged(float value)
        {
            Throttle = value / _slider.maxValue;
            ThrottleChangedChannel.Raise(new ThrottleChangedEventData { newValue = (int)value });
        }

        private float GetCurrentVelocityNormalized()
        {
            if (_velocityQuery.IsEmpty) return 0f;
            var v = _velocityQuery.GetSingleton<VelocityComponent>();
            return v.MaxVelocity > 0f ? Mathf.Clamp01(v.Velocity / v.MaxVelocity) : 0f;
        }
    }
}