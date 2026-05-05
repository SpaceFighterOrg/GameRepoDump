using Assets.Scripts.MVVM.Models.Projectiles;
using Assets.Scripts.Utils;
using GenericEventSystem;
using GenericEventSystem.EventData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace Assets.Scripts.MVVM.ViewModels
{
    public class WeaponButtonViewModel : ViewModelBase, IPointerDownHandler, IPointerUpHandler
    {
        private EventDefinition WeaponFiredChannel;
        private ProjectileType ProjectileType;

        [SerializeField] private Button Button;
        [SerializeField] private Image _icon;

        public bool IsPressedForThisTick { get; private set; }

        public void Initialize(ProjectileType projectileType, EventDefinition channel)
        {
            ProjectileType = projectileType;
            WeaponFiredChannel = channel;

            _icon.sprite = ProjectileType.Icon;

            var eventTrigger = Button.gameObject.AddComponent<EventTrigger>();

            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener(_ => OnPointerDown(null));
            eventTrigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener(_ => OnPointerUp(null));
            eventTrigger.triggers.Add(pointerUp);
        }

        private void Start()
        {
            if (ProjectileType == null || WeaponFiredChannel == null)
            {
                Debug.LogError($"[WeaponButtonViewModel] Not initialized before Start on {gameObject.name}");
            }
        }

        [Preserve]
        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressedForThisTick = true;
            WeaponFiredChannel.Raise(new WeaponFiredEventData() { projectileType = ProjectileType });
            SessionManager.LatestFiredProjectileSpeed = ProjectileType.Speed;
        }

        [Preserve]
        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressedForThisTick = false;
        }

    }
}