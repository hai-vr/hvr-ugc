using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HVR.UGC.Editor
{
    [InitializeOnLoad]
    public static class HVREditorUtil
    {
        public static readonly float TallButtonHeight = EditorGUIUtility.singleLineHeight * 2;
        
        public static HashSet<Type> AllTypes { get; }
        public static HashSet<Type> EditorOnlyTypes { get; }

        static HVREditorUtil()
        {
            AllTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .ToHashSet();

            EditorOnlyTypes = AllTypes
                .Where(type => type.IsInterface && type.FullName.EndsWith("EditorOnly"))
                .ToHashSet();
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
    }
}