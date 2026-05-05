#if UNITY_EDITOR
using System;

namespace GenericEventSystem.Editor
{
    [Serializable]
    public class FieldDefinition
    {
        public string Name = "value";
        public FieldType Type = null;
    }
}
#endif