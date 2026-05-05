using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GenericEventSystem
{

    [CreateAssetMenu(fileName = "Event", menuName = "Generic Event System/Event Channel")]
    public class EventDefinition : ScriptableObject
    {
        private readonly List<EventListener> subs = new();

#if UNITY_EDITOR
        public Type ChannelDataType => payloadScript != null
            ? payloadScript.GetClass()
            : null;

        [SerializeField] private MonoScript payloadScript;

        [SerializeField, HideInInspector] public EventData.EventData EditorPayload;

        [SerializeField, HideInInspector]
        private bool isChannelSet;

        public void Editor_SetChannelTypeOnce(Type inChannelType)
        {
            if (isChannelSet)
            {
                Debug.LogWarning("Channel type already set");
                return;
            }

            if (inChannelType == null)
            {
                Debug.LogError("Null channel type");
                return;
            }


            payloadScript = FindScriptForType(inChannelType);
            isChannelSet = true;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"Event channel auto-bound to {inChannelType.Name}");
        }

        private static MonoScript FindScriptForType(Type type)
        {
            // Make sure AssetDatabase is up to date (important for codegen)
            AssetDatabase.Refresh();

            string[] guids = AssetDatabase.FindAssets("t:MonoScript");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script == null)
                    continue;

                Type scriptType = script.GetClass();

                if (scriptType == null)
                    continue;

                if (scriptType == type)
                    return script;
            }

            return null;
        }

#endif

        public void Subscribe(EventListener sub)
        {
            if (!subs.Contains(sub))
                subs.Add(sub);
        }

        public void Unsubscribe(EventListener sub)
        {
            subs.Remove(sub);
        }

        public void Raise(EventData.EventData data)
        {
#if DEBUG
            if (data == null)
            {
                Debug.LogError(
                    $"Event {name} raised with null data (expected {ChannelDataType?.Name})");
                return;
            }

            if (ChannelDataType == null)
            {
                Debug.LogError($"Event {name} has no channel type set");
                return;
            }

            if (data.GetType() != ChannelDataType)
            {
                Debug.LogError(
                    $"Event {name} expected {ChannelDataType.Name} but got {data.GetType().Name}");
                return;
            }
#endif

            for (int i = subs.Count - 1; i >= 0; i--)
            {
                subs[i].OnEventRaise(data);
            }
        }
    }
}