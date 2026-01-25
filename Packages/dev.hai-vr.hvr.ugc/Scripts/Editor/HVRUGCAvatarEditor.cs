using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HVR.UGC.Editor
{
    [CustomEditor(typeof(HVRUGCAvatar))]
    public class HVRUGCAvatarEditor : UnityEditor.Editor
    {
        private const string BuildAvatarFile = "Build avatar file";
        private const string DeleteLabel = "Delete";
        private const string DisallowedComponentsLabel = "Disallowed components";
        private const string EyeHeightLabel = "Eye height";
        private const string MissingScriptsLabel = "Missing scripts";
        private const string MoveViewpointLabel = "Move Viewpoint";
        private const string NonDestructiveComponentsLabel = "Non-destructive components";
        private const string UnreadableMeshesLabel = "Unreadable meshes";
        
        private const string MsgBypassRestrictions = "BYPASS RESTRICTIONS is enabled on the avatar. Illegal components will be included in the asset bundle. This should only be used for development purposes (e.g., testing sanitization).";
        private const string MsgCollisionMeshNotReadable = "Some collision meshes do not have Read/Write enabled. Enable Read/Write on the mesh settings so that we can calculate a collision mesh for it.";
        private const string MsgComponentNotNextToAnimator = "This component must be placed on the same GameObject as the Animator component of the avatar.";
        private const string MsgComponentsWithMissingScripts = "Game objects that have missing scripts in them are shown here. This could be components from other SDKs which would be irrelevant here.";
        private const string MsgDisallowedComponents = "Only components that exist before the avatar build are shown here. If you use non-destructive tooling, some of this may be irrelevant.\nDisallowed components are removed after non-destructive tooling execute.";
        private const string MsgNoAvatarName = "Please set the avatar name before building.";
        private const string MsgNoCollisionMeshes = "You have not defined any collision meshes for this avatar. You may not be able to have proper physics-based interactions without it.";
        private const string ViewpointLabel = "Viewpoint";
        private const string CreateViewpointLabel = "Create Viewpoint";
        private const string PhysicsLabel = "Physics";
        private const string EditViewpointLabel = "Edit Viewpoint";
        private const string NonInterpolatedRigidbodiesLabel = "Non-interpolated Rigidbodies";
        private const string MsgRigidbodyNotInterpolated = "Some of the rigidbodies have their Interpolation settings set to 'None' or 'Extrapolate'. In HVR, this setting should be set to 'Interpolate' (this recommendation does not apply to all games).";
        private const string SetToInterpolateLabel = "Set to Interpolate";
        private const string SetRigidbodyInterpolationSettingLabel = "Set Rigidbody Interpolation Setting";

        private List<Component> _disallowedComponents;
        private List<Component> _disallowedComponentsNonDestructive;
        private List<GameObject> _gameObjectsThatHaveMissingComponentsInThem;
        private List<Rigidbody> _rigidbodiesThatAreNotSetToInterpolate;        

        public bool editViewpoint;
        private List<Mesh> _unreadableMeshes;

        private void Awake()
        {
            _disallowedComponents = new List<Component>();
            _disallowedComponentsNonDestructive = new List<Component>();
            _gameObjectsThatHaveMissingComponentsInThem = new List<GameObject>();
            _unreadableMeshes = new List<Mesh>();
            _rigidbodiesThatAreNotSetToInterpolate = new List<Rigidbody>();

            FindAllDisallowedComponents();
            FindGameObjectsThatHaveMissingComponentsInThem();
            FindCollisionMeshesThatAreUnreadable();
            FindRigidbodiesThatAreNotSetToInterpolate();
            
            _disallowedComponents.Sort(SortByType);
            _disallowedComponentsNonDestructive.Sort(SortByType);
        }

        private int SortByType(Component a, Component b)
        {
            return string.Compare(a.GetType().Name, b.GetType().Name, StringComparison.Ordinal);
        }

        public override void OnInspectorGUI()
        {
            var avatar = (HVRUGCAvatar)target;
            var hasAnimator = avatar.GetComponent<Animator>() != null;
            
            if (!Application.isPlaying && avatar.GetComponent<HVRUGCMarkAutoprocess>() == null)
            {
                Undo.AddComponent<HVRUGCMarkAutoprocess>(avatar.gameObject);
            }

            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.avatarName)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.avatarBase)));
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField(ViewpointLabel, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.viewpoint)));
                if (HVREditorUtil.ColoredBackground(editViewpoint, HVRUGCUtil.buttonBeingEdited, () => GUILayout.Button(EditViewpointLabel)))
                {
                    if (avatar.viewpoint == null)
                    {
                        var defaultPos = avatar.transform.position + Vector3.up * 1.1f;
                        var worldPos = hasAnimator
                            ? ((avatar.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.LeftEye)?.position??defaultPos) + Vector3.forward * 0.05f)
                            : defaultPos;
                        worldPos.x = avatar.transform.position.x;
                        
                        var newViewpoint = new GameObject("HVRViewpoint")
                        {
                            transform =
                            {
                                parent = avatar.transform,
                                position = worldPos,
                            }
                        };
                        Undo.RegisterCreatedObjectUndo(newViewpoint, CreateViewpointLabel);
                        serializedObject.FindProperty(nameof(HVRUGCAvatar.viewpoint)).objectReferenceValue = newViewpoint;
                    }
                    editViewpoint = !editViewpoint;
                    SceneView.RepaintAll();
                }
                if (hasAnimator)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.FloatField(new GUIContent(EyeHeightLabel), avatar.EyeHeight());
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField(PhysicsLabel, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.collisionMeshes)));
                EditorGUILayout.Separator();
                
#if !HVR_IS_INSTALLED_IN_A_VRC_PROJECT
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.bypassRestrictions)));
#endif
            }

            if (!hasAnimator)
            {
                EditorGUILayout.HelpBox(MsgComponentNotNextToAnimator, MessageType.Error);
            }

            var canBuild = hasAnimator && !string.IsNullOrEmpty(avatar.avatarName) && !Application.isPlaying;
            EditorGUI.BeginDisabledGroup(!canBuild);
            if (GUILayout.Button(BuildAvatarFile, GUILayout.Height(HVREditorUtil.TallButtonHeight)))
            {
                HVRUGCLogging.Log(this, $"User has requested to build asset bundle for avatar {avatar.avatarName} (called {avatar.name} in the scene).");
                
                var assetBundleName = HVRUGCUtil.SanitizeAssetBundleFileName(avatar.avatarName);
                
                var bundlePath = HVRAssetBundleBuilder.PrepareAndBuildAssetBundle(avatar.gameObject, "UGC", assetBundleName,
#if !HVR_IS_INSTALLED_IN_A_VRC_PROJECT
                    avatar.bypassRestrictions
#else
                    // If HVR is in a VRC project, we always bypass all restrictions so that we may execute backports at runtime.
                    // It is going to get sanitized either way at runtime.
                    true
#endif
                );
                
                var directory = Path.GetDirectoryName(bundlePath);
                var newFileName = $"{assetBundleName}.bundle";
                var newPath = Path.Combine(directory, newFileName);
                if (File.Exists(newPath)) File.Delete(newPath);
                File.Move(bundlePath, newPath);
                
                HVRUGCLogging.Log(this, $"Successfully built asset bundle at {newPath}");
                EditorUtility.RevealInFinder(newPath);
            }
            EditorGUI.EndDisabledGroup();

            if (avatar.avatarName == null)
            {
                EditorGUILayout.HelpBox(MsgNoAvatarName, MessageType.Warning);
            }

            if (avatar.collisionMeshes.Length == 0)
            {
                EditorGUILayout.HelpBox(MsgNoCollisionMeshes, MessageType.Warning);
            }
            else if (_unreadableMeshes.Count > 0)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.LabelField(UnreadableMeshesLabel, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(MsgCollisionMeshNotReadable, MessageType.Error);
                foreach (var unreadableMesh in _unreadableMeshes)
                {
                    EditorGUILayout.ObjectField(unreadableMesh, typeof(Mesh), true);
                }
                EditorGUILayout.EndVertical();
            }

            if (_rigidbodiesThatAreNotSetToInterpolate.Count > 0)
            {
                EditorGUILayout.BeginVertical("GroupBox");
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
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.LabelField(DisallowedComponentsLabel, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(MsgDisallowedComponents, MessageType.Info);
                
#if !HVR_IS_INSTALLED_IN_A_VRC_PROJECT
                if (avatar.bypassRestrictions)
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
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.LabelField(MissingScriptsLabel, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(MsgComponentsWithMissingScripts, MessageType.Warning);
                if (GUILayout.Button(DeleteLabel))
                {
                    HVRAssetBundleSanitization.RemoveComponentsWithMissingScriptRecursive(avatar.gameObject);
                    _gameObjectsThatHaveMissingComponentsInThem.Clear();
                    FindGameObjectsThatHaveMissingComponentsInThem();
                }
                foreach (var gameObjectThatHasMissingComponentsInThem in _gameObjectsThatHaveMissingComponentsInThem)
                {
                    EditorGUILayout.ObjectField(gameObjectThatHasMissingComponentsInThem, typeof(GameObject), true);
                }
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void FindAllDisallowedComponents()
        {
            var allowed = HVRAssetBundleSanitization.GetAllowedComponentTypesIncludingAnimator();
            var avatar = (HVRUGCAvatar)target;
            
            var components = avatar.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null) continue;
                if (allowed.Contains(component.GetType())) continue;
                if (component is HVRUGCMarkAutoprocess) continue; // Special component. Will be removed on build.
                
                var isAnyEditorOnly = IsAnyParentEditorOnly(component.transform, avatar.transform);
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
            var avatar = (HVRUGCAvatar)target;
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
            var avatar = (HVRUGCAvatar)target;
            var cm = HVRUGCUtil.SlowSanitizeEndUserProvidedObjectArray(avatar.collisionMeshes);
            foreach (var smr in cm)
            {
                if (smr.sharedMesh is { } mesh)
                {
                    if (!mesh.isReadable)
                    {
                        _unreadableMeshes.Add(mesh);
                    }
                }
            }
        }

        private void FindRigidbodiesThatAreNotSetToInterpolate()
        {
            var avatar = (HVRUGCAvatar)target;
            var rigidbodies = avatar.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rigidbody in rigidbodies)
            {
                if (rigidbody.interpolation != RigidbodyInterpolation.Interpolate && !IsAnyParentEditorOnly(rigidbody.transform, avatar.transform))
                {
                    _rigidbodiesThatAreNotSetToInterpolate.Add(rigidbody);
                }
            }
        }

        private void OnSceneGUI()
        {
            var avatar = (HVRUGCAvatar)target;

            if (editViewpoint)
            {
                if (avatar.viewpoint == null) return;

                var viewpoint = avatar.viewpoint;
                EditorGUI.BeginChangeCheck();
                var newPosition = Handles.PositionHandle(viewpoint.position, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(viewpoint, MoveViewpointLabel);
                    viewpoint.position = newPosition;
                }
            }
        }

        private bool IsAnyParentEditorOnly(Transform t, Transform end)
        {
            if (t == null || t == end) return false;
            if (t.gameObject.CompareTag("EditorOnly")) return true;
            return t.transform.parent != null && IsAnyParentEditorOnly(t.transform.parent, end);
        }
    }
}