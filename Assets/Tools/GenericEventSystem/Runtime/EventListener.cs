using UnityEngine;
using UnityEngine.Events;

namespace GenericEventSystem
{

    public class EventListener : MonoBehaviour
    {
        [SerializeField] private EventDefinition channel;

        [SerializeField] private UnityEvent<EventData.EventData> response;

        protected void Awake()
        {
            if (channel == null)
            {
                Debug.LogWarning($"EventListener (" + gameObject.name + ") - Event channel was not configured");
                return;
            }

            channel.Subscribe(this);
        }

        protected void OnDestroy()
        {
            if (channel == null)
            {
                Debug.LogWarning($"EventListener (" + gameObject.name + ") - Event channel was not configured");
                return;
            }

            channel.Unsubscribe(this);
        }

        public void OnEventRaise(EventData.EventData data)
        {
            response.Invoke(data);
        }
    }
}