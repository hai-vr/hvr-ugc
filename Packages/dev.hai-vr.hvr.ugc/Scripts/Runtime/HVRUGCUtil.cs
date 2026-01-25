using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HVR.UGC
{
    public static class HVRUGCUtil
    {
        public static readonly Color buttonBeingEdited = new(0.49f, 1f, 0.83f);
        
        public static string SanitizeAssetBundleFileName(string fileNameWithIllegalChars)
        {
            if (string.IsNullOrEmpty(fileNameWithIllegalChars)) return "empty";
            
            char[] invalidChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
            
            var newName = fileNameWithIllegalChars.Trim();
            foreach (var c in invalidChars)
            {
                newName = newName.Replace(c.ToString(), "");
            }
            newName = newName.Replace(" ", "-");

            if (string.IsNullOrEmpty(newName)) return "empty";
            return newName;
        }

        /// Semantically used to sanitize a serializable field of objects provided by an End User.<br/>
        /// Given a nullable array of Unity Objects that may contain null-Destroy Objects,
        /// return a non-null array of Unity Objects that does not contain null-Destroy Objects.
        public static T[] SlowSanitizeEndUserProvidedObjectArray<T>(T[] objectsNullable) where T : Object
        {
            if (objectsNullable == null) return Array.Empty<T>();

            return objectsNullable.Where(t => t).ToArray();
        }

        public static TComp UGC_NewDisabledObjectWithComponent<TComp>(string name, Transform parentTransform) where TComp : Component
        {
            var go = new GameObject
            {
                name = name,
                transform =
                {
                    parent = parentTransform,
                    localPosition = Vector3.zero,
                    localRotation = Quaternion.identity,
                    localScale = Vector3.one
                }
            };
            go.SetActive(false);
            return go.AddComponent<TComp>();
        }
    }
}