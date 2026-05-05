#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace GenericEventSystem.Editor
{
    public static class TypeProvider
    {
        public static List<Type> GetAllRuntimeSafeTypes()
        {
            // Get all loaded types in runtime assemblies
            var runtimeTypes = AppDomain.CurrentDomain.GetAssemblies()
                // Exclude UnityEditor assemblies and Editor-only assemblies
                .Where(a => !a.FullName.StartsWith("UnityEditor") && !a.FullName.Contains("-Editor"))
                .SelectMany(SafeGetTypes)
                .Where(IsRuntimeSafeType)
                .ToList();

            // Include primitives manually
            runtimeTypes.AddRange(new Type[]
            {
            typeof(bool), typeof(byte), typeof(char), typeof(short),
            typeof(ushort), typeof(int), typeof(uint), typeof(long),
            typeof(ulong), typeof(float), typeof(double), typeof(decimal),
            typeof(string)
            });

            // Include common generic definitions
            runtimeTypes.AddRange(new Type[]
            {
            typeof(List<>),
            typeof(Dictionary<,>),
            typeof(HashSet<>),
            typeof(Stack<>),
            typeof(Queue<>)
            });

            return runtimeTypes
                .Distinct()
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToList();
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private static bool IsRuntimeSafeType(Type t)
        {
            if (t == null) return false;

            // Skip compiler-generated types
            if (t.Name.StartsWith("<") || t.Name.Contains("PrivateImplementationDetails")) return false;

            if (!t.IsPublic) return false;

            // Exclude Editor namespaces
            if (!string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("UnityEditor")) return false;

            // UnityEngine.Object derived types are fine
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return true;

            // Enums, primitives, and structs
            if (t.IsEnum || t.IsPrimitive || t == typeof(string) || t.IsValueType) return true;

            // Allow all user runtime assemblies (exclude Unity + Editor)
            string asmName = t.Assembly.GetName().Name;

            if (!asmName.StartsWith("Unity") &&
                !asmName.StartsWith("System") &&
                !asmName.StartsWith("mscorlib") &&
                !asmName.Contains("Editor"))
            {
                return true;
            }

            return false;
        }
    }
}
#endif