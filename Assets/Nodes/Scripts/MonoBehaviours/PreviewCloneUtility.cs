using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN.Preview
{
    /// <summary>
    /// Clones a scene GameObject and disables all behavior except rendering.
    /// Intended for "preview" objects in a node/graph UI so they look right but don't run gameplay.
    /// </summary>
    public static class PreviewCloneUtility
    {
        [Serializable]
        public struct Options
        {
            public string nameSuffix;

            // Keep the visuals
            public bool keepRenderersEnabled;      // Usually true
            public bool keepMeshFiltersEnabled;    // Needed for MeshRenderer
            public bool keepSkinnedMeshBones;      // Avoid breaking skinned meshes

            // Disable common behavior
            public bool disableMonoBehaviours;     // Usually true
            public bool disableAnimators;          // Usually true (or false if you want animated preview)
            public bool disableParticles;          // Usually true
            public bool disableLights;             // Usually true (often you want just material preview)
            public bool disableAudio;              // Usually true
            public bool disableUI;                 // Usually true
            public bool disablePhysics;            // Colliders/RBs/Joints/CharacterController

            // Layer / tagging
            public bool setLayer;
            public int previewLayer;

            // Transform and parenting
            public Transform parent;
            public bool keepWorldPosition;
        }

        public static Options DefaultOptions => new Options
        {
            nameSuffix = " (PreviewClone)",
            keepRenderersEnabled = true,
            keepMeshFiltersEnabled = true,
            keepSkinnedMeshBones = true,

            disableMonoBehaviours = true,
            disableAnimators = true,
            disableParticles = true,
            disableLights = true,
            disableAudio = true,
            disableUI = true,
            disablePhysics = true,

            setLayer = false,
            previewLayer = 0,

            parent = null,
            keepWorldPosition = true,
        };

        /// <summary>
        /// Creates a preview clone. Returns the clone root.
        /// </summary>
        public static GameObject CreatePreviewClone(GameObject source, Options options)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // Instantiate (keep active initially so we can traverse enabled state reliably)
            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + options.nameSuffix;

            // Parent it (optional)
            if (options.parent != null)
            {
                clone.transform.SetParent(options.parent, options.keepWorldPosition);
            }

            // Layer (optional)
            if (options.setLayer)
            {
                SetLayerRecursively(clone, options.previewLayer);
            }

            // Build allow-lists first
            var keepEnabled = new HashSet<Behaviour>();
            var keepComponents = new HashSet<Component>();

            // Renderers should stay enabled
            if (options.keepRenderersEnabled)
            {
                foreach (var r in clone.GetComponentsInChildren<Renderer>(true))
                {
                    // Renderer is a Component not a Behaviour, but it has "enabled"
                    // We'll handle Renderer separately.
                    keepComponents.Add(r);
                }
            }

            // MeshFilters should stay (not enable/disable, but keep)
            if (options.keepMeshFiltersEnabled)
            {
                foreach (var mf in clone.GetComponentsInChildren<MeshFilter>(true))
                {
                    keepComponents.Add(mf);
                }
            }

            // Skinned meshes: keep the bones/transforms intact by not disabling their GameObjects
            // We'll handle GameObject deactivation carefully: we won't deactivate GO's at all;
            // we disable components instead. That avoids breaking bone hierarchies.
            // This option is kept for clarity/future expansions.
            // (No special action needed with this component-disabling approach.)

            // Now disable "behavior" components
            DisableBehaviorComponents(clone, options, keepEnabled, keepComponents);

            // Safety: ensure renderers are enabled (even if they were disabled on the source)
            if (options.keepRenderersEnabled)
            {
                foreach (var r in clone.GetComponentsInChildren<Renderer>(true))
                {
                    r.enabled = true;
                }
            }

            return clone;
        }

        private static void DisableBehaviorComponents(
            GameObject root,
            Options options,
            HashSet<Behaviour> keepEnabled,
            HashSet<Component> keepComponents)
        {
            // 1) Disable MonoBehaviours (scripts)
            if (options.disableMonoBehaviours)
            {
                foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    // Don't disable if it's on the allow-list (rare; kept for extension)
                    if (keepEnabled.Contains(mb)) continue;
                    mb.enabled = false;
                }
            }

            // 2) Disable animators
            if (options.disableAnimators)
            {
                foreach (var a in root.GetComponentsInChildren<Animator>(true))
                {
                    a.enabled = false;
                }
            }

            // 3) Disable particles
            if (options.disableParticles)
            {
                foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    // Stop + clear; doesn't rely on enabled flags
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                foreach (var psr in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    // ParticleSystemRenderer is a Renderer; if you truly want *no* particles,
                    // disable it explicitly even though it's a renderer.
                    // If you want particles visible, remove this.
                    psr.enabled = false;
                }
            }

            // 4) Disable lights
            if (options.disableLights)
            {
                foreach (var l in root.GetComponentsInChildren<Light>(true))
                {
                    l.enabled = false;
                }
            }

            // 5) Disable audio
            if (options.disableAudio)
            {
                foreach (var al in root.GetComponentsInChildren<AudioListener>(true))
                {
                    al.enabled = false;
                }
                foreach (var asc in root.GetComponentsInChildren<AudioSource>(true))
                {
                    asc.enabled = false;
                    asc.Stop();
                }
            }

            // 6) Disable UI (Canvas etc.)
            if (options.disableUI)
            {
                foreach (var c in root.GetComponentsInChildren<Canvas>(true))
                {
                    c.enabled = false;
                }
                foreach (var gr in root.GetComponentsInChildren<CanvasGroup>(true))
                {
                    gr.interactable = false;
                    gr.blocksRaycasts = false;
                    gr.alpha = 0f;
                }
            }

            // 7) Disable physics
            if (options.disablePhysics)
            {
                foreach (var col in root.GetComponentsInChildren<Collider>(true))
                {
                    col.enabled = false;
                }
                foreach (var col2d in root.GetComponentsInChildren<Collider2D>(true))
                {
                    col2d.enabled = false;
                }
                foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
                {
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                foreach (var rb2d in root.GetComponentsInChildren<Rigidbody2D>(true))
                {
                    rb2d.simulated = false;
                    rb2d.linearVelocity = Vector2.zero;
                    rb2d.angularVelocity = 0f;
                }
                foreach (var cc in root.GetComponentsInChildren<CharacterController>(true))
                {
                    cc.enabled = false;
                }
            }

            // 8) Finally: disable any Behaviour we didn't explicitly keep that might still run
            // (NavMeshAgent, Playables, etc.)
            foreach (var b in root.GetComponentsInChildren<Behaviour>(true))
            {
                // Keep the ones we know are essential (none by default)
                if (keepEnabled.Contains(b)) continue;

                // NOTE: MeshRenderer/SkinnedMeshRenderer are NOT Behaviours, so they won't appear here.
                // This is safe.
                if (b is Transform) continue; // not possible; Transform isn't a Behaviour

                // If user asked to keep it, don't touch it
                if (keepComponents.Contains(b)) continue;

                // If it's a Light/Audio/etc. we handled above it is already disabled, but this is fine.
                b.enabled = false;
            }

            // 9) Disable any non-renderer Components you want “hard off” by type could be added here.
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = layer;
            }
        }

        /// <summary>
        /// Clones a scene GameObject and strips EVERYTHING except:
        /// - Rendering (Renderer + MeshFilter, SkinnedMeshRenderer, SpriteRenderer, etc.)
        /// - UI (Canvas/RectTransform/CanvasRenderer + UnityEngine.UI + TMPro by namespace)
        ///
        /// This intentionally removes most behavior instead of disabling it.
        /// </summary>
        public static GameObject CreateSimpleRenderOnlyClone(GameObject source, Options options)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + (string.IsNullOrEmpty(options.nameSuffix) ? " (RenderOnlyClone)" : options.nameSuffix);

            if (options.parent != null)
                clone.transform.SetParent(options.parent, options.keepWorldPosition);

            if (options.setLayer)
                SetLayerRecursively(clone, options.previewLayer);

            StripToRenderingAndUI(clone);

            // Safety: ensure any remaining renderers are enabled
            foreach (var r in clone.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;

            return clone;
        }

        private static void StripToRenderingAndUI(GameObject root)
        {
            // Gather everything first, then destroy.
            var toDestroy = new List<Component>(256);

            // We want to know which GameObjects are "UI hosts" so we don't kill needed UI helpers.
            // If a GO has CanvasRenderer or any Unity UI/TMPro component, consider it UI-ish.
            var uiGOs = new HashSet<GameObject>();

            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                if (IsUIComponent(c)) uiGOs.Add(c.gameObject);
            }

            foreach (var c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;

                // Never remove Transform/RectTransform (RectTransform derives from Transform as a component instance).
                if (c is Transform) continue;

                // Keep UI components (Canvas, CanvasRenderer, Graphic, TMP, Layout, etc.)
                if (IsUIComponent(c)) continue;

                // Keep renderers, but NOT particle renderers (they need ParticleSystem to make sense)
                if (IsAllowedRenderer(c)) continue;

                // Keep MeshFilter only if it is supporting a non-skinned mesh renderer on the same GO.
                if (c is MeshFilter mf)
                {
                    // If it has MeshRenderer, keep it. Otherwise toss it.
                    if (mf.GetComponent<MeshRenderer>() != null)
                        continue;

                    // Skinned meshes don't use MeshFilter.
                    toDestroy.Add(c);
                    continue;
                }

                // Optional: keep LODGroup if you want renderer switching to remain.
                // If not, remove it.
                if (c is LODGroup)
                {
                    // Keeping it usually doesn't "run gameplay", but it does affect render visibility.
                    // If you'd rather strip it, comment this 'continue' out.
                    continue;
                }

                // If this GO is UI-ish, be extra conservative and keep "harmless" UI helpers by namespace.
                // (This is mostly redundant with IsUIComponent, but safe.)
                if (uiGOs.Contains(c.gameObject))
                {
                    var ns = c.GetType().Namespace ?? "";
                    if (ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal) ||
                        ns.StartsWith("TMPro", StringComparison.Ordinal))
                        continue;
                }

                // Otherwise: remove it.
                toDestroy.Add(c);
            }

            // Destroy components
            for (int i = 0; i < toDestroy.Count; i++)
            {
                var c = toDestroy[i];
                if (c == null) continue;
                UnityEngine.Object.DestroyImmediate(c);
            }

            // Also kill particle systems entirely (they can still tick if something slips through)
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (ps == null) continue;
                UnityEngine.Object.DestroyImmediate(ps);
            }
        }

        private static bool IsAllowedRenderer(Component c)
        {
            // Keep most renderers (MeshRenderer, SkinnedMeshRenderer, SpriteRenderer, LineRenderer, etc.)
            // BUT exclude ParticleSystemRenderer because it depends on ParticleSystem behavior.
            if (c is ParticleSystemRenderer) return false;
            return c is Renderer;
        }

        private static bool IsUIComponent(Component c)
        {
            if (c == null) return false;

            // Core UI plumbing
            if (c is Canvas) return true;
            if (c is CanvasRenderer) return true;
            if (c is RectTransform) return true;   // RectTransform is a component instance
            if (c is CanvasGroup) return true;

            // Namespace-based catch-all:
            // - UnityEngine.UI (Image, Text, RawImage, LayoutGroup, Mask, etc.)
            // - TMPro (TMP_Text, TextMeshProUGUI, TMP_SubMeshUI, etc.)
            var ns = c.GetType().Namespace ?? "";
            if (ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal)) return true;
            if (ns.StartsWith("TMPro", StringComparison.Ordinal)) return true;

            return false;
        }
        [Serializable]
        public struct RenderReplicaOptions
        {
            public string nameSuffix;
            public Transform parent;
            public bool keepWorldPosition;
            public bool setLayer;
            public int layer;

            public bool includeUI;
            public bool includeLineAndTrail;
            public bool bakeSkinnedMeshesToStatic; // recommended default = true
        }

        public static RenderReplicaOptions DefaultReplicaOptions => new RenderReplicaOptions
        {
            nameSuffix = " (RenderReplica)",
            parent = null,
            keepWorldPosition = true,
            setLayer = false,
            layer = 0,
            includeUI = false,
            includeLineAndTrail = true,
            bakeSkinnedMeshesToStatic = true,
        };

        public static GameObject CreateRenderReplica(GameObject source, RenderReplicaOptions opt)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // 1) Clone only transforms (hierarchy skeleton)
            var srcToDst = new Dictionary<Transform, Transform>(256);
            var dstRoot = CloneTransformHierarchy(source.transform, opt, srcToDst);

            // 2) Copy render components onto matching transforms
            foreach (var kvp in srcToDst)
            {
                var srcT = kvp.Key;
                var dstT = kvp.Value;

                CopyStaticRenderers(srcT, dstT, opt);

                if (opt.bakeSkinnedMeshesToStatic)
                    BakeSkinnedToStaticIfPresent(srcT, dstT);
                else
                    CopySkinnedRendererIfPresent_WithRemap(srcT, dstT, srcToDst); // optional path
            }

            return dstRoot.gameObject;
        }

        private static Transform CloneTransformHierarchy(
            Transform srcRoot,
            RenderReplicaOptions opt,
            Dictionary<Transform, Transform> map)
        {
            var dstRootGO = new GameObject(srcRoot.name + opt.nameSuffix);
            var dstRoot = dstRootGO.transform;

            if (opt.parent != null)
                dstRoot.SetParent(opt.parent, opt.keepWorldPosition);

            // Copy TRS (local if parented to replica, world if not)
            // We'll do local TRS mirroring under the replica skeleton:
            dstRoot.localPosition = srcRoot.localPosition;
            dstRoot.localRotation = srcRoot.localRotation;
            dstRoot.localScale = srcRoot.localScale;

            map[srcRoot] = dstRoot;

            // Recursively create children
            CloneChildrenRecursive(srcRoot, dstRoot, opt, map);

            if (opt.setLayer) SetLayerRecursively(dstRootGO, opt.layer);

            return dstRoot;
        }

        private static void CloneChildrenRecursive(
            Transform srcParent,
            Transform dstParent,
            RenderReplicaOptions opt,
            Dictionary<Transform, Transform> map)
        {
            for (int i = 0; i < srcParent.childCount; i++)
            {
                var srcChild = srcParent.GetChild(i);
                var dstChildGO = new GameObject(srcChild.name);
                var dstChild = dstChildGO.transform;

                dstChild.SetParent(dstParent, false);
                dstChild.localPosition = srcChild.localPosition;
                dstChild.localRotation = srcChild.localRotation;
                dstChild.localScale = srcChild.localScale;

                map[srcChild] = dstChild;

                CloneChildrenRecursive(srcChild, dstChild, opt, map);
            }
        }

        private static void CopyStaticRenderers(Transform srcT, Transform dstT, RenderReplicaOptions opt)
        {
            // MeshRenderer + MeshFilter
            var srcMR = srcT.GetComponent<MeshRenderer>();
            var srcMF = srcT.GetComponent<MeshFilter>();
            if (srcMR != null && srcMF != null && srcMF.sharedMesh != null)
            {
                var dstMF = dstT.gameObject.AddComponent<MeshFilter>();
                dstMF.sharedMesh = srcMF.sharedMesh;

                var dstMR = dstT.gameObject.AddComponent<MeshRenderer>();
                dstMR.sharedMaterials = srcMR.sharedMaterials;

                // Copy a few common render flags
                dstMR.shadowCastingMode = srcMR.shadowCastingMode;
                dstMR.receiveShadows = srcMR.receiveShadows;
                dstMR.lightProbeUsage = srcMR.lightProbeUsage;
                dstMR.reflectionProbeUsage = srcMR.reflectionProbeUsage;
                dstMR.allowOcclusionWhenDynamic = srcMR.allowOcclusionWhenDynamic;
                dstMR.enabled = true;
            }

            // SpriteRenderer
            var srcSR = srcT.GetComponent<SpriteRenderer>();
            if (srcSR != null)
            {
                var dstSR = dstT.gameObject.AddComponent<SpriteRenderer>();
                dstSR.sprite = srcSR.sprite;
                dstSR.sharedMaterial = srcSR.sharedMaterial;
                dstSR.color = srcSR.color;
                dstSR.flipX = srcSR.flipX;
                dstSR.flipY = srcSR.flipY;
                dstSR.sortingLayerID = srcSR.sortingLayerID;
                dstSR.sortingOrder = srcSR.sortingOrder;
                dstSR.enabled = true;
            }

            // LineRenderer / TrailRenderer (optional)
            if (opt.includeLineAndTrail)
            {
                var srcLR = srcT.GetComponent<LineRenderer>();
                if (srcLR != null)
                {
                    var dstLR = dstT.gameObject.AddComponent<LineRenderer>();
                    // Minimal copy (you can expand this)
                    dstLR.sharedMaterial = srcLR.sharedMaterial;
                    dstLR.widthMultiplier = srcLR.widthMultiplier;
                    dstLR.positionCount = srcLR.positionCount;
                    var tmp = new Vector3[srcLR.positionCount];
                    srcLR.GetPositions(tmp);
                    dstLR.SetPositions(tmp);
                    dstLR.enabled = true;
                }

                var srcTR = srcT.GetComponent<TrailRenderer>();
                if (srcTR != null)
                {
                    var dstTR = dstT.gameObject.AddComponent<TrailRenderer>();
                    dstTR.sharedMaterial = srcTR.sharedMaterial;
                    dstTR.widthMultiplier = srcTR.widthMultiplier;
                    dstTR.time = srcTR.time;
                    dstTR.enabled = true;
                }
            }

            // UI: I’d only include if you truly want it as visuals (usually you don’t).
            // If you do, we can add a separate UI replication pass.
        }

        private static void BakeSkinnedToStaticIfPresent(Transform srcT, Transform dstT)
        {
            var srcSMR = srcT.GetComponent<SkinnedMeshRenderer>();
            if (srcSMR == null || srcSMR.sharedMesh == null) return;

            // Bake current deformed pose into a new mesh
            var baked = new Mesh();
            srcSMR.BakeMesh(baked);

            var dstMF = dstT.gameObject.AddComponent<MeshFilter>();
            dstMF.sharedMesh = baked;

            var dstMR = dstT.gameObject.AddComponent<MeshRenderer>();
            dstMR.sharedMaterials = srcSMR.sharedMaterials;
            dstMR.shadowCastingMode = srcSMR.shadowCastingMode;
            dstMR.receiveShadows = srcSMR.receiveShadows;
            dstMR.enabled = true;
        }

        // Optional: “true” skinned copy path (more complex), only if you need it.
        private static void CopySkinnedRendererIfPresent_WithRemap(
            Transform srcT,
            Transform dstT,
            Dictionary<Transform, Transform> map)
        {
            var srcSMR = srcT.GetComponent<SkinnedMeshRenderer>();
            if (srcSMR == null || srcSMR.sharedMesh == null) return;

            var dstSMR = dstT.gameObject.AddComponent<SkinnedMeshRenderer>();
            dstSMR.sharedMesh = srcSMR.sharedMesh;
            dstSMR.sharedMaterials = srcSMR.sharedMaterials;

            // Remap bones/rootBone to cloned hierarchy
            if (srcSMR.rootBone != null && map.TryGetValue(srcSMR.rootBone, out var dstRootBone))
                dstSMR.rootBone = dstRootBone;

            var srcBones = srcSMR.bones;
            if (srcBones != null && srcBones.Length > 0)
            {
                var dstBones = new Transform[srcBones.Length];
                for (int i = 0; i < srcBones.Length; i++)
                {
                    var b = srcBones[i];
                    if (b != null && map.TryGetValue(b, out var dstB))
                        dstBones[i] = dstB;
                }
                dstSMR.bones = dstBones;
            }

            dstSMR.enabled = true;
        }
    }
}
