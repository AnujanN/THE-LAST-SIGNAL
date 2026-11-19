using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// Alex's visible first-person body: legs that walk, run and crouch, a left arm that raises the
    /// torch while it is on, and a hand that physically reaches to the exact point being used
    /// (two-bone IK), so taking, pressing, placing and pushing all show a real hand doing it.
    /// The head is hidden (the camera is the eyes) and the body is anchored so the neck always sits
    /// just behind the camera: the torso never blocks the view, the arms swing into it.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class PlayerBody : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerLook look;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private FlashlightController flashlight;

        [Header("Bones")]
        [SerializeField] private Transform head;
        [Tooltip("Spine bones that share the lean from the view pitch.")]
        [SerializeField] private Transform[] spine = new Transform[0];
        [SerializeField, Range(0f, 1f)] private float pitchLean = 0.3f;
        [Tooltip("Extra lean towards what the hand is reaching for.")]
        [SerializeField] private float reachLeanDegrees = 12f;
        [Tooltip("Right arm: upper arm, forearm, hand.")]
        [SerializeField] private Transform[] rightArm = new Transform[3];
        [Tooltip("Left arm: upper arm, forearm, hand (used for two-handed pushes when the torch is off).")]
        [SerializeField] private Transform[] leftArm = new Transform[3];

        [Header("First-person anchoring")]
        [Tooltip("The camera (eyes). The body is shifted every frame so its neck stays just behind it.")]
        [SerializeField] private Transform eyes;
        [Tooltip("How far behind the eyes the head bone is kept (horizontal metres).")]
        [SerializeField] private float neckBehindEyes = 0.1f;

        [Header("Reach")]
        [SerializeField] private float reachSeconds = 0.75f;
        [Tooltip("Hand stops this far short of the surface it touches.")]
        [SerializeField] private float touchOffset = 0.04f;
        [Tooltip("Wrist-to-palm distance: the wrist stops this far short so the palm touches.")]
        [SerializeField] private float palmLength = 0.07f;
        [Tooltip("Most the chest leans (degrees) to bring a slightly-too-far target within reach.")]
        [SerializeField] private float maxReachLean = 30f;

        [Header("Props")]
        [SerializeField] private GameObject torchModel;
        [SerializeField] private GameObject torchLens;
        [Tooltip("If set, the torch hangs here (in front of the view) while on and the left hand is pulled to it by IK.")]
        [SerializeField] private Transform torchGrip;
        [Tooltip("Dim light that only affects the body's rendering layer (the torch beam does not light the body).")]
        [SerializeField] private Light bodyFill;
        [SerializeField] private float fillWhenTorchOff = 0.25f;
        [SerializeField] private float fillWhenTorchOn = 0.7f;

        [Header("Animator layers")]
        [SerializeField] private int torchLayer = 1;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int CrouchHash = Animator.StringToHash("Crouch");
        private static readonly int TorchHash = Animator.StringToHash("Torch");
        private bool hasTorchParam;

        private float torchWeight;
        private Transform holdGrip;
        private float holdWeight;

        // Fingers (humanoid): index, middle, ring, little proximal + intermediate, per hand.
        private Transform[] leftFingers = new Transform[0], rightFingers = new Transform[0];
        // Every finger joint (index, middle, ring, little x 3, then thumb x 3) and the axis, in its parent's space,
        // that curls it towards the palm (measured from the rig's own muscles).
        private Transform[] leftJoints = new Transform[0], rightJoints = new Transform[0];
        private Vector3[] leftAxes = new Vector3[0], rightAxes = new Vector3[0];
        // Degrees of curl per joint level (proximal, intermediate, distal) for a fist and for holding a sheet.
        private static readonly float[] FistCurl = { 70f, 95f, 60f };
        private static readonly float[] ThumbFist = { 20f, 35f, 30f };
        private static readonly float[] SheetCurl = { 18f, 22f, 12f };
        private const int Index1 = 0, Middle1 = 1, Ring1 = 2, Little1 = 3, Index2 = 4, Middle2 = 5, Ring2 = 6, Little2 = 7;

        /// <summary>
        /// 0 = the right hand just reaches for the grip point (picking a sheet up off a table),
        /// 1 = it holds the sheet properly from behind, fingers up the back, writing left clear.
        /// </summary>
        public float HoldOrient { get; set; }
        private float reachTimer;
        private Vector3 reachTarget;
        private bool twoHanded;

        /// <summary>What the last interaction looked like: "Grab", "Use" or "Push" (tests, debugging).</summary>
        public string LastAction { get; private set; } = "";
        /// <summary>0..1 how far into the current reach the hand is.</summary>
        public float ReachWeight { get; private set; }
        public Vector3 ReachTarget => reachTarget;
        public Transform RightHand => rightArm.Length > 2 ? rightArm[2] : null;
        /// <summary>0..1 how firmly the right hand is holding something up (a note).</summary>
        public float HoldWeight => holdWeight;

        /// <summary>
        /// Hold something up in the right hand (a note being read), or let go (null). Reading pauses the game,
        /// so while holding, the animator runs on unscaled time and the IK keeps working.
        /// </summary>
        public void HoldWith(Transform grip)
        {
            holdGrip = grip;
            if (animator != null) animator.updateMode = grip != null ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<PlayerController>();
            if (look == null) look = GetComponent<PlayerLook>();
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (flashlight == null) flashlight = GetComponentInChildren<FlashlightController>();
            SetUpHands();
        }


        private void SetUpHands()
        {
            if (animator == null || !animator.isHuman || animator.avatar == null) return;
            Transform B(HumanBodyBones b) => animator.GetBoneTransform(b);
            leftFingers = new[]
            {
                B(HumanBodyBones.LeftIndexProximal), B(HumanBodyBones.LeftMiddleProximal), B(HumanBodyBones.LeftRingProximal), B(HumanBodyBones.LeftLittleProximal),
                B(HumanBodyBones.LeftIndexIntermediate), B(HumanBodyBones.LeftMiddleIntermediate), B(HumanBodyBones.LeftRingIntermediate), B(HumanBodyBones.LeftLittleIntermediate),
            };
            rightFingers = new[]
            {
                B(HumanBodyBones.RightIndexProximal), B(HumanBodyBones.RightMiddleProximal), B(HumanBodyBones.RightRingProximal), B(HumanBodyBones.RightLittleProximal),
                B(HumanBodyBones.RightIndexIntermediate), B(HumanBodyBones.RightMiddleIntermediate), B(HumanBodyBones.RightRingIntermediate), B(HumanBodyBones.RightLittleIntermediate),
            };
            foreach (Transform t in leftFingers) if (t == null) { leftFingers = new Transform[0]; break; }
            foreach (Transform t in rightFingers) if (t == null) { rightFingers = new Transform[0]; break; }

            MeasureCurlAxes("Left", out leftJoints, out leftAxes);
            MeasureCurlAxes("Right", out rightJoints, out rightAxes);
        }

        /// <summary>
        /// Which way each finger joint bends towards the palm, measured once by moving the rig's own finger
        /// muscles between open and closed (works on any humanoid rig, whatever its bone axes).
        /// </summary>
        private void MeasureCurlAxes(string side, out Transform[] joints, out Vector3[] axes)
        {
            string[] names = { "Index", "Middle", "Ring", "Little", "Thumb" };
            string[] boneSuffix = { "Proximal", "Intermediate", "Distal" };
            var j = new System.Collections.Generic.List<Transform>();
            var ax = new System.Collections.Generic.List<Vector3>();
            using (var handler = new HumanPoseHandler(animator.avatar, animator.transform))
            {
                var pose = new HumanPose();
                handler.GetHumanPose(ref pose);
                float[] original = (float[])pose.muscles.Clone();
                foreach (string f in names)
                    for (int level = 0; level < 3; level++)
                    {
                        var bone = (HumanBodyBones)System.Enum.Parse(typeof(HumanBodyBones), side + f + boneSuffix[level]);
                        Transform t = animator.GetBoneTransform(bone);
                        int m = System.Array.IndexOf(HumanTrait.MuscleName, side + " " + f + " " + (level + 1) + " Stretched");
                        if (t == null || m < 0) { joints = new Transform[0]; axes = new Vector3[0]; return; }
                        pose.muscles[m] = 1f;
                        handler.SetHumanPose(ref pose);
                        Quaternion open = t.localRotation;
                        pose.muscles[m] = -1f;
                        handler.SetHumanPose(ref pose);
                        Quaternion closed = t.localRotation;
                        pose.muscles[m] = original[m];
                        (closed * Quaternion.Inverse(open)).ToAngleAxis(out float angle, out Vector3 axis);
                        if (angle > 180f) { angle = 360f - angle; axis = -axis; }
                        j.Add(t);
                        ax.Add(angle > 0.5f ? axis.normalized : Vector3.zero);
                    }
                handler.SetHumanPose(ref pose);
            }
            joints = j.ToArray();
            axes = ax.ToArray();
        }

        private void OnEnable()
        {
            if (interactor != null) interactor.Interacted += OnInteracted;
            hasTorchParam = false;
            if (animator != null)
                foreach (AnimatorControllerParameter p in animator.parameters)
                    if (p.nameHash == TorchHash) hasTorchParam = true;
        }

        private void OnDisable()
        {
            if (interactor != null) interactor.Interacted -= OnInteracted;
        }

        private void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            float dt = Time.deltaTime;
            float speed = controller != null ? controller.HorizontalSpeed : 0f;
            animator.SetFloat(SpeedHash, speed, 0.12f, dt);
            animator.SetBool(CrouchHash, controller != null && controller.IsCrouching);

            bool torchOn = flashlight != null && flashlight.IsOn;
            torchWeight = Mathf.MoveTowards(torchWeight, torchOn ? 1f : 0f, dt * 4f);
            if (torchLayer < animator.layerCount) animator.SetLayerWeight(torchLayer, torchWeight);
            if (bodyFill != null) bodyFill.intensity = Mathf.Lerp(fillWhenTorchOff, fillWhenTorchOn, torchWeight);
            if (hasTorchParam) animator.SetBool(TorchHash, torchOn);

            if (torchModel != null)
            {
                bool owned = flashlight != null && flashlight.IsOwned;
                // A torch held at the grip is only out while it is on; one in the hand is always carried.
                bool show = torchGrip != null ? owned && torchWeight > 0.05f : owned;
                if (torchModel.activeSelf != show) torchModel.SetActive(show);
            }
            if (torchLens != null && torchLens.activeSelf != torchOn) torchLens.SetActive(torchOn);

            if (reachTimer > 0f) reachTimer -= dt;
            // Fast reach out, brief contact, slower return.
            float t = 1f - Mathf.Clamp01(reachTimer / reachSeconds);
            ReachWeight = reachTimer <= 0f ? 0f : t < 0.35f ? Mathf.SmoothStep(0f, 1f, t / 0.35f)
                : t < 0.6f ? 1f : Mathf.SmoothStep(1f, 0f, (t - 0.6f) / 0.4f);
        }

        private void LateUpdate()
        {
            // Paused (no game time): the animator does not rewrite the pose, so the bones still hold last
            // frame's adjusted pose. Adjusting again would pile the bends up; keep the pose as it is.
            bool paused = Time.deltaTime <= 0f;
            if ((paused && holdGrip == null && holdWeight <= 0f) || animator == null || !animator.isActiveAndEnabled) return;
            float dt = paused ? Time.unscaledDeltaTime : Time.deltaTime;
            holdWeight = Mathf.MoveTowards(holdWeight, holdGrip != null ? 1f : 0f, dt * 3f);

            // Fingers first (this rewrites the pose from muscles): a fist round the torch, a light grip on a sheet.
            bool torchHeld = torchGrip != null && torchWeight > 0f && !(twoHanded && ReachWeight > 0f);
            CurlFingers(torchHeld ? torchWeight : 0f, holdGrip != null ? holdWeight * HoldOrient : 0f);

            // The camera is the eyes: never render our own head from the inside.
            if (head != null) head.localScale = Vector3.zero;
            Transform bodyRoot = animator != null ? animator.transform : null;
            if (bodyRoot != null) bodyRoot.localPosition = Vector3.zero;

            if (look != null && spine.Length > 0)
            {
                float lean = (Mathf.Clamp(look.Pitch, -15f, 70f) * pitchLean + reachLeanDegrees * ReachWeight) / spine.Length;
                Quaternion bend = Quaternion.AngleAxis(lean, transform.right);
                foreach (Transform b in spine)
                    if (b != null) b.rotation = bend * b.rotation;
            }

            // Head-anchored body: whatever the animation or lean does, keep the neck just behind the
            // eyes so the torso never ends up in front of the camera.
            if (bodyRoot != null && head != null && eyes != null)
            {
                Vector3 target = eyes.position - transform.forward * neckBehindEyes;
                Vector3 delta = target - head.position;
                delta.y = 0f;
                bodyRoot.position += delta;
            }

            // Torch up in the left hand (unless both hands are pushing a door): the fist closes round its handle.
            if (torchHeld) GripTorch(torchWeight);

            if (ReachWeight > 0f)
            {
                Vector3 side = transform.right * 0.11f;
                Reach(rightArm, twoHanded ? reachTarget + side : reachTarget, transform.right, ReachWeight);
                if (twoHanded) Reach(leftArm, reachTarget - side, -transform.right, ReachWeight);
            }

            // Holding a note up to read: the right hand holds its lower edge from behind.
            if (holdGrip != null && holdWeight > 0f) GripSheet(holdWeight);
        }

        private void CurlFingers(float leftFist, float rightGrip)
        {
            if (leftFist > 0f) Curl(leftJoints, leftAxes, FistCurl, ThumbFist, leftFist);
            if (rightGrip > 0f) Curl(rightJoints, rightAxes, SheetCurl, null, rightGrip);
        }

        /// <summary>Bend each joint towards the palm on top of the animated pose (fingers 0-11, thumb 12-14).</summary>
        private static void Curl(Transform[] joints, Vector3[] axes, float[] fingers, float[] thumb, float weight)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                bool isThumb = i >= 12;
                if (isThumb && thumb == null) continue;
                float deg = (isThumb ? thumb[i - 12] : fingers[i % 3]) * weight;
                if (axes[i] != Vector3.zero) joints[i].localRotation = Quaternion.AngleAxis(deg, axes[i]) * joints[i].localRotation;
            }
        }

        /// <summary>The torch handle runs through the fist: knuckles along it, index finger towards the lens.</summary>
        private void GripTorch(float weight)
        {
            Vector3 axis = torchGrip.forward;
            Vector3 handle = torchGrip.position + axis * 0.02f;
            GripAt(leftArm, leftFingers, -transform.right, handle, axis, null, FistCentre, weight);
        }

        /// <summary>
        /// A sheet held at its lower edge from behind: fingers up the back of the page, palm towards the eyes,
        /// so the writing stays clear. While HoldOrient is 0 the hand only reaches for the grip point.
        /// </summary>
        private void GripSheet(float weight)
        {
            Vector3 sheetUp = holdGrip.forward, sheetRight = holdGrip.right;
            if (HoldOrient <= 0.01f || rightFingers.Length == 0)
            {
                Reach(rightArm, holdGrip.position, transform.right, weight);
                return;
            }
            // Fingers point up the page, leaning in a little; knuckle line (little -> index) to Alex's right.
            Vector3 fingers = (sheetUp * 0.95f - sheetRight * 0.3f).normalized;
            GripAt(rightArm, rightFingers, transform.right, holdGrip.position, sheetRight, fingers, MiddleFinger, weight, HoldOrient);
        }

        private Vector3 FistCentre(Transform[] f) => (f[Index2].position + f[Middle2].position + f[Ring2].position + f[Little2].position) * 0.25f;
        private Vector3 MiddleFinger(Transform[] f) => f[Middle2].position;

        /// <summary>
        /// Puts a hand round something: reach for it, turn the hand so its knuckle line runs along
        /// <paramref name="knuckles"/> (and the fingers along <paramref name="fingersDir"/> if given, otherwise straight on
        /// from the forearm), then correct the reach so the grasp point lands exactly on <paramref name="target"/>.
        /// </summary>
        private void GripAt(Transform[] arm, Transform[] fingers, Vector3 outward, Vector3 target, Vector3 knuckles,
            Vector3? fingersDir, System.Func<Transform[], Vector3> grasp, float weight, float orient = 1f)
        {
            if (arm == null || arm.Length < 3 || arm[0] == null || arm[1] == null || arm[2] == null) return;
            Reach(arm, target, outward, weight);
            if (fingers.Length < 8) return;
            Transform hand = arm[2];
            Vector3 f = fingers[Middle1].position - hand.position;
            Vector3 k = fingers[Index1].position - fingers[Little1].position;
            if (f.sqrMagnitude < 1e-8f || k.sqrMagnitude < 1e-8f) return;
            Vector3 fd = fingersDir ?? Vector3.ProjectOnPlane(hand.position - arm[1].position, knuckles);
            if (fd.sqrMagnitude < 1e-6f) fd = Vector3.ProjectOnPlane(-transform.up, knuckles);
            // Rotation taking the hand's current (fingers, knuckles) frame onto the wanted one.
            Quaternion now = Quaternion.LookRotation(f, k);
            Quaternion want = Quaternion.LookRotation(fd, knuckles);
            Quaternion turn = Quaternion.Slerp(Quaternion.identity, want * Quaternion.Inverse(now), weight * orient);
            Quaternion handRot = turn * hand.rotation;
            hand.rotation = handRot;
            // Second pass: move the wrist so the grasp point (fist centre, finger) sits on the target.
            Vector3 error = (target - grasp(fingers)) * weight;
            Vector3 hint = arm[1].position + outward * 0.3f - transform.up * 0.3f;
            SolveTwoBone(arm[0], arm[1], arm[2], hand.position + error, hint, 1f);
            hand.rotation = handRot;
        }

        private void Reach(Transform[] arm, Vector3 target, Vector3 outward, float weight)
        {
            if (arm == null || arm.Length < 3 || arm[0] == null || arm[1] == null || arm[2] == null) return;
            // The palm, not the wrist, meets the target.
            target -= (target - arm[0].position).normalized * palmLength;
            // Just out of reach: lean the chest towards it, as a person would.
            Transform chest = spine.Length > 0 ? spine[spine.Length - 1] : null;
            if (chest != null && chest != arm[0])
            {
                float armLength = (arm[1].position - arm[0].position).magnitude + (arm[2].position - arm[1].position).magnitude;
                Vector3 toTarget = target - arm[0].position;
                Vector3 lever = arm[0].position - chest.position;
                float deficit = toTarget.magnitude - armLength * 0.97f;
                Vector3 axis = Vector3.Cross(lever, toTarget);
                if (deficit > 0f && lever.magnitude > 0.05f && axis.sqrMagnitude > 1e-6f)
                {
                    float angle = Mathf.Min(deficit / lever.magnitude * Mathf.Rad2Deg, maxReachLean) * weight;
                    chest.rotation = Quaternion.AngleAxis(angle, axis.normalized) * chest.rotation;
                }
            }
            // Elbow hint: out to the side and down, like a natural reach.
            Vector3 hint = arm[1].position + outward * 0.3f - transform.up * 0.3f;
            SolveTwoBone(arm[0], arm[1], arm[2], target, hint, weight);
        }

        private void OnInteracted(IInteractable target)
        {
            // Inside a hiding place there is nothing to reach for (entering/leaving is instant).
            if (target is HidingSpot) { LastAction = "Push"; return; }

            // A sheet to read: the note holder drives the hand (reach, pick up, hold).
            if (target is NoteInteractable n && n.CanHold) { LastAction = "Grab"; return; }

            Vector3 point = interactor != null ? interactor.TargetPoint : transform.position + transform.forward;
            if (eyes != null) point -= (point - eyes.position).normalized * touchOffset;
            reachTarget = point;
            reachTimer = reachSeconds;
            bool torchOn = flashlight != null && flashlight.IsOn;
            twoHanded = target is Door && !torchOn;
            LastAction = target is PickupItem || target is BatteryPickup || target is NoteInteractable ? "Grab"
                : target is Door ? "Push" : "Use";
        }

        /// <summary>Analytic two-bone IK (shoulder, elbow, wrist) with an elbow hint.</summary>
        public static void SolveTwoBone(Transform a, Transform b, Transform c, Vector3 target, Vector3 hint, float weight)
        {
            Vector3 aPos = a.position, bPos = b.position, cPos = c.position;
            Vector3 tPos = Vector3.Lerp(cPos, target, weight);
            Vector3 ab = bPos - aPos, bc = cPos - bPos, ac = cPos - aPos, at = tPos - aPos;
            float abLen = ab.magnitude, bcLen = bc.magnitude, acLen = ac.magnitude, atLen = at.magnitude;
            if (abLen < 1e-4f || bcLen < 1e-4f || atLen < 1e-4f) return;

            float oldAngle = TriangleAngle(acLen, abLen, bcLen);
            float newAngle = TriangleAngle(atLen, abLen, bcLen);
            Vector3 axis = Vector3.Cross(ab, bc);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.Cross(hint - aPos, bc);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.Cross(at, bc);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.up;
            axis.Normalize();
            float half = 0.5f * (oldAngle - newAngle);
            float sin = Mathf.Sin(half), cos = Mathf.Cos(half);
            b.rotation = new Quaternion(axis.x * sin, axis.y * sin, axis.z * sin, cos) * b.rotation;

            cPos = c.position;
            ac = cPos - aPos;
            a.rotation = Quaternion.FromToRotation(ac, at) * a.rotation;

            // Swing the elbow towards the hint around the shoulder-wrist axis.
            bPos = b.position;
            cPos = c.position;
            ab = bPos - aPos;
            ac = cPos - aPos;
            float acSqr = ac.sqrMagnitude;
            if (acSqr < 1e-8f) return;
            Vector3 acNorm = ac / Mathf.Sqrt(acSqr);
            Vector3 ah = hint - aPos;
            Vector3 abProj = ab - acNorm * Vector3.Dot(ab, acNorm);
            Vector3 ahProj = ah - acNorm * Vector3.Dot(ah, acNorm);
            float maxReach = abLen + bcLen;
            if (abProj.sqrMagnitude > maxReach * maxReach * 0.001f && ahProj.sqrMagnitude > 1e-8f)
                a.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(abProj, ahProj), weight) * a.rotation;
        }

        private static float TriangleAngle(float opposite, float side1, float side2)
        {
            float c = Mathf.Clamp((side1 * side1 + side2 * side2 - opposite * opposite) / (side1 * side2) / 2f, -1f, 1f);
            return Mathf.Acos(c);
        }
    }
}
