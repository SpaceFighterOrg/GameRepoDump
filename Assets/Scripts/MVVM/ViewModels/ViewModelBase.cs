using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scripts.MVVM.ViewModels
{
    public class ViewModelBase : MonoBehaviour
    {
        protected void AddPointerDownListener(Button button, System.Action action)
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>()
                          ?? button.gameObject.AddComponent<EventTrigger>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        protected void AddPointerUpListener(Button button, System.Action action)
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>()
                          ?? button.gameObject.AddComponent<EventTrigger>();

            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }
    }
}