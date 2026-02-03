using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HVR.UGC
{
    public static class HVRAssetBundleSanitization
    {
        private static readonly HashSet<Type> AllowedComponentTypes = new()
        {
            typeof(Transform),
            // UGC
            typeof(HVRUGCAvatar),
            typeof(HVRUGCBelonging),
            // Renderers
            typeof(SkinnedMeshRenderer),
            typeof(MeshRenderer),
            typeof(MeshFilter),
            typeof(TrailRenderer),
            typeof(LineRenderer),
            // Particle System
            typeof(ParticleSystem),
            typeof(ParticleSystemRenderer),
            // Light
            typeof(Light),
            // Audio
            typeof(AudioSource),
            // Constraints
            typeof(PositionConstraint),
            typeof(RotationConstraint),
            typeof(ParentConstraint),
            typeof(ScaleConstraint),
            typeof(AimConstraint),
            typeof(LookAtConstraint),
            // Cloth
            typeof(Cloth),
            // Colliders
            typeof(SphereCollider),
            typeof(CapsuleCollider),
            typeof(BoxCollider),
            typeof(MeshCollider),
            // Physics
            typeof(Rigidbody),
            typeof(CharacterJoint),
            typeof(ConfigurableJoint),
            typeof(FixedJoint),
            typeof(HingeJoint),
            typeof(SpringJoint),
            // UI (Eventless)
            typeof(RectTransform),
            typeof(Canvas),
            // Text
            typeof(Text),
            // Does not include Animator, as this is treated separately.
        };

        private static readonly HashSet<string> __AllowedNamedComponentTypes = new()
        {
            // Light
            HVRUGCHardcodedTypeNames.UniversalAdditionalLightData,
            // Text
            HVRUGCHardcodedTypeNames.TextMeshPro,
            HVRUGCHardcodedTypeNames.TextMeshProUGUI,
            // Custom
            HVRUGCHardcodedTypeNames.AutomaticFaceTracking,
            HVRUGCHardcodedTypeNames.JiggleRig
        };
        private static Type[] _allowedNamedComponentTypes;
        private static Type[] AllowedNamedComponentTypes
        {
            get
            {
                _allowedNamedComponentTypes ??= NamedToTypes();
                return _allowedNamedComponentTypes;
            }
        }

        public static Type[] GetAllowedComponentTypesIncludingAnimator()
        {
            return AllowedComponentTypes
                .Concat(AllowedNamedComponentTypes)
                .Concat(new[] { typeof(Animator) })
                .ToArray();
        }

        private static Type[] NamedToTypes()
        {
            return __AllowedNamedComponentTypes.Select(s =>
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType(s);
                    if (type != null)
                    {
                        return type;
                    }
                }

                return null;
            })
                .Where(type => type != null)
                .ToArray();
        }

        /// It is NOT SAFE to run this on non-AssetBundles, as this can modify project prefabs or delete assets from the project.
        public static GameObject PreInstantiateSanitize(GameObject prefab)
        {
            if (prefab == null) return null;

            // Remove components, as they may have exploits in them.
            RemoveIllegalComponents(prefab, true);
            
            // Some components may contain references to GameObjects and Components that do not exist in the hierarchy of the prefab.
            // We need to strip them, too, as these references could be instantiated later.
            {
                // FIXME: Too complicated to remove deep references for now (it destroys prefabs). Try to improve later.
                // For now, we're dereferencing them instead.
                // RemoveDeepReferences(prefab);
                
                // FIXME: SerializedObject is not available at runtime. ????!!!!????!!!!
#if UNITY_EDITOR
                DereferenceExternalObjects(prefab);
#endif
            }
            
            ReconfigureAnimators(prefab);

            return prefab;
        }

        /// It is NOT SAFE to run this on non-AssetBundles, as this can modify project prefabs or delete assets from the project.
        public static void PostInstantiateSanitizeSceneInstance(GameObject sceneInstance)
        {
            RemoveComponentsWithMissingScriptRecursive(sceneInstance);
        }

        public static void RemoveComponentsWithMissingScriptRecursive(GameObject sceneInstance)
        {
            var allTransforms = sceneInstance.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
#if UNITY_EDITOR
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
#endif
            }
        }

        private static void RemoveIllegalComponents(GameObject prefab, bool allowAnimator)
        {
            var insist = new List<Component>();
            
            var allTransforms = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                var components = t.GetComponents<Component>();
#if UNITY_EDITOR
                // Looks like this isn't working unless it's instantiated in the scene
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
#endif
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }
                    
                    if (AllowedComponentTypes.Contains(component.GetType()))
                    {
                        continue;
                    }
                    
                    if (AllowedNamedComponentTypes.Contains(component.GetType()))
                    {
                        continue;
                    }

                    if (allowAnimator)
                    {
                        // NOTE: Animators are rebuilt in the next step. We need to extract their avatar object.
                        if (component is Animator)
                        {
                            continue;
                        }
                    }

                    HVRUGCLogging.Log(typeof(HVRAssetBundleSanitization), $"Removing unauthorized component {component} from object...");
                    // FIXME: This operation can fail, if the component is required by another component on the same object.
                    Object.DestroyImmediate(component, true);

                    if (null != component)
                    {
                        insist.Add(component);
                    }
                }
            }

            var insistIterations = 0;
            while (insist.Count > 0)
            {
                HVRUGCLogging.Log(typeof(HVRAssetBundleSanitization), $"Failed to remove {insist.Count} components, will try again.");
                var index = 0;
                while (index < insist.Count)
                {
                    var component = insist[index];
                    Object.DestroyImmediate(component, true);
                    if (null == component)
                    {
                        insist.RemoveAt(index);
                    }
                    else
                    {
                        index++;
                    }
                }

                insistIterations++;
                if (insistIterations > 100)
                {
                    throw new InvalidOperationException($"Failed to remove some components after {insistIterations} attempts.");
                }
            }
        }

        private static void ReconfigureAnimators(GameObject prefab)
        {
            var allAnimators = prefab.GetComponentsInChildren<Animator>(true);
            foreach (var animator in allAnimators)
            {
                animator.fireEvents = false;
            }

            var rootAnimator = prefab.GetComponent<Animator>();
            if (rootAnimator != null)
            {
                rootAnimator.runtimeAnimatorController = null;
            }
        }

        private static void RebuildAnimators(GameObject prefab)
        {
            var allAnimators = prefab.GetComponentsInChildren<Animator>(true);
            foreach (var oldAnimator in allAnimators)
            {
                var avatar = oldAnimator.avatar;
                var animatorGameObject = oldAnimator.gameObject;
                
                Object.DestroyImmediate(oldAnimator, true);
                
                var newAnimator = animatorGameObject.AddComponent<Animator>();
                newAnimator.avatar = avatar;
                newAnimator.fireEvents = false;
            }
        }

#if UNITY_EDITOR
        // This is really annoying. Asset bundles and prefabs can contain references to other GameObjects and Components
        // that are not actually in the hierarchy, and those objects could contain components that we need to get rid of,
        // because there's a possibility that a some component would instantiate them.
        private static void RemoveDeepReferences(GameObject prefab)
        {
            var objects = CollectDeepReferences(prefab);
            
            var unprocessedReferencedGameObjects = new HashSet<GameObject>();
            foreach (var obj in objects)
            {
                if (obj is GameObject go)
                {
                    unprocessedReferencedGameObjects.Add(go);
                }
                else if (obj is Component component)
                {
                    unprocessedReferencedGameObjects.Add(component.gameObject);
                }
            }
            
            // We've already processed them.
            var allTransforms = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                unprocessedReferencedGameObjects.Remove(t.gameObject);
            }
            
            foreach (var unprocessed in unprocessedReferencedGameObjects)
            {
                RemoveIllegalComponents(unprocessed, false);
            }
        }

        private static HashSet<Object> CollectDeepReferences(GameObject prefab)
        {
            var collectedObjects = new HashSet<Object>();
            
            var allTransforms = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                var components = t.GetComponents<Component>();

                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    using var so = new SerializedObject(component);
                    var property = so.GetIterator();
                    
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            var value = property.objectReferenceValue;
                            if (value != null)
                            {
                                HVRUGCLogging.Log(typeof(HVRAssetBundleSanitization), $"Deep reference found Component: {component.GetType().Name} on {component.name} " +
                                          $"-> Property: {property.propertyPath} " +
                                          $"-> References: {value.name} ({value.GetType().Name})");
                                collectedObjects.Add(value);
                            }
                        }
                    }
                }
            }

            return collectedObjects;
        }

        private static void DereferenceExternalObjects(GameObject prefab)
        {
            var allowedGameObjects = new HashSet<GameObject>();
            
            var transforms = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                allowedGameObjects.Add(t.gameObject);
            }
            
            var allTransforms = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                var components = t.GetComponents<Component>();

                foreach (var component in components)
                {
                    if (component == null) continue;
                    
                    using var so = new SerializedObject(component);
                    var property = so.GetIterator();
                    
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        
                        var value = property.objectReferenceValue;
                        if (value == null) continue;

                        // We don't care about Material references, etc. and we don't care about GameObjects that are part of our hierarchy.
                        var isReferenceGameObjectOrComponent = value is GameObject go && !allowedGameObjects.Contains(go)
                                || value is Component c && !allowedGameObjects.Contains(c.gameObject);
                        if (!isReferenceGameObjectOrComponent) continue;
                        
                        HVRUGCLogging.Log(typeof(HVRAssetBundleSanitization), $"Deep reference found Component: {component.GetType().Name} on {component.name} " +
                                                                           $"-> Property: {property.propertyPath} " +
                                                                           $"-> References: {value.name} ({value.GetType().Name})");
                        if (value is GameObject || value is Transform)
                        {
                            // We want to keep World Fixed Object tricks, where constraints can contain a reference to a prefabbed GameObject.
                            var ourGameObject = value is GameObject tempGo ? tempGo
                                : value is Transform tempTransform ? tempTransform.gameObject
                                : throw new InvalidOperationException("Invalid case encountered");
                            var allSubComponents = ourGameObject.GetComponentsInChildren<Component>(true);
                            var containsOnlyTransforms = allSubComponents.All(c => c is Transform);
                            if (containsOnlyTransforms)
                            {
                                HVRUGCLogging.Log(typeof(HVRAssetBundleSanitization), $"We're not dereferencing {ourGameObject.name} because it doesn't have any non-Transform component in its hierarchy, so it's safe to assume this is just a World Fixed Object trick.");
                            }
                            // This contains at least once Component.
                            else
                            {
                                HVRUGCLogging.Log(typeof(HVRAssetBundleSanitization), $"Dereferencing {ourGameObject.name} because it contains at least one Component in its hierarchy.");
                                property.objectReferenceValue = null;
                            }
                        }
                        // This is a Component that isn't a Transform.
                        else
                        {
                            HVRUGCLogging.Log(typeof(HVRAssetBundleSanitization), $"Dereferencing {value.name} because it's a non-Transform Component of type {value.GetType().Name}.");
                            property.objectReferenceValue = null;
                        }
                    }

                    if (so.hasModifiedProperties)
                    {
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }
#endif
    }
}