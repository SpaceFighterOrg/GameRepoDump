#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenericEventSystem.Editor
{
    [Serializable]
    public class FieldType
    {
        // Base (non-array, non-generic) type
        public Type BaseType;

        // Generic arguments (for List<T>, Dictionary<K,V>, etc.)
        public List<FieldType> GenericArgs = new List<FieldType>();

        public bool IsArray;

        public FieldType(Type t)
        {
            BaseType = t;
        }

        /* Display name (Editor UI) */
        public string GetDisplayName()
        {
            string core;

            if (BaseType.IsGenericType)
            {
                // Use generic definition name
                string baseName = BaseType.Name.Split('`')[0];

                string args = "";

                if (GenericArgs != null && GenericArgs.Count > 0)
                {
                    // Recursively get nested type names
                    args = string.Join(", ", GenericArgs.Select(a => a != null ? a.GetDisplayName() : "T"));
                }
                else if (BaseType.IsConstructedGenericType)
                {
                    // Constructed generic (from reflection) – use actual type args
                    args = string.Join(", ", BaseType.GetGenericArguments().Select(a => FromSystemType(a).GetDisplayName()));
                }

                core = $"{baseName}<{args}>";
            }
            else if (BaseType == typeof(string)) core = "String";
            else if (BaseType == typeof(int)) core = "Int";
            else if (BaseType == typeof(float)) core = "Float";
            else if (BaseType == typeof(double)) core = "Double";
            else if (BaseType == typeof(bool)) core = "Bool";
            else core = BaseType.Name;

            return IsArray ? core + "[]" : core;
        }

        /* =========================
         * Code name (CodeGen)
         * ========================= */
        public string GetCodeName(HashSet<string> importedNamespaces = null)
        {
            string core;

            if (BaseType.IsGenericTypeDefinition)
            {
                string args = string.Join(
                    ", ",
                    GenericArgs.Select(a => a != null
                        ? a.GetCodeName(importedNamespaces)
                        : "object")
                );

                string baseName = BaseType.Name.Split('`')[0];

                if (BaseType.Namespace != null &&
                    importedNamespaces != null &&
                    importedNamespaces.Contains(BaseType.Namespace))
                {
                    core = $"{baseName}<{args}>";
                }
                else
                {
                    core = $"{Qualify(BaseType, importedNamespaces).Split('`')[0]}<{args}>";
                }
            }
            else if (BaseType == typeof(string)) core = "string";
            else if (BaseType == typeof(int)) core = "int";
            else if (BaseType == typeof(float)) core = "float";
            else if (BaseType == typeof(double)) core = "double";
            else if (BaseType == typeof(bool)) core = "bool";
            else core = Qualify(BaseType, importedNamespaces);

            return IsArray ? core + "[]" : core;
        }

        private static string Qualify(Type type, HashSet<string> importedNamespaces)
        {
            if (type == null)
                return "object";

            if (importedNamespaces != null &&
                !string.IsNullOrEmpty(type.Namespace) &&
                importedNamespaces.Contains(type.Namespace))
            {
                return type.Name;
            }

            return string.IsNullOrEmpty(type.Namespace)
                ? type.Name
                : type.Namespace + "." + type.Name;
        }

        public static FieldType FromSystemType(Type type)
        {
            if (type == null)
                return null;

            bool isArray = type.IsArray;
            if (isArray)
                type = type.GetElementType();

            FieldType ft;

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                // Constructed generic (e.g. CustomType<int>)
                Type genericDef = type.GetGenericTypeDefinition();

                ft = new FieldType(genericDef);

                Type[] args = type.GetGenericArguments();
                foreach (var arg in args)
                {
                    ft.GenericArgs.Add(FromSystemType(arg));
                }
            }
            else
            {
                // Non-generic or already generic definition
                ft = new FieldType(type);
            }

            ft.IsArray = isArray;

            return ft;
        }
    }

}
#endif