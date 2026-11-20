using LastSignal.Enemy;
using LastSignal.Environment;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Player;
using LastSignal.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>Builds the shared prefabs: Player, HUD (touch controls + story UI + menus) and The Echo.</summary>
    public static class GamePrefabs
    {
        public const string PlayerPath = "Assets/Prefabs/Player/Player.prefab";
        public const string HudPath = "Assets/Prefabs/UI/HUD.prefab";
        public const string EchoPath = "Assets/Prefabs/Enemy/Echo.prefab";

        [MenuItem("The Last Signal/Build/Rebuild Shared Prefabs")]
        public static void RebuildAll()
        {
            BuildPlayer();
            BuildHud();
            BuildEcho();
            ScareKit.RebuildPrefabs();
            Debug.Log("[GamePrefabs] Rebuilt Player, HUD, Echo and apparition prefabs.");
        }

        public static GameObject Player(bool rebuild = false) =>
            !rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) != null ? AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) : BuildPlayer();

        public static GameObject Hud(bool rebuild = false) =>
            !rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) != null ? AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) : BuildHud();

        public static GameObject Echo(bool rebuild = false) =>
            !rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(EchoPath) != null ? AssetDatabase.LoadAssetAtPath<GameObject>(EchoPath) : BuildEcho();

        // ================================================================== Player

        public static GameObject BuildPlayer()
        {
            EnsureFolder("Assets/Prefabs/Player");
            var root = new GameObject("Player") { tag = "Player" };

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0f;

            var router = root.AddComponent<PlayerInputRouter>();
            var inventory = root.AddComponent<InventoryManager>();
            var controller = root.AddComponent<PlayerController>();
            var look = root.AddComponent<PlayerLook>();
            var interactor = root.AddComponent<PlayerInteractor>();
            var steps = root.AddComponent<AudioSource>();
            steps.playOnAwake = false;
            steps.spatialBlend = 0f;
            var noise = root.AddComponent<NoiseEmitter>();
            Set(noise, "stepSource", steps);
            var breathSource = root.AddComponent<AudioSource>();
            breathSource.playOnAwake = false;
            breathSource.spatialBlend = 0f;
            var breath = root.AddComponent<PlayerBreath>();
            Set(breath, "input", router);
            Set(breath, "source", breathSource);

            Transform cameraRoot = Group(root.transform, "CameraRoot", new Vector3(0f, 1.65f, 0f));
            var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(cameraRoot, false);
            // Eyes sit just in front of the (hidden) head of the body model.
            camGo.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.015f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            var flashGo = new GameObject("Flashlight");
            flashGo.transform.SetParent(cameraRoot, false);
            // Light comes from about where the left hand holds the torch.
            flashGo.transform.localPosition = new Vector3(-0.14f, -0.2f, 0.22f);
            var spot = flashGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            // Strong source with physical (inverse-square) falloff: a bright pool near the player, walls
            // a few metres away clearly lit, the rest of the room stays black.
            spot.range = 26f;
            spot.spotAngle = 68f;
            spot.innerSpotAngle = 30f;
            spot.intensity = 42f;
            spot.color = new Color(1f, 0.94f, 0.82f);
            // Real-time shadows on both presets (owner's decision): objects cast moving shadows in the beam.
            spot.shadows = LightShadows.Hard;
            spot.shadowStrength = 0.9f;
            spot.shadowNearPlane = 0.15f;
            spot.enabled = false;
            // The beam lights the world (layer 0) but not Alex's own arms and torch (BodyLayer).
            SetLightLayers(spot, 1u);
            var click = flashGo.AddComponent<AudioSource>();
            click.playOnAwake = false;
            click.spatialBlend = 0f;
            var flashlight = flashGo.AddComponent<FlashlightController>();

            Set(controller, "input", router);
            Set(controller, "cameraRoot", cameraRoot);
            Set(look, "input", router);
            Set(look, "pitchPivot", cameraRoot);
            Set(interactor, "input", router);
            Set(interactor, "viewCamera", cam);
            Set(interactor, "inventory", inventory);
            Set(interactor, "flashlight", flashlight);
            Set(flashlight, "spotLight", spot);
            Set(flashlight, "input", router);
            Set(flashlight, "inventory", inventory);
            Set(flashlight, "audioSource", click);
            flashGo.AddComponent<HandheldSway>();

            var motion = camGo.AddComponent<CameraMotion>();
            Set(motion, "controller", controller);
            Set(motion, "breath", breath);

            BuildPlayerBody(root, controller, look, interactor, flashlight);

            // Loose notes are held up here to read, lit by a dim warm light so they can be read with the torch off.
            Transform noteHold = Group(camGo.transform, "NoteHold", new Vector3(0.02f, -0.035f, 0.37f));
            var readGo = new GameObject("ReadingLight");
            readGo.transform.SetParent(noteHold, false);
            readGo.transform.localPosition = new Vector3(0.12f, 0.18f, -0.2f);
            var reading = readGo.AddComponent<Light>();
            reading.type = LightType.Point;
            reading.range = 0.9f;
            reading.intensity = 0.6f;
            reading.color = new Color(1f, 0.92f, 0.78f);
            reading.shadows = LightShadows.None;
            SetLightLayers(reading, 1u | BodyLayer); // the sheet and the hand holding it
            reading.enabled = false;
            var holder = root.AddComponent<NoteHolder>();
            Set(holder, "holdPoint", noteHold);
            Set(holder, "readingLight", reading);
            PlayerBody bodyRig = root.GetComponent<PlayerBody>();
            if (bodyRig != null) Set(holder, "body", bodyRig);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public const string PlayerAnimatorPath = "Assets/Art/Models/Player/PlayerAnimator.controller";
        /// <summary>Rendering layer used only by the first-person body and its props.</summary>
        public const uint BodyLayer = 1u << 1;


        /// <summary>
        /// Alex's visible body (same CC0 mannequin, dark jacket and gloves). Head hidden at runtime by
        /// PlayerBody; no shadows so it never blocks the torch beam.
        /// </summary>
        private static void BuildPlayerBody(GameObject root, PlayerController controller, PlayerLook look,
            PlayerInteractor interactor, FlashlightController flashlight)
        {
            if (MixamoSetup.HasAlex)
            {
                BuildMixamoPlayerBody(root, controller, look, interactor, flashlight);
                return;
            }
            GameObject model = EnsureEchoModel();
            if (model == null) return;
            var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // model faces -Z
            body.transform.localScale = Vector3.one * 1.03f;

            Material clothes = Mat("AlexClothes", new Color(0.075f, 0.08f, 0.09f), smoothness: 0.12f);
            Material gloves = Mat("AlexGloves", new Color(0.03f, 0.03f, 0.032f), smoothness: 0.3f);
            foreach (SkinnedMeshRenderer smr in body.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = new Material[smr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = i == 0 ? clothes : gloves;
                smr.sharedMaterials = mats;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.receiveShadows = true;
                smr.updateWhenOffscreen = true;
                smr.renderingLayerMask = BodyLayer;
            }

            Animator animator = body.GetComponent<Animator>();
            if (animator == null) animator = body.AddComponent<Animator>();
            animator.runtimeAnimatorController = EnsurePlayerAnimator(model);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Transform head = FindDeep(body.transform, "DEF-head");
            Transform[] spine = { FindDeep(body.transform, "DEF-spine.002"), FindDeep(body.transform, "DEF-spine.003") };

            // Torch in the left hand, placed using the torch-holding pose.
            GameObject torch = null, torchLens = null;
            Transform hand = FindDeep(body.transform, "DEF-hand.L");
            AnimationClip hold = EchoClip("Idle_Torch_Loop");
            if (hand != null && hold != null)
            {
                hold.SampleAnimation(body, 0f);
                Material torchMat = Mat("TorchBody", new Color(0.16f, 0.16f, 0.17f), smoothness: 0.65f);
                torch = Prim(root.transform, "Torch", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.05f, 0.12f, 0.05f), torchMat, collider: false);
                torch.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                torch.GetComponent<Renderer>().renderingLayerMask = BodyLayer;
                torch.transform.position = hand.position + hand.up * 0.07f + root.transform.forward * 0.05f;
                torch.transform.rotation = Quaternion.FromToRotation(Vector3.up, root.transform.forward);
                torch.transform.SetParent(hand, true);
                // Glowing lens at the front, shown while the torch is on.
                GameObject lens = Prim(torch.transform, "Lens", PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(1.15f, 0.04f, 1.15f),
                    UnlitMat("TorchLens", new Color(1f, 0.93f, 0.75f)), collider: false);
                lens.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lens.GetComponent<Renderer>().renderingLayerMask = BodyLayer;
                lens.SetActive(false);
                torchLens = lens;
                AnimationClip idle = EchoClip("Idle_Loop");
                if (idle != null) idle.SampleAnimation(body, 0f);
            }

            // Bounce light from the beam onto the hands; affects only the body layer.
            Transform cameraRoot = root.transform.Find("CameraRoot");
            var fillGo = new GameObject("BodyFill");
            fillGo.transform.SetParent(cameraRoot != null ? cameraRoot : root.transform, false);
            fillGo.transform.localPosition = new Vector3(0.05f, 0.05f, 0.55f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 1.4f;
            fill.intensity = 0.12f;
            fill.color = new Color(1f, 0.92f, 0.8f);
            fill.shadows = LightShadows.None;
            SetLightLayers(fill, BodyLayer);

            var pb = root.AddComponent<PlayerBody>();
            Set(pb, "animator", animator);
            Set(pb, "controller", controller);
            Set(pb, "look", look);
            Set(pb, "interactor", interactor);
            Set(pb, "flashlight", flashlight);
            Set(pb, "head", head);
            SetArray(pb, "spine", spine);
            SetArray(pb, "rightArm", new Object[] { FindDeep(body.transform, "DEF-upper_arm.R"), FindDeep(body.transform, "DEF-forearm.R"), FindDeep(body.transform, "DEF-hand.R") });
            SetArray(pb, "leftArm", new Object[] { FindDeep(body.transform, "DEF-upper_arm.L"), FindDeep(body.transform, "DEF-forearm.L"), FindDeep(body.transform, "DEF-hand.L") });
            if (torch != null) Set(pb, "torchModel", torch);
            if (torchLens != null) Set(pb, "torchLens", torchLens);
            Set(pb, "bodyFill", fill);
            Camera eyesCam = root.GetComponentInChildren<Camera>();
            if (eyesCam != null) Set(pb, "eyes", eyesCam.transform);
        }

        /// <summary>
        /// Alex as a real person (Mixamo character, humanoid): walk / run / run-with-torch / crouch clips, the
        /// torch held up by IK in the left hand while it is on (tucked away when off), the right hand reaching
        /// for what you use. Head hidden; the beam does not light the body (rendering layers).
        /// </summary>
        /// <summary>
        /// A fresh URP Lit material built from an imported Mixamo material's textures. The imported ones use the
        /// specular workflow with colour-imported normal maps, which left Alex's body black or a flat blue-grey.
        /// Matte, no environment reflections; normal map imported as a normal map; textures capped at 1024 px.
        /// </summary>
        private static Material MatteCopy(Material source)
        {
            if (source == null) return null;
            string dir = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(source)).Replace('\\', '/') + "/Materials";
            EnsureFolder(dir);
            string path = dir + "/" + source.name + "_Matte.mat";
            Texture baseTex = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
            Texture2D normal = null;
            if (baseTex != null)
            {
                string texPath = AssetDatabase.GetAssetPath(baseTex);
                Cap(texPath, false);
                string normalPath = texPath.Replace("_Diffuse", "_Normal");
                if (normalPath != texPath && System.IO.File.Exists(normalPath))
                {
                    Cap(normalPath, true);
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                }
            }
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader.name != "Universal Render Pipeline/Lit")
            {
                if (m != null) AssetDatabase.DeleteAsset(path);
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.name = source.name + "_Matte";
            m.SetFloat("_WorkflowMode", 1f); // metallic
            m.SetTexture("_BaseMap", baseTex);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0.2f);
            m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Character textures: 1024 px is plenty on a phone; normal maps imported as normal maps.</summary>
        private static void Cap(string texPath, bool isNormal)
        {
            var ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (ti == null) return;
            bool changed = false;
            if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; changed = true; }
            if (isNormal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; changed = true; }
            if (changed) ti.SaveAndReimport();
        }

        private static void BuildMixamoPlayerBody(GameObject root, PlayerController controller, PlayerLook look,
            PlayerInteractor interactor, FlashlightController flashlight)
        {
            GameObject body = MixamoSetup.Body(MixamoSetup.AlexModel, MixamoSetup.AlexAnimator(), root.transform);
            if (body == null) return;
            foreach (SkinnedMeshRenderer smr in body.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // Matte copies of the imported materials: the originals are fairly glossy and reflect the default
                // sky, which turned skin and clothes a flat blue-grey in the dark.
                var mats = smr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = MatteCopy(mats[i]);
                smr.sharedMaterials = mats;
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.receiveShadows = true;
                smr.updateWhenOffscreen = true;
                smr.renderingLayerMask = BodyLayer;
            }
            Animator animator = body.GetComponent<Animator>();
            Transform Bone(HumanBodyBones b) => MixamoSetup.Bone(body, b);

            // The torch hangs in front of the view (where the left hand holds it); the hand is pulled to it by IK.
            Transform cameraRoot = root.transform.Find("CameraRoot");
            Transform grip = Group(cameraRoot != null ? cameraRoot : root.transform, "TorchGrip", new Vector3(-0.2f, -0.19f, 0.42f));
            Material torchMat = Mat("TorchBody", new Color(0.16f, 0.16f, 0.17f), smoothness: 0.65f);
            GameObject torch = Prim(grip, "Torch", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.05f), new Vector3(0.045f, 0.11f, 0.045f), torchMat,
                new Vector3(90f, 0f, 0f), collider: false);
            torch.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            torch.GetComponent<Renderer>().renderingLayerMask = BodyLayer;
            GameObject lens = Prim(torch.transform, "Lens", PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(1.2f, 0.04f, 1.2f),
                UnlitMat("TorchLens", new Color(1f, 0.93f, 0.75f)), collider: false);
            lens.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lens.SetActive(false);
            torch.SetActive(false);

            var fillGo = new GameObject("BodyFill");
            fillGo.transform.SetParent(cameraRoot != null ? cameraRoot : root.transform, false);
            fillGo.transform.localPosition = new Vector3(0.05f, 0.05f, 0.55f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 1.4f;
            fill.intensity = 0.12f;
            fill.color = new Color(1f, 0.92f, 0.8f);
            fill.shadows = LightShadows.None;
            SetLightLayers(fill, BodyLayer);

            var pb = root.AddComponent<PlayerBody>();
            Set(pb, "animator", animator);
            Set(pb, "controller", controller);
            Set(pb, "look", look);
            Set(pb, "interactor", interactor);
            Set(pb, "flashlight", flashlight);
            Set(pb, "head", Bone(HumanBodyBones.Head));
            SetArray(pb, "spine", new Object[] { Bone(HumanBodyBones.Spine), Bone(HumanBodyBones.Chest) });
            SetArray(pb, "rightArm", new Object[] { Bone(HumanBodyBones.RightUpperArm), Bone(HumanBodyBones.RightLowerArm), Bone(HumanBodyBones.RightHand) });
            SetArray(pb, "leftArm", new Object[] { Bone(HumanBodyBones.LeftUpperArm), Bone(HumanBodyBones.LeftLowerArm), Bone(HumanBodyBones.LeftHand) });
            Set(pb, "torchModel", torch);
            Set(pb, "torchLens", lens);
            Set(pb, "torchGrip", grip);
            Set(pb, "bodyFill", fill);
            Camera eyesCam = root.GetComponentInChildren<Camera>();
            if (eyesCam != null) Set(pb, "eyes", eyesCam.transform);
        }

        /// <summary>
        /// Base layer: stand (idle / walk / jog) and crouch (idle / crouch-walk) blends.
        /// Torch layer (left arm only): raise the torch; PlayerBody drives its weight. Hands reaching
        /// for things are procedural (two-bone IK in PlayerBody), not clips.
        /// </summary>
        private static UnityEditor.Animations.AnimatorController EnsurePlayerAnimator(GameObject model)
        {
            const string version = "player-animator-v2";
            EnsureFolder("Assets/Art/Models/Player");
            var existing = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(PlayerAnimatorPath);
            if (existing != null && AssetImporter.GetAtPath(PlayerAnimatorPath).userData == version) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(PlayerAnimatorPath);

            AvatarMask leftArm = EnsureMask("Assets/Art/Models/Player/Mask_LeftArm.mask", model, "DEF-shoulder.L");

            var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Crouch", AnimatorControllerParameterType.Bool);

            // ---- base locomotion
            UnityEditor.Animations.AnimatorState stand = ctrl.CreateBlendTreeInController("Stand", out UnityEditor.Animations.BlendTree standTree, 0);
            standTree.blendParameter = "Speed";
            standTree.useAutomaticThresholds = false;
            standTree.AddChild(EchoClip("Idle_Loop"), 0f);
            standTree.AddChild(EchoClip("Walk_Loop"), 2.2f);
            standTree.AddChild(EchoClip("Jog_Fwd_Loop"), 4.0f);
            UnityEditor.Animations.AnimatorState crouch = ctrl.CreateBlendTreeInController("Crouch", out UnityEditor.Animations.BlendTree crouchTree, 0);
            crouchTree.blendParameter = "Speed";
            crouchTree.useAutomaticThresholds = false;
            crouchTree.AddChild(EchoClip("Crouch_Idle_Loop"), 0f);
            crouchTree.AddChild(EchoClip("Crouch_Fwd_Loop"), 1.2f);
            UnityEditor.Animations.AnimatorStateMachine baseSm = ctrl.layers[0].stateMachine;
            baseSm.defaultState = stand;
            var down = stand.AddTransition(crouch);
            down.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Crouch");
            down.hasExitTime = false;
            down.duration = 0.25f;
            var up = crouch.AddTransition(stand);
            up.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Crouch");
            up.hasExitTime = false;
            up.duration = 0.25f;

            // ---- torch (left arm)
            ctrl.AddLayer("Torch");
            UnityEditor.Animations.AnimatorControllerLayer[] layers = ctrl.layers;
            layers[1].avatarMask = leftArm;
            layers[1].defaultWeight = 0f;
            ctrl.layers = layers;
            UnityEditor.Animations.AnimatorState holdState = ctrl.layers[1].stateMachine.AddState("Hold");
            holdState.motion = EchoClip("Idle_Torch_Loop");

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetImporter.GetAtPath(PlayerAnimatorPath).userData = version;
            AssetDatabase.WriteImportSettingsIfDirty(PlayerAnimatorPath);
            return ctrl;
        }

        /// <summary>Generic-rig mask: only transforms whose path contains the given bone name animate.</summary>
        private static AvatarMask EnsureMask(string path, GameObject model, string mustContain)
        {
            if (AssetDatabase.LoadAssetAtPath<AvatarMask>(path) != null) AssetDatabase.DeleteAsset(path);
            var mask = new AvatarMask();
            mask.AddTransformPath(model.transform, true);
            for (int i = 0; i < mask.transformCount; i++)
                mask.SetTransformActive(i, mask.GetTransformPath(i).Contains(mustContain));
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            AssetDatabase.CreateAsset(mask, path);
            return mask;
        }

        // ================================================================== Echo

        public const string EchoModelPath = "Assets/Art/Models/Echo/UAL_Standard.fbx";
        public const string EchoAnimatorPath = "Assets/Art/Models/Echo/EchoAnimator.controller";

        public static GameObject BuildEcho()
        {
            EnsureFolder("Assets/Prefabs/Enemy");
            var root = new GameObject("Echo");
            // Charred, slightly damp skin: almost black, a dull sheen in the flashlight beam.
            Material skin = Mat("EchoSkin", new Color(0.03f, 0.028f, 0.03f), smoothness: 0.22f);
            Material eyes = Mat("EchoEyes", new Color(0.85f, 0.9f, 1f), emissive: true);
            Transform eye = Group(root.transform, "Eye", new Vector3(0f, 1.95f, 0.12f));

            Transform head;
            Transform[] spine = new Transform[0], upperArms = new Transform[0], lowerArms = new Transform[0];
            Renderer[] eyeRenderers;
            Animator animator = null;
            Transform[] hands = new Transform[0];
            string humanoid = MixamoSetup.HasEcho ? MixamoSetup.EchoModel : MixamoSetup.HasAlex ? MixamoSetup.AlexModel : null;
            GameObject model = humanoid == null ? EnsureEchoModel() : null;
            if (humanoid != null)
            {
                // Mixamo body (Echo.fbx, or until it exists a black silhouette of a person), driven by the zombie
                // animations, stretched tall and thin; EchoPresence adds the hunch, long arms and fingers, twitches.
                GameObject body = MixamoSetup.Body(humanoid, MixamoSetup.EchoAnimator(), root.transform);
                body.transform.localScale = new Vector3(0.95f, 1.15f, 0.95f);
                foreach (SkinnedMeshRenderer smr in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mats = new Material[smr.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = skin;
                    smr.sharedMaterials = mats;
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    smr.updateWhenOffscreen = false;
                }
                animator = body.GetComponent<Animator>();
                Transform Bone(HumanBodyBones b) => MixamoSetup.Bone(body, b);
                head = Bone(HumanBodyBones.Head) ?? body.transform;
                spine = new[] { Bone(HumanBodyBones.Spine), Bone(HumanBodyBones.Chest), Bone(HumanBodyBones.Neck) };
                upperArms = new[] { Bone(HumanBodyBones.LeftUpperArm), Bone(HumanBodyBones.RightUpperArm) };
                lowerArms = new[] { Bone(HumanBodyBones.LeftLowerArm), Bone(HumanBodyBones.RightLowerArm) };
                hands = new[] { Bone(HumanBodyBones.LeftHand), Bone(HumanBodyBones.RightHand) };
                eyeRenderers = new Renderer[2];
                for (int s = 0; s < 2; s++)
                {
                    GameObject e = Prim(root.transform, s == 0 ? "EyeL" : "EyeR", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.032f, eyes, collider: false);
                    e.transform.position = head.position + root.transform.up * 0.1f + root.transform.forward * 0.075f + root.transform.right * (s == 0 ? -0.032f : 0.032f);
                    e.transform.SetParent(head, true);
                    e.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    eyeRenderers[s] = e.GetComponent<Renderer>();
                }
            }
            else if (model != null)
            {
                // Quaternius mannequin (CC0), stretched into a tall, thin, wrong-looking figure.
                var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // model faces -Z
                body.transform.localScale = new Vector3(0.82f, 1.17f, 0.82f);
                foreach (SkinnedMeshRenderer smr in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mats = new Material[smr.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = skin;
                    smr.sharedMaterials = mats;
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    smr.updateWhenOffscreen = false;
                }
                animator = body.GetComponent<Animator>();
                if (animator == null) animator = body.AddComponent<Animator>();
                animator.runtimeAnimatorController = EnsureEchoAnimator();
                animator.applyRootMotion = false;
                // EchoPresence bends bones after the animation every frame, so the pose must be rewritten every frame.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                head = FindDeep(body.transform, "DEF-head") ?? body.transform;
                spine = new[] { FindDeep(body.transform, "DEF-spine.002"), FindDeep(body.transform, "DEF-spine.003"), FindDeep(body.transform, "DEF-neck") };
                upperArms = new[] { FindDeep(body.transform, "DEF-upper_arm.L"), FindDeep(body.transform, "DEF-upper_arm.R") };
                lowerArms = new[] { FindDeep(body.transform, "DEF-forearm.L"), FindDeep(body.transform, "DEF-forearm.R") };
                // Eyes in the model's own space (face towards -Z), then attached to the head bone.
                eyeRenderers = new Renderer[2];
                for (int s = 0; s < 2; s++)
                {
                    Vector3 local = new Vector3(s == 0 ? -0.034f : 0.034f, 1.685f, -0.105f);
                    GameObject e = Prim(root.transform, s == 0 ? "EyeL" : "EyeR", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.03f, eyes, collider: false);
                    e.transform.position = body.transform.TransformPoint(local);
                    e.transform.SetParent(head, true);
                    e.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    eyeRenderers[s] = e.GetComponent<Renderer>();
                }
            }
            else
            {
                // Fallback if the model is missing: primitive silhouette.
                Prim(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f), new Vector3(0.6f, 0.95f, 0.5f), skin, collider: false);
                head = Prim(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.78f, 0.02f), new Vector3(0.42f, 0.48f, 0.42f), skin, collider: false).transform;
                eyeRenderers = new Renderer[]
                {
                    Prim(root.transform, "EyeL", PrimitiveType.Sphere, new Vector3(-0.09f, 1.82f, 0.19f), Vector3.one * 0.06f, eyes, collider: false).GetComponent<Renderer>(),
                    Prim(root.transform, "EyeR", PrimitiveType.Sphere, new Vector3(0.09f, 1.82f, 0.19f), Vector3.one * 0.06f, eyes, collider: false).GetComponent<Renderer>()
                };
            }

            // Faint glow from the eyes onto nearby walls (no shadows).
            var glowGo = new GameObject("EyeGlow");
            glowGo.transform.SetParent(head, false);
            glowGo.transform.position = root.transform.TransformPoint(new Vector3(0f, 1.95f, 0.25f));
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 1.6f;
            glow.intensity = 0.6f;
            glow.color = new Color(0.75f, 0.8f, 0.9f);
            glow.shadows = LightShadows.None;

            ParticleSystem smoke = EchoSmoke(root.transform);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.9f;
            agent.speed = 1.3f;
            agent.acceleration = 10f;
            agent.angularSpeed = 320f;
            agent.stoppingDistance = 0.2f;

            var voice = root.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 1.5f;
            voice.maxDistance = 22f;

            var ai = root.AddComponent<EchoAIController>();
            Set(ai, "eye", eye);
            Set(ai, "voice", voice);
            if (animator != null) Set(ai, "animator", animator);

            var catchSequence = root.AddComponent<EchoCatchSequence>();
            Set(catchSequence, "head", head);
            Set(catchSequence, "voice", voice);
            if (animator != null && animator.isHuman)
            {
                SetArray(catchSequence, "leftArm", new Object[] { animator.GetBoneTransform(HumanBodyBones.LeftUpperArm), animator.GetBoneTransform(HumanBodyBones.LeftLowerArm), animator.GetBoneTransform(HumanBodyBones.LeftHand) });
                SetArray(catchSequence, "rightArm", new Object[] { animator.GetBoneTransform(HumanBodyBones.RightUpperArm), animator.GetBoneTransform(HumanBodyBones.RightLowerArm), animator.GetBoneTransform(HumanBodyBones.RightHand) });
            }
            else if (animator != null)
            {
                Transform b = animator.transform;
                SetArray(catchSequence, "leftArm", new Object[] { FindDeep(b, "DEF-upper_arm.L"), FindDeep(b, "DEF-forearm.L"), FindDeep(b, "DEF-hand.L") });
                SetArray(catchSequence, "rightArm", new Object[] { FindDeep(b, "DEF-upper_arm.R"), FindDeep(b, "DEF-forearm.R"), FindDeep(b, "DEF-hand.R") });
            }

            var presence = root.AddComponent<EchoPresence>();
            Set(presence, "ai", ai);
            Set(presence, "head", head);
            SetArray(presence, "eyes", eyeRenderers);
            Set(presence, "eyeGlow", glow);
            Set(presence, "smoke", smoke);
            SetArray(presence, "spine", spine);
            SetArray(presence, "upperArms", upperArms);
            SetArray(presence, "lowerArms", lowerArms);
            SetArray(presence, "hands", hands);
            // A limping, lurching, dead-armed walk on top of the walk clip (it finds its own humanoid bones).
            var gait = root.AddComponent<MonsterGait>();
            if (animator != null) Set(gait, "animator", animator);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EchoPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static ParticleSystem EchoSmoke(Transform parent)
        {
            var go = new GameObject("Smoke");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
            main.startColor = new Color(0.02f, 0.02f, 0.025f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;
            main.gravityModifier = -0.03f;
            var emission = ps.emission;
            emission.rateOverTime = 9f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.45f, 1.7f, 0.3f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            colour.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            Material m = UnlitMat("EchoSmoke", Color.white, transparent: true);
            Sprite soft = EnsureSprite("SoftDot", SpriteShape.Soft);
            if (soft != null) m.SetTexture("_BaseMap", soft.texture);
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        /// <summary>Imports the Echo model as a generic rig with looping clips (once).</summary>
        private static GameObject EnsureEchoModel()
        {
            var importer = AssetImporter.GetAtPath(EchoModelPath) as ModelImporter;
            if (importer == null) return null;
            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Generic) { importer.animationType = ModelImporterAnimationType.Generic; changed = true; }
            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel) { importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; changed = true; }
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None) { importer.materialImportMode = ModelImporterMaterialImportMode.None; changed = true; }
            if (importer.clipAnimations == null || importer.clipAnimations.Length == 0)
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                foreach (ModelImporterClipAnimation c in clips) c.loopTime = c.name.Contains("_Loop");
                importer.clipAnimations = clips;
                changed = true;
            }
            if (changed) importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelPath);
        }

        private static AnimationClip EchoClip(string name)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(EchoModelPath))
                if (o is AnimationClip c && (c.name == "Rig|" + name || c.name == name)) return c;
            Debug.LogWarning("[GamePrefabs] Echo clip not found: " + name);
            return null;
        }

        /// <summary>
        /// Speed blend (idle, stiff walk, lurching jog, sprint) plus three poses driven by the AI state:
        /// crouched listening, pressing against a hiding place while inspecting it, and the lunge when it catches you.
        /// </summary>
        private static UnityEditor.Animations.AnimatorController EnsureEchoAnimator()
        {
            const string version = "echo-animator-v2";
            var existing = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(EchoAnimatorPath);
            if (existing != null && AssetImporter.GetAtPath(EchoAnimatorPath).userData == version) return existing;
            if (existing != null) AssetDatabase.DeleteAsset(EchoAnimatorPath);
            var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(EchoAnimatorPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("State", AnimatorControllerParameterType.Int);
            UnityEditor.Animations.AnimatorState move = ctrl.CreateBlendTreeInController("Move", out UnityEditor.Animations.BlendTree tree, 0);
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(EchoClip("Idle_Loop"), 0f);
            tree.AddChild(EchoClip("Walk_Formal_Loop"), 1.1f);
            tree.AddChild(EchoClip("Jog_Fwd_Loop"), 2.6f);
            tree.AddChild(EchoClip("Sprint_Loop"), 3.9f);

            UnityEditor.Animations.AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            sm.defaultState = move;
            AddStatePose(sm, move, "Listen", EchoClip("Crouch_Idle_Loop"), (int)EchoAIController.State.Listen, 0.35f);
            AddStatePose(sm, move, "Inspect", EchoClip("Push_Loop"), (int)EchoAIController.State.Inspect, 0.3f);
            // Caught: no punch. It stands over you; EchoCatchSequence drives the grabbing hands and head.
            AddStatePose(sm, move, "Caught", EchoClip("Idle_Loop"), (int)EchoAIController.State.Caught, 0.08f);
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetImporter.GetAtPath(EchoAnimatorPath).userData = version;
            AssetDatabase.WriteImportSettingsIfDirty(EchoAnimatorPath);
            return ctrl;
        }

        private static void AddStatePose(UnityEditor.Animations.AnimatorStateMachine sm, UnityEditor.Animations.AnimatorState move,
            string name, AnimationClip clip, int stateValue, float blend)
        {
            UnityEditor.Animations.AnimatorState s = sm.AddState(name);
            s.motion = clip;
            UnityEditor.Animations.AnimatorStateTransition enter = sm.AddAnyStateTransition(s);
            enter.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, stateValue, "State");
            if (name == "Inspect") enter.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.3f, "Speed");
            enter.duration = blend;
            enter.canTransitionToSelf = false;
            enter.hasExitTime = false;
            UnityEditor.Animations.AnimatorStateTransition exit = s.AddTransition(move);
            exit.AddCondition(UnityEditor.Animations.AnimatorConditionMode.NotEqual, stateValue, "State");
            exit.duration = 0.3f;
            exit.hasExitTime = false;
            if (name == "Inspect")
            {
                UnityEditor.Animations.AnimatorStateTransition walkAway = s.AddTransition(move);
                walkAway.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.5f, "Speed");
                walkAway.duration = 0.3f;
                walkAway.hasExitTime = false;
            }
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                Transform f = FindDeep(c, name);
                if (f != null) return f;
            }
            return null;
        }

        // ================================================================== HUD

        public static GameObject BuildHud()
        {
            EnsureFolder("Assets/Prefabs/UI");
            Sprite circle = EnsureSprite("Circle", SpriteShape.Disc);
            Sprite ring = EnsureSprite("Ring", SpriteShape.Ring);
            Sprite square = EnsureSprite("Square", SpriteShape.Square);
            Sprite vignette = EnsureSprite("Vignette", SpriteShape.Vignette);

            var root = new GameObject("HUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            root.AddComponent<GraphicRaycaster>();
            var hud = root.AddComponent<HUDController>();
            var mobile = root.AddComponent<MobileInputUI>();
            var pause = root.AddComponent<PauseController>();
            var story = root.AddComponent<StoryHUD>();

            // Threat vignette behind everything.
            RectTransform vig = UIRect(root.transform, "ThreatVignette");
            Stretch(vig);
            Image vigImage = UIImage(vig, vignette, new Color(1f, 1f, 1f, 0f), raycast: false);

            RectTransform safe = UIRect(root.transform, "SafeArea");
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // ---------------- touch controls
            RectTransform controls = UIRect(safe, "MobileControls");
            Stretch(controls);
            RectTransform lookRect = UIRect(controls, "LookArea");
            Anchors(lookRect, new Vector2(0.4f, 0f), new Vector2(1f, 1f));
            UIImage(lookRect, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var lookArea = lookRect.gameObject.AddComponent<TouchLookArea>();

            RectTransform stickArea = UIRect(controls, "JoystickArea");
            Anchors(stickArea, new Vector2(0f, 0f), new Vector2(0.4f, 0.75f));
            UIImage(stickArea, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            RectTransform stickBase = UIRect(stickArea, "Base");
            Place(stickBase, Vector2.zero, new Vector2(230f, 230f), new Vector2(240f, 240f));
            Image baseImage = UIImage(stickBase, ring, new Color(1f, 1f, 1f, 0.35f), raycast: false);
            RectTransform knob = UIRect(stickBase, "Knob");
            Place(knob, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
            UIImage(knob, circle, new Color(1f, 1f, 1f, 0.55f), raycast: false);
            var stick = stickArea.gameObject.AddComponent<VirtualJoystick>();
            Set(stick, "baseRect", stickBase);
            Set(stick, "knob", knob);
            Set(stick, "baseGraphic", baseImage);

            var corner = new Vector2(1f, 0f);
            var top = new Vector2(1f, 1f);
            MobileButton use = TouchButton(controls, circle, "Interact", "USE", MobileButton.Kind.Interact, corner, new Vector2(-180f, 180f), 180f, 40);
            MobileButton crouch = TouchButton(controls, circle, "Crouch", "HIDE /\nCROUCH", MobileButton.Kind.Crouch, corner, new Vector2(-385f, 100f), 125f, 20);
            MobileButton sprint = TouchButton(controls, circle, "Sprint", "RUN", MobileButton.Kind.SprintToggle, corner, new Vector2(-350f, 285f), 120f, 28);
            MobileButton light = TouchButton(controls, circle, "Flashlight", "LIGHT", MobileButton.Kind.Flashlight, corner, new Vector2(-170f, 390f), 120f, 26);
            MobileButton bag = TouchButton(controls, circle, "Inventory", "BAG", MobileButton.Kind.Inventory, top, new Vector2(-190f, -80f), 90f, 24);
            MobileButton pauseBtn = TouchButton(controls, circle, "Pause", "II", MobileButton.Kind.Pause, top, new Vector2(-80f, -80f), 90f, 32);
            // Only shown while hidden (MobileInputUI); hold it while The Echo checks the hiding place.
            MobileButton breathBtn = TouchButton(controls, circle, "HoldBreath", "HOLD\nBREATH", MobileButton.Kind.HoldBreath, corner, new Vector2(-620f, 210f), 165f, 26);
            breathBtn.gameObject.SetActive(false);

            // ---------------- HUD elements
            RectTransform cross = UIRect(safe, "Crosshair");
            Place(cross, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            Image crossImage = UIImage(cross, circle, new Color(1f, 1f, 1f, 0.35f), raycast: false);

            TMP_Text prompt = UIText(safe, "Prompt", "", 34, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1000f, 60f));
            prompt.gameObject.SetActive(false);

            TMP_Text message = UIText(safe, "Message", "", 32, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1200f, 60f));
            var messageGroup = message.gameObject.AddComponent<CanvasGroup>();
            messageGroup.alpha = 0f;
            messageGroup.blocksRaycasts = false;

            RectTransform battery = UIRect(safe, "Battery");
            Place(battery, new Vector2(0f, 1f), new Vector2(230f, -60f), new Vector2(180f, 24f));
            UIImage(battery, square, new Color(0f, 0f, 0f, 0.5f), raycast: false);
            RectTransform fillRect = UIRect(battery, "Fill");
            Stretch(fillRect, 3f);
            Image fill = UIImage(fillRect, square, new Color(1f, 0.95f, 0.75f, 0.9f), raycast: false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            UIText(battery, "Label", "LIGHT", 20, new Vector2(0f, 0.5f), new Vector2(-50f, 0f), new Vector2(90f, 30f));
            battery.gameObject.SetActive(false);

            TMP_Text objective = UIText(safe, "Objective", "", 26, new Vector2(0f, 1f), new Vector2(300f, -150f), new Vector2(520f, 120f),
                TextAlignmentOptions.TopLeft, wrap: true);
            var objectiveGroup = objective.gameObject.AddComponent<CanvasGroup>();
            objectiveGroup.alpha = 0.85f;
            objectiveGroup.blocksRaycasts = false;

            TMP_Text hidden = UIText(safe, "HiddenLabel", "HIDDEN\n<size=60%>If it comes close, hold your breath (Space / HOLD BREATH)</size>", 30,
                new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(900f, 90f));
            hidden.color = new Color(0.7f, 0.85f, 1f, 0.9f);

            RectTransform breathMeter = UIRect(safe, "BreathMeter");
            Place(breathMeter, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(320f, 16f));
            UIImage(breathMeter, square, new Color(0f, 0f, 0f, 0.55f), raycast: false);
            RectTransform breathFillRect = UIRect(breathMeter, "Fill");
            Stretch(breathFillRect, 2f);
            Image breathFill = UIImage(breathFillRect, square, new Color(0.75f, 0.9f, 1f, 0.9f), raycast: false);
            breathFill.type = Image.Type.Filled;
            breathFill.fillMethod = Image.FillMethod.Horizontal;
            breathFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            breathMeter.gameObject.SetActive(false);

            RectTransform subtitlePanel = UIRect(safe, "Subtitles");
            Place(subtitlePanel, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1000f, 130f));
            UIImage(subtitlePanel, square, new Color(0f, 0f, 0f, 0.65f), raycast: false);
            TMP_Text subtitleText = UIText(subtitlePanel, "Text", "", 32, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 120f),
                TextAlignmentOptions.Center, wrap: true);

            TMP_Text title = UIText(safe, "LevelTitle", "", 46, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1400f, 200f));
            var titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            titleGroup.alpha = 0f;
            titleGroup.blocksRaycasts = false;

            // Static that tears the screen when The Echo catches you (above the HUD, below the menus).
            RectTransform catchRect = UIRect(root.transform, "CatchStatic");
            Stretch(catchRect);
            var catchStatic = catchRect.gameObject.AddComponent<RawImage>();
            catchStatic.raycastTarget = false;
            catchStatic.color = new Color(1f, 1f, 1f, 0f);
            catchRect.gameObject.SetActive(false);

            // ---------------- modal panels (on top)
            // Note reader: a sheet of aged, stained paper held up in the torch light, handwritten in ink.
            RectTransform notePanel = UIPanel(root.transform, "NotePanel", new Color(0f, 0f, 0f, 0.88f));
            RectTransform paper = UIRect(notePanel, "Paper");
            Place(paper, new Vector2(0.5f, 0.5f), new Vector2(-40f, 50f), new Vector2(650f, 894f));
            paper.localRotation = Quaternion.Euler(0f, 0f, -2.2f);
            Image paperImage = UIImage(paper, PaperArt.PaperSprite() ?? square, Color.white, raycast: true);
            paperImage.preserveAspect = false;
            RectTransform torchSpot = UIRect(paper, "TorchLight");
            Place(torchSpot, new Vector2(0.5f, 0.5f), new Vector2(-60f, 120f), new Vector2(760f, 760f));
            UIImage(torchSpot, EnsureSprite("SoftDot", SpriteShape.Soft), new Color(1f, 0.96f, 0.82f, 0.22f), raycast: false);
            TMP_FontAsset hand = PaperArt.HandwritingFont();
            Color ink = new Color(0.11f, 0.09f, 0.13f);
            TMP_Text noteTitle = UIText(paper, "Title", "", 46, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(560f, 80f));
            noteTitle.color = ink;
            TMP_Text noteBody = UIText(paper, "Body", "", 38, new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(545f, 700f),
                TextAlignmentOptions.TopLeft, wrap: true);
            noteBody.color = ink;
            noteBody.enableAutoSizing = true;
            noteBody.fontSizeMin = 22f;
            noteBody.fontSizeMax = 40f;
            noteBody.lineSpacing = -8f;
            if (hand != null)
            {
                noteTitle.font = hand;
                noteBody.font = hand;
            }
            Button closeNote = UIButton(notePanel, "CloseButton", "CLOSE", new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(320f, 90f), 34, story.CloseNote);
            Set(story, "noteSheet", paper.gameObject);
            Set(story, "notePaper", paperImage);
            Set(story, "noteCloseLabel", closeNote.GetComponentInChildren<TMP_Text>());
            Set(story, "noteBackdrop", notePanel.GetComponent<Image>());
            var sprites = new Object[PaperArt.Styles];
            for (int st = 0; st < sprites.Length; st++) sprites[st] = PaperArt.PaperSprite(st);
            SetArray(story, "paperSprites", sprites);

            RectTransform invPanel = UIPanel(root.transform, "InventoryPanel", new Color(0f, 0f, 0f, 0.75f));
            UIText(invPanel, "Title", "INVENTORY", 48, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(800f, 80f));
            TMP_Text invText = UIText(invPanel, "Items", "", 34, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(800f, 460f),
                TextAlignmentOptions.TopLeft, wrap: true);
            UIButton(invPanel, "CloseButton", "CLOSE", new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(320f, 90f), 34, story.CloseInventory);

            // Keypad (CodeLock): an old mechanical door keypad. Time keeps running while it is open.
            var codeUi = root.AddComponent<CodePanelUI>();
            RectTransform codePanel = UIPanel(root.transform, "CodePanel", new Color(0f, 0f, 0f, 0.6f));
            Sprite disc = EnsureSprite("Circle", SpriteShape.Disc);
            RectTransform frame = UIRect(codePanel, "Frame");
            Place(frame, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(640f, 800f));
            UIImage(frame, square, new Color(0.18f, 0.18f, 0.14f, 1f), raycast: true);
            RectTransform pad = UIRect(frame, "Plate");
            Stretch(pad, 12f);
            UIImage(pad, square, new Color(0.4f, 0.41f, 0.31f, 1f), raycast: true);
            foreach (Vector2 c in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
            {
                RectTransform screw = UIRect(pad, "Screw");
                Place(screw, new Vector2(0.5f, 0.5f), new Vector2(c.x * 280f, c.y * 365f), new Vector2(34f, 34f));
                UIImage(screw, disc, new Color(0.16f, 0.16f, 0.15f, 1f), raycast: false);
                RectTransform slot = UIRect(screw, "Slot");
                Place(slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 5f));
                slot.localRotation = Quaternion.Euler(0f, 0f, 35f + c.x * 20f);
                UIImage(slot, square, new Color(0.05f, 0.05f, 0.05f, 1f), raycast: false);
            }
            TMP_Text codeTitle = UIText(pad, "Title", "KEYPAD", 26, new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(520f, 40f));
            codeTitle.color = new Color(0.12f, 0.12f, 0.08f);
            codeTitle.fontStyle = FontStyles.Bold;
            // Indicator lamps and speaker grille on the left.
            Image Lamp(string n, float y, Color c)
            {
                RectTransform bezel = UIRect(pad, n + "Bezel");
                Place(bezel, new Vector2(0.5f, 0.5f), new Vector2(-195f, y), new Vector2(104f, 92f));
                UIImage(bezel, square, new Color(0.12f, 0.12f, 0.1f, 1f), raycast: false);
                RectTransform lamp = UIRect(bezel, n);
                Stretch(lamp, 8f);
                return UIImage(lamp, square, c, raycast: false);
            }
            Image green = Lamp("GreenLamp", 190f, new Color(0.12f, 0.3f, 0.14f, 1f));
            Image red = Lamp("RedLamp", 70f, new Color(0.35f, 0.08f, 0.07f, 1f));
            for (int gy = 0; gy < 4; gy++)
                for (int gx = 0; gx < 3; gx++)
                {
                    RectTransform hole = UIRect(pad, "Grille");
                    Place(hole, new Vector2(0.5f, 0.5f), new Vector2(-225f + gx * 30f, -140f - gy * 30f), new Vector2(18f, 18f));
                    UIImage(hole, disc, new Color(0.1f, 0.1f, 0.09f, 1f), raycast: false);
                }
            // Counter window showing the digits.
            RectTransform window = UIRect(pad, "Window");
            Place(window, new Vector2(0.5f, 0.5f), new Vector2(85f, 255f), new Vector2(340f, 86f));
            UIImage(window, square, new Color(0.03f, 0.03f, 0.03f, 1f), raycast: false);
            TMP_Text codeDisplay = UIText(window, "Digits", "_ _ _ _", 56, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 80f));
            codeDisplay.color = new Color(0.9f, 0.88f, 0.8f);
            codeDisplay.fontStyle = FontStyles.Bold;
            // Recessed key matrix.
            RectTransform matrix = UIRect(pad, "Matrix");
            Place(matrix, new Vector2(0.5f, 0.5f), new Vector2(85f, -55f), new Vector2(340f, 460f));
            UIImage(matrix, square, new Color(0.07f, 0.07f, 0.065f, 1f), raycast: false);
            string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "CLR", "0", "OK" };
            for (int k = 0; k < keys.Length; k++)
            {
                int col = k % 3, row = k / 3;
                Vector2 pos = new Vector2(-108f + col * 108f, 165f - row * 110f);
                RectTransform bevel = UIRect(matrix, "Bevel_" + keys[k]);
                Place(bevel, new Vector2(0.5f, 0.5f), pos + new Vector2(0f, 2f), new Vector2(98f, 100f));
                UIImage(bevel, square, new Color(0.32f, 0.32f, 0.3f, 1f), raycast: false);
                RectTransform key = UIRect(matrix, "Key_" + keys[k]);
                Place(key, new Vector2(0.5f, 0.5f), pos - new Vector2(0f, 3f), new Vector2(94f, 94f));
                Image face = UIImage(key, square, new Color(0.06f, 0.06f, 0.06f, 1f), raycast: true);
                var b = key.gameObject.AddComponent<Button>();
                b.targetGraphic = face;
                ColorBlock cb = b.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = new Color(1.6f, 1.6f, 1.6f);
                cb.pressedColor = new Color(3f, 3f, 3f);
                cb.selectedColor = Color.white;
                b.colors = cb;
                if (keys[k] == "CLR" || keys[k] == "OK")
                {
                    // Draw the cross and the tick as white strokes (the font has no such glyphs).
                    bool tick = keys[k] == "OK";
                    (Vector2 at, Vector2 size, float rot)[] strokes = tick
                        ? new[] { (new Vector2(-12f, -6f), new Vector2(30f, 9f), -45f), (new Vector2(9f, 4f), new Vector2(52f, 9f), 52f) }
                        : new[] { (Vector2.zero, new Vector2(54f, 9f), 45f), (Vector2.zero, new Vector2(54f, 9f), -45f) };
                    foreach (var st in strokes)
                    {
                        RectTransform line = UIRect(key, "Stroke");
                        Place(line, new Vector2(0.5f, 0.5f), st.at, st.size);
                        line.localRotation = Quaternion.Euler(0f, 0f, st.rot);
                        UIImage(line, square, new Color(0.93f, 0.92f, 0.88f, 1f), raycast: false);
                    }
                    if (tick) UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, codeUi.Submit);
                    else UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, codeUi.Clear);
                }
                else
                {
                    TMP_Text label = UIText(key, "Label", keys[k], 58, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(94f, 94f));
                    label.color = new Color(0.95f, 0.94f, 0.9f);
                    label.fontStyle = FontStyles.Bold;
                    UnityEditor.Events.UnityEventTools.AddIntPersistentListener(b.onClick, codeUi.PressDigit, int.Parse(keys[k]));
                }
            }
            UIButton(codePanel, "CloseButton", "STEP AWAY", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(360f, 80f), 30, codeUi.Close);
            Set(codeUi, "panel", codePanel.gameObject);
            Set(codeUi, "titleText", codeTitle);
            Set(codeUi, "displayText", codeDisplay);
            Set(codeUi, "greenLamp", green);
            Set(codeUi, "redLamp", red);
            codePanel.gameObject.SetActive(false);

            RectTransform pausePanel = UIPanel(root.transform, "PausePanel", new Color(0f, 0f, 0f, 0.82f));
            RectTransform mainPage = UIRect(pausePanel, "MainPage");
            Stretch(mainPage);
            UIText(mainPage, "Title", "PAUSED", 72, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(800f, 100f));
            UIButton(mainPage, "Resume", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(560f, 100f), 38, pause.Resume);
            UIButton(mainPage, "Restart", "RESTART CHECKPOINT", new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(560f, 100f), 34, pause.RestartCheckpoint);
            UIButton(mainPage, "Settings", "SETTINGS", new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(560f, 100f), 38, pause.OpenSettings);
            UIButton(mainPage, "MainMenu", "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(560f, 100f), 38, pause.QuitToMainMenu);
            RectTransform settingsPage = SettingsPage(pausePanel, pause.ShowMain);

            RectTransform gameOver = UIPanel(root.transform, "GameOverPanel", new Color(0.08f, 0f, 0f, 0.88f));
            UIText(gameOver, "Title", "THE ECHO FOUND YOU", 64, new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1400f, 100f));
            UIText(gameOver, "Hint", "Listen for its footsteps. Crouch to stay quiet, hide when it gets close - and hold your breath if it checks your hiding place.", 30,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1400f, 60f));
            UIButton(gameOver, "Retry", "RETRY CHECKPOINT", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(560f, 100f), 36, story.RetryCheckpoint);
            UIButton(gameOver, "MainMenu", "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(560f, 100f), 36, story.QuitToMenu);

            // ---------------- wiring
            Set(hud, "crosshair", crossImage);
            Set(hud, "promptText", prompt);
            Set(hud, "messageText", message);
            Set(hud, "messageGroup", messageGroup);
            Set(hud, "batteryRoot", battery.gameObject);
            Set(hud, "batteryFill", fill);
            Set(mobile, "controlsRoot", controls.gameObject);
            Set(mobile, "joystick", stick);
            Set(mobile, "lookArea", lookArea);
            SetArray(mobile, "buttons", new Object[] { use, crouch, sprint, light, bag, pauseBtn, breathBtn });
            Set(pause, "pausePanel", pausePanel.gameObject);
            Set(pause, "mainPage", mainPage.gameObject);
            Set(pause, "settingsPage", settingsPage.gameObject);
            Set(story, "objectiveText", objective);
            Set(story, "objectiveGroup", objectiveGroup);
            Set(story, "subtitlePanel", subtitlePanel.gameObject);
            Set(story, "subtitleText", subtitleText);
            Set(story, "notePanel", notePanel.gameObject);
            Set(story, "noteTitle", noteTitle);
            Set(story, "noteBody", noteBody);
            Set(story, "inventoryPanel", invPanel.gameObject);
            Set(story, "inventoryText", invText);
            Set(story, "gameOverPanel", gameOver.gameObject);
            Set(story, "titleGroup", titleGroup);
            Set(story, "titleText", title);
            Set(story, "hiddenLabel", hidden.gameObject);
            Set(story, "threatVignette", vigImage);
            Set(story, "breathRoot", breathMeter.gameObject);
            Set(story, "breathFill", breathFill);
            Set(story, "catchOverlay", catchStatic);

            notePanel.gameObject.SetActive(false);
            invPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            gameOver.gameObject.SetActive(false);
            subtitlePanel.gameObject.SetActive(false);
            hidden.gameObject.SetActive(false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static MobileButton TouchButton(Transform parent, Sprite sprite, string name, string label,
            MobileButton.Kind kind, Vector2 anchor, Vector2 position, float size, float fontSize)
        {
            RectTransform r = UIRect(parent, name + "Button");
            Place(r, anchor, position, new Vector2(size, size));
            Image bg = UIImage(r, sprite, new Color(1f, 1f, 1f, 0.25f), raycast: true);
            UIText(r, "Label", label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var b = r.gameObject.AddComponent<MobileButton>();
            SetValue(b, "kind", kind);
            Set(b, "background", bg);
            return b;
        }

        /// <summary>Settings page used by the pause menu and the main menu.</summary>
        public static RectTransform SettingsPage(Transform parent, UnityAction back)
        {
            RectTransform page = UIRect(parent, "SettingsPage");
            Stretch(page);
            var ui = page.gameObject.AddComponent<SettingsPanelUI>();
            var c = new Vector2(0.5f, 0.5f);
            UIText(page, "Title", "SETTINGS", 60, c, new Vector2(0f, 400f), new Vector2(800f, 80f));
            Button quality = UIButton(page, "Quality", "GRAPHICS", c, new Vector2(0f, 300f), new Vector2(640f, 80f), 32, ui.CycleQuality);
            Slider sens = LabeledSlider(page, "Sensitivity", "LOOK SENSITIVITY", 200f);
            Slider master = LabeledSlider(page, "Master", "MASTER VOLUME", 90f);
            Slider ambience = LabeledSlider(page, "Ambience", "AMBIENCE VOLUME", -20f);
            Slider sfx = LabeledSlider(page, "Sfx", "EFFECTS VOLUME", -130f);
            Button subs = UIButton(page, "Subtitles", "SUBTITLES", c, new Vector2(0f, -240f), new Vector2(640f, 80f), 30, ui.ToggleSubtitles);
            UIText(page, "SubsNote", "Story dialogue and radio messages are always subtitled.", 24, c, new Vector2(0f, -300f), new Vector2(900f, 40f));
            UIButton(page, "Back", "BACK", c, new Vector2(0f, -390f), new Vector2(360f, 90f), 36, back);
            Set(ui, "qualityLabel", quality.GetComponentInChildren<TMP_Text>());
            Set(ui, "subtitlesLabel", subs.GetComponentInChildren<TMP_Text>());
            Set(ui, "sensitivity", sens);
            Set(ui, "masterVolume", master);
            Set(ui, "ambienceVolume", ambience);
            Set(ui, "sfxVolume", sfx);
            page.gameObject.SetActive(false);
            return page;
        }

        private static Slider LabeledSlider(Transform page, string name, string label, float y)
        {
            var c = new Vector2(0.5f, 0.5f);
            UIText(page, name + "Label", label, 28, c, new Vector2(0f, y + 30f), new Vector2(640f, 40f));
            return UISlider(page, name + "Slider", c, new Vector2(0f, y - 15f), new Vector2(640f, 40f));
        }
    }
}
