using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HVR.UGC.Editor
{
    internal class HVRChecker
    {
        private const string DeleteLabel = "Delete";
        private const string DisallowedComponentsLabel = "Disallowed components";
        private const string MissingScriptsLabel = "Missing scripts";
        private const string NonDestructiveComponentsLabel = "Non-destructive components";
        private const string NonInterpolatedRigidbodiesLabel = "Non-interpolated Rigidbodies";
        private const string SetRigidbodyInterpolationSettingLabel = "Set Rigidbody Interpolation Setting";
        private const string SetToInterpolateLabel = "Set to Interpolate";
        
        private const string MsgBypassRestrictions = "BYPASS RESTRICTIONS is enabled on the avatar. Illegal components will be included in the asset bundle. This should only be used for development purposes (e.g., testing sanitization).";
        private const string MsgComponentsWithMissingScripts = "Game objects that have missing scripts in them are shown here. This could be components from other SDKs which would be irrelevant here.";
        private const string MsgDisallowedComponents = "Only components that exist before the avatar build are shown here. If you use non-destructive tooling, some of this may be irrelevant.\nDisallowed components are removed after non-destructive tooling execute.";
        private const string MsgRigidbodyNotInterpolated = "Some of the rigidbodies have their Interpolation settings set to 'None' or 'Extrapolate'. In HVR, this setting should be set to 'Interpolate' (this recommendation does not apply to all games).";
        
        private readonly MonoBehaviour _target;

        private readonly List<Component> _disallowedComponents;
        private readonly List<Component> _disallowedComponentsNonDestructive;
        private readonly List<GameObject> _gameObjectsThatHaveMissingComponentsInThem;
        private readonly List<Rigidbody> _rigidbodiesThatAreNotSetToInterpolate;
        internal readonly List<Mesh> UnreadableMeshes;

        public HVRChecker(MonoBehaviour target, HVRBundleType bundleType)
        {
            _target = target;
                
            _disallowedComponents = new List<Component>();
            _disallowedComponentsNonDestructive = new List<Component>();
            _gameObjectsThatHaveMissingComponentsInThem = new List<GameObject>();
            UnreadableMeshes = new List<Mesh>();
            _rigidbodiesThatAreNotSetToInterpolate = new List<Rigidbody>();

            FindAllDisallowedComponents();
            FindGameObjectsThatHaveMissingComponentsInThem();
            if (bundleType == HVRBundleType.Avatar) FindCollisionMeshesThatAreUnreadable();
            FindRigidbodiesThatAreNotSetToInterpolate();
            
            _disallowedComponents.Sort(SortByType);
            _disallowedComponentsNonDestructive.Sort(SortByType);
        }

        private int SortByType(Component a, Component b)
        {
            return string.Compare(a.GetType().Name, b.GetType().Name, StringComparison.Ordinal);
        }

        private void FindAllDisallowedComponents()
        {
            var allowed = HVRAssetBundleSanitization.GetAllowedComponentTypesIncludingAnimator();
            var avatar = _target;
                
            var components = avatar.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null) continue;
                if (allowed.Contains(component.GetType())) continue;
                if (component is HVRUGCMarkAutoprocess) continue; // Special component. Will be removed on build.
                    
                var isAnyEditorOnly = HVREditorUtil.IsAnyParentEditorOnly(component.transform, avatar.transform);
                if (!isAnyEditorOnly)
                {
                    if (HVREditorUtil.IsNonDestructiveComponent(component))
                    {
                        _disallowedComponentsNonDestructive.Add(component);
                    }
                    else
                    {
                        _disallowedComponents.Add(component);
                    }
                }
            }
        }

        private void FindGameObjectsThatHaveMissingComponentsInThem()
        {
            var avatar = _target;
            foreach (var t in avatar.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponents<Component>().Any(c => c == null))
                {
                    _gameObjectsThatHaveMissingComponentsInThem.Add(t.gameObject);
                }
            }
        }

        private void FindCollisionMeshesThatAreUnreadable()
        {
            var avatar = (HVRUGCAvatar)_target;
            var cm = HVRUGCUtil.SlowSanitizeEndUserProvidedObjectArray(avatar.collisionMeshes);
            foreach (var smr in cm)
            {
                if (smr.sharedMesh is { } mesh)
                {
                    if (!mesh.isReadable)
                    {
                        UnreadableMeshes.Add(mesh);
                    }
                }
            }
        }

        private void FindRigidbodiesThatAreNotSetToInterpolate()
        {
            var avatar = _target;
            var rigidbodies = avatar.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rigidbody in rigidbodies)
            {
                if (rigidbody.interpolation != RigidbodyInterpolation.Interpolate && !HVREditorUtil.IsAnyParentEditorOnly(rigidbody.transform, avatar.transform))
                {
                    _rigidbodiesThatAreNotSetToInterpolate.Add(rigidbody);
                }
            }
        }

        public void EditorLayout(bool bypassRestrictions)
        {
            if (_rigidbodiesThatAreNotSetToInterpolate.Count > 0)
            {
                EditorGUILayout.BeginVertical(HVREditorUtil.GroupBox);
                EditorGUILayout.LabelField(NonInterpolatedRigidbodiesLabel, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(MsgRigidbodyNotInterpolated, MessageType.Warning);
                if (GUILayout.Button(SetToInterpolateLabel))
                {
                    foreach (var rigidbody in _rigidbodiesThatAreNotSetToInterpolate)
                    {
                        Undo.RecordObject(rigidbody, SetRigidbodyInterpolationSettingLabel);
                        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    }
                    _rigidbodiesThatAreNotSetToInterpolate.Clear();
                    FindRigidbodiesThatAreNotSetToInterpolate();
                }
                foreach (var rigidbody in _rigidbodiesThatAreNotSetToInterpolate)
                {
                    EditorGUILayout.ObjectField(rigidbody, typeof(Rigidbody), true);
                }
                EditorGUILayout.EndVertical();
            }

            if (_disallowedComponents.Count > 0 || _disallowedComponentsNonDestructive.Count > 0)
            {
                EditorGUILayout.BeginVertical(HVREditorUtil.GroupBox);
                EditorGUILayout.LabelField(DisallowedComponentsLabel, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(MsgDisallowedComponents, MessageType.Info);
                    
#if !HVR_IS_INSTALLED_IN_A_VRC_PROJECT
                if (bypassRestrictions)
                {
                    EditorGUILayout.HelpBox(MsgBypassRestrictions, MessageType.Warning);
                }
#endif
                foreach (var disallowedComponent in _disallowedComponents)
                {
                    EditorGUILayout.ObjectField(disallowedComponent, typeof(Component), true);
                }

                if (_disallowedComponentsNonDestructive.Count > 0)
                {
                    EditorGUILayout.LabelField(NonDestructiveComponentsLabel, EditorStyles.boldLabel);
                    EditorGUI.BeginDisabledGroup(true);
                    foreach (var disallowedComponent in _disallowedComponentsNonDestructive)
                    {
                        EditorGUILayout.ObjectField(disallowedComponent, typeof(Component), true);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndVertical();
            }

            if (_gameObjectsThatHaveMissingComponentsInThem.Count > 0)
            {
                EditorGUILayout.BeginVertical(HVREditorUtil.GroupBox);
                EditorGUILayout.LabelField(MissingScriptsLabel, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(MsgComponentsWithMissingScripts, MessageType.Warning);
                if (GUILayout.Button(DeleteLabel))
                {
                    HVRAssetBundleSanitization.RemoveComponentsWithMissingScriptRecursive(_target.gameObject);
                    _gameObjectsThatHaveMissingComponentsInThem.Clear();
                    FindGameObjectsThatHaveMissingComponentsInThem();
                }
                foreach (var gameObjectThatHasMissingComponentsInThem in _gameObjectsThatHaveMissingComponentsInThem)
                {
                    EditorGUILayout.ObjectField(gameObjectThatHasMissingComponentsInThem, typeof(GameObject), true);
                }
                EditorGUILayout.EndVertical();
            }
        }
    }
}