using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HVR.UGC.Editor
{
    [InitializeOnLoad]
    internal static class HVREditorUtil
    {
        // This is just so that when we search for `GUI.*"` to search for stray strings during development, we don't get GroupBox results
        public const string GroupBox = "GroupBox";
        
        public static readonly float TallButtonHeight = EditorGUIUtility.singleLineHeight * 2;
        
        public static HashSet<Type> AllTypes { get; }
        public static HashSet<Type> EditorOnlyTypes { get; }

        static HVREditorUtil()
        {
            AllTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .ToHashSet();

            EditorOnlyTypes = AllTypes
                // The following catches IEditorOnly, INDMFEditorOnly, and IPrefabulousEditorOnly
                // It is explicitly NOT the EditorOnly tag.
                .Where(type => type.IsInterface && type.FullName.EndsWith("EditorOnly"))
                .ToHashSet();
        }

        public static bool TryGetTypeByFullName(string typeName, out Type type)
        {
            type = AllTypes.FirstOrDefault(type => type.FullName == typeName);
            return type != null;
        }

        public static bool IsNonDestructiveComponent(Component component)
        {
            return EditorOnlyTypes.Any(type => type.IsAssignableFrom(component.GetType()));
        }
        
        public static T ColoredBackground<T>(bool isActive, Color bgColor, Func<T> inside)
        {
            var col = GUI.color;
            try
            {
                if (isActive) GUI.color = bgColor;
                return inside();
            }
            finally
            {
                GUI.color = col;
            }
        }

        public static bool IsAnyParentEditorOnly(Transform t, Transform end)
        {
            if (t == null || t == end) return false;
            if (t.gameObject.CompareTag("EditorOnly")) return true;
            return t.transform.parent != null && IsAnyParentEditorOnly(t.transform.parent, end);
        }
    }
}