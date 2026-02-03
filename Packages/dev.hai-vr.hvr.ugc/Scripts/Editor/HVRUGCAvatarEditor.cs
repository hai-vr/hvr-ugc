using System.IO;
using UnityEditor;
using UnityEngine;

namespace HVR.UGC.Editor
{
    [CustomEditor(typeof(HVRUGCAvatar))]
    public class HVRUGCAvatarEditor : UnityEditor.Editor
    {
        private const string BuildAvatarFile = "Build avatar file";
        private const string EyeHeightLabel = "Eye height";
        private const string MoveViewpointLabel = "Move Viewpoint";
        private const string UnreadableMeshesLabel = "Unreadable meshes";

        private const string MsgCollisionMeshNotReadable = "Some collision meshes do not have Read/Write enabled. Enable Read/Write on the mesh settings so that we can calculate a collision mesh for it.";
        private const string MsgComponentNotNextToAnimator = "This component must be placed on the same GameObject as the Animator component of the avatar.";
        private const string MsgNoAvatarName = "Please set the avatar name before building.";
        private const string MsgNoCollisionMeshes = "You have not defined any collision meshes for this avatar. You may not be able to have proper physics-based interactions without it.";
        private const string ViewpointLabel = "Viewpoint";
        private const string CreateViewpointLabel = "Create Viewpoint";
        private const string PhysicsLabel = "Physics";
        private const string EditViewpointLabel = "Edit Viewpoint";
        private const string MsgNoViewpoint = "Please set a viewpoint before building.";

        public bool editViewpoint;
        
        private HVRChecker _checker;

        private void Awake()
        {
            _checker = new HVRChecker((MonoBehaviour)target, HVRBundleType.Avatar);
        }

        public override void OnInspectorGUI()
        {
            var my = (HVRUGCAvatar)target;
            var hasAnimator = my.GetComponent<Animator>() != null;
            
            // We don't add this in VRC projects because we don't want to prevent NDMF's Apply On Play from working.
            // (This is also disabled in the code for HVRUGCMarkAutoprocess itself)
#if !HVR_IS_INSTALLED_IN_A_VRC_PROJECT
            if (!Application.isPlaying && my.GetComponent<HVRUGCMarkAutoprocess>() == null)
            {
                Undo.AddComponent<HVRUGCMarkAutoprocess>(my.gameObject);
            }
#endif

            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.avatarName)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.avatarBase)));
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField(ViewpointLabel, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.viewpoint)));
                if (HVREditorUtil.ColoredBackground(editViewpoint, HVRUGCUtil.buttonBeingEdited, () => GUILayout.Button(EditViewpointLabel)))
                {
                    if (my.viewpoint == null)
                    {
                        var newViewpoint = HVRUGCAvatar.GenerateViewpoint(my, hasAnimator);
                        Undo.RegisterCreatedObjectUndo(newViewpoint, CreateViewpointLabel);
                        serializedObject.FindProperty(nameof(HVRUGCAvatar.viewpoint)).objectReferenceValue = newViewpoint;
                    }
                    editViewpoint = !editViewpoint;
                    SceneView.RepaintAll();
                }
                if (hasAnimator)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.FloatField(new GUIContent(EyeHeightLabel), my.EyeHeight());
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.Separator();
                
                EditorGUILayout.LabelField(PhysicsLabel, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.collisionMeshes)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.collisionMeshBlendshapes)));
                EditorGUILayout.Separator();
                
#if !HVR_IS_INSTALLED_IN_A_VRC_PROJECT
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCAvatar.bypassRestrictions)));
#endif
            }

            if (!hasAnimator)
            {
                EditorGUILayout.HelpBox(MsgComponentNotNextToAnimator, MessageType.Error);
            }

            var canBuild = hasAnimator && !string.IsNullOrEmpty(my.avatarName) && !Application.isPlaying;
            EditorGUI.BeginDisabledGroup(!canBuild);
            if (GUILayout.Button(BuildAvatarFile, GUILayout.Height(HVREditorUtil.TallButtonHeight)))
            {
                HVRUGCLogging.Log(this, $"User has requested to build asset bundle for avatar {my.avatarName} (called {my.name} in the scene).");
                
                var assetBundleName = HVRUGCUtil.SanitizeAssetBundleFileName(my.avatarName);
                
                var bundlePath = HVRAssetBundleBuilder.PrepareAndBuildAssetBundle(my.gameObject, "UGC", assetBundleName, HVRBundleType.Avatar,
#if !HVR_IS_INSTALLED_IN_A_VRC_PROJECT
                    my.bypassRestrictions
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

            if (string.IsNullOrEmpty(my.avatarName))
            {
                EditorGUILayout.HelpBox(MsgNoAvatarName, MessageType.Warning);
            }
            else if (my.viewpoint == null)
            {
                EditorGUILayout.HelpBox(MsgNoViewpoint, MessageType.Warning);
            }
            
            if (my.collisionMeshes.Length == 0)
            {
                EditorGUILayout.HelpBox(MsgNoCollisionMeshes, MessageType.Warning);
            }
            else if (_checker.UnreadableMeshes.Count > 0)
            {
                EditorGUILayout.BeginVertical(HVREditorUtil.GroupBox);
                EditorGUILayout.LabelField(UnreadableMeshesLabel, EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(MsgCollisionMeshNotReadable, MessageType.Error);
                foreach (var unreadableMesh in _checker.UnreadableMeshes)
                {
                    EditorGUILayout.ObjectField(unreadableMesh, typeof(Mesh), true);
                }
                EditorGUILayout.EndVertical();
            }

            _checker.EditorLayout(my.bypassRestrictions);

            serializedObject.ApplyModifiedProperties();
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
    }
}