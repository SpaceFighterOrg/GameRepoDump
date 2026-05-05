#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GenericEventSystem.Editor
{
    [CustomEditor(typeof(EventDefinition))]
    public class EventDefinitionInspectorEditor : UnityEditor.Editor
    {
        SerializedProperty payloadScriptProp;

        // Drag in your EventData icon here, or load it dynamically
        private Texture2D eventDataIcon;

        private void OnEnable()
        {
            payloadScriptProp = serializedObject.FindProperty("payloadScript");
            eventDataIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Tools/GenericEventSystem/Editor/event_data_icon.png"
            );
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Event Channel Settings", EditorStyles.boldLabel);

            DrawPayloadScriptField();

            EditorGUILayout.Space();

            // Draw all other serialized fields except runtime-only ones
            DrawPropertiesExcluding(serializedObject,
                "EditorPayload",
                "isChannelSet",
                "m_Script",
                "payloadScript"
            );

            serializedObject.ApplyModifiedProperties();

            DrawRuntimePayloadSection();
        }

        private void DrawPayloadScriptField()
        {
            EditorGUILayout.BeginHorizontal();

            MonoScript currentScript = payloadScriptProp.objectReferenceValue as MonoScript;

            GUI.enabled = false;
            EditorGUILayout.ObjectField(currentScript, typeof(MonoScript), false);
            GUI.enabled = true;

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                EventDataScriptPicker.Open(
                    pickedScript =>
                    {
                        if (pickedScript != null && typeof(EventData.EventData).IsAssignableFrom(pickedScript.GetClass()))
                        {
                            payloadScriptProp.objectReferenceValue = pickedScript;
                            serializedObject.ApplyModifiedProperties();

                            // Update EditorPayload if needed
                            var ed = target as EventDefinition;
                            Type newType = pickedScript.GetClass();
                            if (ed != null && (ed.EditorPayload == null || ed.EditorPayload.GetType() != newType))
                                ed.EditorPayload = (EventData.EventData)Activator.CreateInstance(newType);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(
                                "Invalid Script",
                                "Selected script must inherit from EventData",
                                "OK"
                            );
                        }
                    },
                    eventDataIcon
                );
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRuntimePayloadSection()
        {
            var so = (EventDefinition)target;
            var type = so.ChannelDataType;

            if (type == null)
            {
                EditorGUILayout.HelpBox("Channel type not set.", MessageType.Info);
                return;
            }

            if (so.EditorPayload == null || so.EditorPayload.GetType() != type)
            {
                so.EditorPayload = (EventData.EventData)Activator.CreateInstance(type);
                EditorUtility.SetDirty(so);
            }

            var editorData = so.EditorPayload;

            var fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance
            );

            if (fields.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Event Payload", EditorStyles.boldLabel);
                editorData = (EventData.EventData)DrawObjectFields(type, editorData);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Trigger"))
            {
                so.Raise(editorData);
            }

            EditorUtility.SetDirty(so);
        }

        object DrawField(string name, Type type, object value)
        {
            if (type == typeof(int))
                return EditorGUILayout.IntField(name, value != null ? (int)value : 0);

            if (type == typeof(float))
                return EditorGUILayout.FloatField(name, value != null ? (float)value : 0f);

            if (type == typeof(string))
                return EditorGUILayout.TextField(name, value as string);

            if (type == typeof(bool))
                return EditorGUILayout.Toggle(name, value != null && (bool)value);

            if (type.IsEnum)
                return EditorGUILayout.EnumPopup(name, (Enum)value);

            if (type.IsValueType && !type.IsPrimitive)
            {
                if (value == null)
                    value = Activator.CreateInstance(type);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                value = DrawObjectFields(type, value);
                EditorGUILayout.EndVertical();
                return value;
            }

            if (typeof(System.Collections.IList).IsAssignableFrom(type))
                return DrawListField(name, type, value);

            EditorGUILayout.LabelField(name, $"Unsupported type ({type.Name})");
            return value;
        }

        object DrawListField(string name, Type listType, object value)
        {
            Type elementType = listType.IsArray
                ? listType.GetElementType()
                : listType.GetGenericArguments()[0];

            System.Collections.IList list;

            if (value == null)
            {
                list = listType.IsArray
                    ? Array.CreateInstance(elementType, 0)
                    : (System.Collections.IList)Activator.CreateInstance(listType);
            }
            else
            {
                list = (System.Collections.IList)value;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);

            int size = EditorGUILayout.IntField("Size", list.Count);
            if (size != list.Count)
                ResizeList(ref list, size, elementType, listType);

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Element {i}", GUILayout.Width(70));
                object element = list[i];
                element = DrawField("", elementType, element);
                list[i] = element;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            return list;
        }

        void ResizeList(ref System.Collections.IList list, int newSize, Type elementType, Type listType)
        {
            if (listType.IsArray)
            {
                Array newArray = Array.CreateInstance(elementType, newSize);
                for (int i = 0; i < Math.Min(list.Count, newSize); i++)
                    newArray.SetValue(list[i], i);
                list = newArray;
            }
            else
            {
                while (list.Count < newSize)
                    list.Add(Activator.CreateInstance(elementType));
                while (list.Count > newSize)
                    list.RemoveAt(list.Count - 1);
            }
        }

        object DrawObjectFields(Type type, object obj)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.IsNotSerialized)
                    continue;

                object fieldValue = field.GetValue(obj);
                fieldValue = DrawField(field.Name, field.FieldType, fieldValue);
                field.SetValue(obj, fieldValue);
            }

            return obj;
        }
    }
}
#endif