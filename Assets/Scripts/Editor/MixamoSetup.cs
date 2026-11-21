using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Mixamo characters (downloaded by the owner, free for games): Alex in Assets/Art/Models/Mixamo/Alex and
    /// The Echo's body + zombie animations in .../Echo. Sets every file up as a Humanoid rig (so any Mixamo
    /// animation drives any Mixamo body), scales bodies to a real height, extracts textures, marks loops and
    /// builds the animator controllers. Falls back to the CC0 mannequin when a file is missing.
    /// </summary>
    public static class MixamoSetup
    {
        public const string Root = "Assets/Art/Models/Mixamo";
        public const string AlexModel = Root + "/Alex/Alex.fbx";
        public const string EchoModel = Root + "/Echo/Echo.fbx";
        public const string AlexController = Root + "/Alex/AlexAnimator.controller";
        public const string EchoController = Root + "/Echo/EchoAnimator.controller";
        private const string Version = "mixamo-v1";

        public static bool HasAlex => File.Exists(AlexModel);
        public static bool HasEcho => File.Exists(EchoModel);

        [MenuItem("The Last Signal/Build/Setup Mixamo Characters")]
        public static void SetupAll()
        {
            if (HasAlex) { Character(AlexModel, 1.78f); Animations(Root + "/Alex", Avatar(AlexModel)); AlexAnimator(true); }
            if (HasEcho) { Character(EchoModel, 1.8f); }
            if (HasAlex || HasEcho)
            {
                Avatar src = HasEcho ? Avatar(EchoModel) : Avatar(AlexModel);
                Animations(Root + "/Echo", src);
                EchoAnimator(true);
            }
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ import settings

        /// <summary>Humanoid body scaled so it stands targetHeight tall, with textures extracted.</summary>
        public static GameObject Character(string path, float targetHeight)
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) return null;
            bool changed = false;
            string texDir = Path.GetDirectoryName(path).Replace('\\', '/') + "/Textures";
            if (!AssetDatabase.IsValidFolder(texDir))
            {
                EnsureFolder(texDir);
                imp.ExtractTextures(texDir);
                AssetDatabase.Refresh();
                imp = (ModelImporter)AssetImporter.GetAtPath(path);
            }
            if (imp.animationType != ModelImporterAnimationType.Human) { imp.animationType = ModelImporterAnimationType.Human; changed = true; }
            if (imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; changed = true; }
            if (imp.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription) { imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; changed = true; }
            if (imp.importAnimation) { imp.importAnimation = false; changed = true; }
            if (changed) imp.SaveAndReimport();

            // Scale to a real height (Mixamo exports vary a lot).
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            float h = Height(model);
            if (h > 0.1f && Mathf.Abs(h - targetHeight) > 0.03f)
            {
                imp.globalScale *= targetHeight / h;
                imp.SaveAndReimport();
                model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            // The humanoid description keeps the skeleton it was first made from; rebuild it at the final
            // scale or the avatar's human scale stays wrong (bodies drawn ~100x too high).
            imp = (ModelImporter)AssetImporter.GetAtPath(path);
            if (imp.userData != "avatar-rebuilt-v1")
            {
                ResetAvatar(path);
                imp = (ModelImporter)AssetImporter.GetAtPath(path);
                imp.userData = "avatar-rebuilt-v1";
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void ResetAvatar(string path)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            imp.SaveAndReimport();
            imp = (ModelImporter)AssetImporter.GetAtPath(path);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.SaveAndReimport();
        }

        private static float Height(GameObject model)
        {
            if (model == null) return 0f;
            Renderer[] rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 0f;
            float min = float.MaxValue, max = float.MinValue;
            foreach (Renderer r in rs) { min = Mathf.Min(min, r.bounds.min.y); max = Mathf.Max(max, r.bounds.max.y); }
            return max - min;
        }

        public static Avatar Avatar(string path)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Avatar a && a.isHuman) return a;
            return null;
        }

        /// <summary>Every animation file in a folder: Humanoid, in place, loops marked, clip named after the file.</summary>
        public static void Animations(string folder, Avatar source)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;
            foreach (string file in Directory.GetFiles(folder, "*.fbx"))
            {
                string path = file.Replace('\\', '/');
                if (path == AlexModel || path == EchoModel) continue;
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) continue;
                string clipName = Path.GetFileNameWithoutExtension(path);
                bool loop = !(clipName.Contains("Scream") || clipName.Contains("Attack"));
                imp.animationType = ModelImporterAnimationType.Human;
                // Each file builds its own humanoid avatar: animations downloaded on different Mixamo
                // characters have different skeletons, and humanoid retargeting maps them onto any body.
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importAnimation = true;
                // Keyframe reduction crushed the Echo's clips to two keys per curve (a run cycle became a straight
                // slide between two poses), so its animations are imported uncompressed.
                if (folder.EndsWith("/Echo")) imp.animationCompression = ModelImporterAnimationCompression.Off;
                ModelImporterClipAnimation[] clips = imp.defaultClipAnimations;
                if (clips.Length == 0)
                {
                    // First import with the humanoid rig: import once so the default clip exists, then configure it.
                    imp.SaveAndReimport();
                    imp = (ModelImporter)AssetImporter.GetAtPath(path);
                    clips = imp.defaultClipAnimations;
                }
                foreach (ModelImporterClipAnimation c in clips)
                {
                    c.name = clipName;
                    c.loopTime = loop;
                    c.loopPose = loop;
                    // In place: bake root motion into the pose so the character never drifts.
                    c.lockRootRotation = true;
                    c.keepOriginalOrientation = true;
                    // Height from the feet (not "original": that is in the source file's centimetres).
                    c.lockRootHeightY = true;
                    c.keepOriginalPositionY = false;
                    c.heightFromFeet = true;
                    c.lockRootPositionXZ = true;
                    c.keepOriginalPositionXZ = false;
                }
                imp.clipAnimations = clips;
                imp.SaveAndReimport();
            }
        }

        public static AnimationClip Clip(string folder, params string[] names)
        {
            foreach (string n in names)
            {
                string path = Root + "/" + folder + "/" + n + ".fbx";
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview")) return c;
            }
            Debug.LogWarning("[MixamoSetup] Clip not found in " + folder + ": " + string.Join("/", names));
            return null;
        }

        // ------------------------------------------------------------------ controllers

        /// <summary>Alex: stand (idle / walk / run), stand with the torch (run with torch), crouch.</summary>
        public static AnimatorController AlexAnimator(bool force = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AlexController);
            if (!force && existing != null && AssetImporter.GetAtPath(AlexController).userData == Version) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(AlexController);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(AlexController);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Torch", AnimatorControllerParameterType.Bool);
            AnimationClip idle = Clip("Alex", "Idle", "Idel"), walk = Clip("Alex", "Walking", "Walk"), run = Clip("Alex", "Running", "Run");
            AnimationClip torchRun = Clip("Alex", "Running with torch") ?? run;
            AnimatorState stand = Blend(ctrl, "Stand", (idle, 0f), (walk, 2.2f), (run, 4f));
            AnimatorState standTorch = Blend(ctrl, "StandTorch", (idle, 0f), (walk, 2.2f), (torchRun, 4f));
            AnimatorState crouch = Blend(ctrl, "Crouch", (Clip("Alex", "Crouching Idle", "Crouch Idle"), 0f), (Clip("Alex", "Crouched Walking", "Crouch Walk"), 1.2f));
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            sm.defaultState = stand;
            Link(stand, standTorch, AnimatorConditionMode.If, "Torch");
            Link(standTorch, stand, AnimatorConditionMode.IfNot, "Torch");
            foreach (AnimatorState s in new[] { stand, standTorch })
            {
                Link(s, crouch, AnimatorConditionMode.If, "Crouch");
            }
            Link(crouch, stand, AnimatorConditionMode.IfNot, "Crouch");
            Finish(ctrl, AlexController);
            return ctrl;
        }

        /// <summary>
        /// Gait speeds in metres per second: the speed each locomotion clip is stretched to match, so the feet
        /// stay planted instead of skating over the floor. They line up with the Echo's patrol (1.3), investigate
        /// (2.0) and chase (3.3, surging 0.9-1.15x) speeds in EchoSettings.
        /// </summary>
        public const float EchoWalkSpeed = 1.3f, EchoLurchSpeed = 2.1f, EchoRunSpeed = 3.3f;
        /// <summary>Measured natural speeds of the source clips (m/s at the Echo's size): Alex's Walking and Zombie Running.</summary>
        private const float WalkClipSpeed = 0.72f, RunClipSpeed = 3.0f;
        private const string EchoVersion = "echo-v2";

        /// <summary>
        /// The Echo: idle / walk / lurching jog / run by speed; pressing at a hiding place; towering over you when it
        /// catches you. The supplied "Zombie Walk" has no leg movement at all (the legs hold one pose), so the walk
        /// is Alex's walking clip, hunched and long-armed by EchoPresence; the jog and run are the Zombie Running
        /// clip played at two speeds.
        /// </summary>
        public static AnimatorController EchoAnimator(bool force = false)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(EchoController);
            if (!force && existing != null && AssetImporter.GetAtPath(EchoController).userData == EchoVersion) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(EchoController);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(EchoController);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("State", AnimatorControllerParameterType.Int);
            AnimationClip idle = Clip("Echo", "Zombie Idle");
            AnimationClip run = Clip("Echo", "Zombie Running");
            AnimationClip walk = Clip("Alex", "Walking");
            AnimatorState move = ctrl.CreateBlendTreeInController("Move", out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null) tree.AddChild(walk, EchoWalkSpeed);
            if (run != null) { tree.AddChild(run, EchoLurchSpeed); tree.AddChild(run, EchoRunSpeed); }
            ChildMotion[] kids = tree.children;
            for (int i = 0; i < kids.Length; i++)
            {
                float t = kids[i].threshold;
                kids[i].timeScale = t < 0.01f ? 1f : Mathf.Approximately(t, EchoWalkSpeed) ? EchoWalkSpeed / WalkClipSpeed : t / RunClipSpeed;
            }
            tree.children = kids;
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            sm.defaultState = move;
            Pose(sm, move, "Listen", idle, (int)Enemy.EchoAIController.State.Listen, 0.35f);
            Pose(sm, move, "Inspect", Clip("Echo", "Zombie Attack"), (int)Enemy.EchoAIController.State.Inspect, 0.3f);
            Pose(sm, move, "Caught", Clip("Echo", "Zombie Scream"), (int)Enemy.EchoAIController.State.Caught, 0.08f);
            Finish(ctrl, EchoController);
            AssetImporter.GetAtPath(EchoController).userData = EchoVersion;
            AssetDatabase.WriteImportSettingsIfDirty(EchoController);
            return ctrl;
        }

        private static AnimatorState Blend(AnimatorController ctrl, string name, params (AnimationClip clip, float speed)[] motions)
        {
            AnimatorState state = ctrl.CreateBlendTreeInController(name, out BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            foreach (var m in motions) if (m.clip != null) tree.AddChild(m.clip, m.speed);
            return state;
        }

        private static void Link(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string param)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.AddCondition(mode, 0f, param);
            t.hasExitTime = false;
            t.duration = 0.25f;
        }

        private static void Pose(AnimatorStateMachine sm, AnimatorState move, string name, AnimationClip clip, int state, float blend)
        {
            AnimatorState s = sm.AddState(name);
            s.motion = clip;
            AnimatorStateTransition enter = sm.AddAnyStateTransition(s);
            enter.AddCondition(AnimatorConditionMode.Equals, state, "State");
            if (name == "Inspect") enter.AddCondition(AnimatorConditionMode.Less, 0.3f, "Speed");
            enter.duration = blend;
            enter.canTransitionToSelf = false;
            enter.hasExitTime = false;
            AnimatorStateTransition exit = s.AddTransition(move);
            exit.AddCondition(AnimatorConditionMode.NotEqual, state, "State");
            exit.duration = 0.3f;
            exit.hasExitTime = false;
            if (name == "Inspect")
            {
                AnimatorStateTransition walkAway = s.AddTransition(move);
                walkAway.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
                walkAway.duration = 0.3f;
                walkAway.hasExitTime = false;
            }
        }

        private static void Finish(AnimatorController ctrl, string path)
        {
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetImporter.GetAtPath(path).userData = Version;
            AssetDatabase.WriteImportSettingsIfDirty(path);
        }

        // ------------------------------------------------------------------ helpers for prefabs

        /// <summary>Instantiate an unpacked humanoid body with its animator ready (avatar + controller).</summary>
        public static GameObject Body(string modelPath, RuntimeAnimatorController controller, Transform parent)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) return null;
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            body.name = "Body";
            body.transform.SetParent(parent, false);
            Animator a = body.GetComponent<Animator>();
            if (a == null) a = body.AddComponent<Animator>();
            a.avatar = BuiltAvatar(modelPath, body);
            a.runtimeAnimatorController = controller;
            a.applyRootMotion = false;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            return body;
        }

        /// <summary>
        /// Humanoid avatar built from the body's actual (scaled) skeleton. The importer's own avatar for these
        /// Mixamo files keeps a centimetre human scale (~100), which places the body ~100 m up in the air.
        /// </summary>
        public static Avatar BuiltAvatar(string modelPath, GameObject instance)
        {
            string assetPath = modelPath.Replace(".fbx", "_HumanAvatar.asset");
            var imp = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            HumanDescription desc = imp.humanDescription;
            var bones = new List<SkeletonBone>();
            foreach (Transform t in instance.GetComponentsInChildren<Transform>())
                bones.Add(new SkeletonBone { name = t == instance.transform ? Path.GetFileNameWithoutExtension(modelPath) : t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            desc.skeleton = bones.ToArray();
            string originalName = instance.name;
            instance.name = Path.GetFileNameWithoutExtension(modelPath);
            Avatar av = AvatarBuilder.BuildHumanAvatar(instance, desc);
            instance.name = originalName;
            av.name = Path.GetFileNameWithoutExtension(modelPath) + "_HumanAvatar";
            // Keep one asset (same GUID) so prefabs built earlier in the same run keep their reference.
            Avatar existing = AssetDatabase.LoadAssetAtPath<Avatar>(assetPath);
            if (existing == null) { AssetDatabase.CreateAsset(av, assetPath); return av; }
            EditorUtility.CopySerialized(av, existing);
            existing.name = av.name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(av);
            return existing;
        }

        public static Transform Bone(GameObject body, HumanBodyBones bone)
        {
            Animator a = body.GetComponent<Animator>();
            return a != null && a.isHuman ? a.GetBoneTransform(bone) : null;
        }
    }
}
