using UnityEngine;
using UnityEngine.AI;

namespace LastSignal.Enemy
{
    /// <summary>
    /// Turns the Echo's plain walk into something that is not a person walking. Applied on top of the walk
    /// animation every frame (after EchoPresence's hunch), and faded out as it breaks into its zombie run:
    /// - a limp: the bad (right) leg swings through slowly and the good one hurries, so the rhythm is uneven;
    /// - the bad leg twisted out with its foot pointed down, so the toes drag;
    /// - a heavy lurch: the body rolls over onto whichever leg is planted and dips on the bad one;
    /// - the long arms hang dead in front of it and swing loosely, a beat behind the body;
    /// - the right shoulder sags and the head lolls with the lurch.
    /// Purely visual; it never changes where the AI goes or what it senses.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class MonsterGait : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Header("Limp")]
        [Tooltip("Animation speed while the bad leg swings through (slower) and while the good leg does (faster).")]
        [SerializeField] private float badLegSwingSpeed = 0.72f;
        [SerializeField] private float goodLegSwingSpeed = 1.32f;
        [SerializeField] private float dragLegOutDegrees = 14f;
        [SerializeField] private float dragFootDownDegrees = 28f;

        [Header("Lurch")]
        [SerializeField] private float lurchRollDegrees = 11f;
        [SerializeField] private float badStepDipDegrees = 7f;
        [SerializeField] private float shoulderSagDegrees = 12f;
        [SerializeField] private float headLollDegrees = 14f;

        [Header("Dead arms")]
        [SerializeField, Range(0f, 1f)] private float armWeight = 0.85f;
        [SerializeField] private float armForward = 0.45f;
        [SerializeField] private float armSwingDegrees = 16f;

        private NavMeshAgent agent;
        private EchoAIController ai;
        private Transform hips, spine, chest, head, rightShoulder;
        private Transform leftFoot, rightFoot, rightUpperLeg, rightFootBone;
        private Transform[] upperArms = new Transform[2], lowerArms = new Transform[2], hands = new Transform[2];
        private float lurch, lurchVelocity, armSwing, armSwingVelocity;

        /// <summary>How much of the monster gait is applied right now (0 standing or running, 1 walking).</summary>
        public float Weight { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ai = GetComponent<EchoAIController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) { enabled = false; return; }
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            rightFootBone = rightFoot;
            upperArms[0] = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            upperArms[1] = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            lowerArms[0] = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            lowerArms[1] = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            hands[0] = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            hands[1] = animator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        private void OnDisable()
        {
            if (animator != null) animator.speed = 1f;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused: the animator did not rewrite the bones, so do not bend them again

            // Walking only: not standing, not in a pose (listening, pressing at a hiding place, catching), not running.
            float speed = agent != null && agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
            bool posed = ai != null && (ai.Current == EchoAIController.State.Listen || ai.Current == EchoAIController.State.Inspect
                                        || ai.Current == EchoAIController.State.Caught || ai.Current == EchoAIController.State.Inactive);
            float target = posed ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.7f, speed)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.3f, 2.9f, speed)));
            Weight = Mathf.MoveTowards(Weight, target, dt * 2.5f);

            // Which leg is swinging: the higher foot. +1 = the bad (right) leg is in the air, -1 = the good one.
            float swing = 0f;
            if (leftFoot != null && rightFoot != null)
                swing = Mathf.Clamp((rightFoot.position.y - leftFoot.position.y) / 0.12f, -1f, 1f);

            // The limp: slow through the bad leg's swing, hurry through the good one's.
            if (animator != null)
                animator.speed = Mathf.Lerp(1f, swing > 0f ? Mathf.Lerp(1f, badLegSwingSpeed, swing) : Mathf.Lerp(1f, goodLegSwingSpeed, -swing), Weight);
            if (Weight < 0.01f) return;

            Vector3 up = Vector3.up, fwd = transform.forward, right = transform.right;

            // The body lurches over the planted leg (springy, so it overshoots a little), dipping on the bad side.
            lurch = Mathf.SmoothDamp(lurch, -swing, ref lurchVelocity, 0.12f);
            float roll = lurch * lurchRollDegrees * Weight;
            float dip = Mathf.Max(0f, -lurch) * badStepDipDegrees * Weight; // weight on the bad leg: fold forward
            if (hips != null) hips.rotation = Quaternion.AngleAxis(roll * 0.6f, fwd) * hips.rotation;
            if (spine != null) spine.rotation = Quaternion.AngleAxis(roll * 0.5f, fwd) * Quaternion.AngleAxis(dip, right) * spine.rotation;
            if (chest != null) chest.rotation = Quaternion.AngleAxis(-roll * 0.25f, fwd) * chest.rotation;
            if (rightShoulder != null) rightShoulder.rotation = Quaternion.AngleAxis(shoulderSagDegrees * Weight, fwd) * rightShoulder.rotation;
            if (head != null) head.rotation = Quaternion.AngleAxis(-roll * headLollDegrees / Mathf.Max(1f, lurchRollDegrees), fwd) * head.rotation;

            // The bad leg: turned out, foot pointed down so its toes scrape along.
            if (rightUpperLeg != null) rightUpperLeg.rotation = Quaternion.AngleAxis(dragLegOutDegrees * Weight, up) * rightUpperLeg.rotation;
            if (rightFootBone != null) rightFootBone.rotation = Quaternion.AngleAxis(dragFootDownDegrees * Weight * Mathf.Clamp01(swing + 0.3f), right) * rightFootBone.rotation;

            // Dead arms: hanging forward and a little out, swinging loosely a beat behind the lurch.
            armSwing = Mathf.SmoothDamp(armSwing, lurch, ref armSwingVelocity, 0.28f);
            for (int i = 0; i < 2; i++)
            {
                Transform upper = upperArms[i], lower = lowerArms[i], hand = hands[i];
                if (upper == null || lower == null) continue;
                float side = i == 0 ? -1f : 1f;
                Quaternion sway = Quaternion.AngleAxis(armSwing * armSwingDegrees * side, fwd) * Quaternion.AngleAxis(armSwing * armSwingDegrees * 0.6f, right);
                Vector3 hang = sway * (-up + fwd * armForward + right * side * 0.12f).normalized;
                Aim(upper, lower.position, hang, armWeight * Weight);
                if (hand != null)
                {
                    Vector3 forearm = sway * (-up + fwd * (armForward + 0.35f) + right * side * 0.05f).normalized;
                    Aim(lower, hand.position, forearm, armWeight * Weight);
                }
            }
        }

        /// <summary>Turn a bone so the direction to its child points along dir (blended by weight).</summary>
        private static void Aim(Transform bone, Vector3 childPosition, Vector3 dir, float weight)
        {
            Vector3 current = childPosition - bone.position;
            if (current.sqrMagnitude < 1e-6f) return;
            Quaternion turn = Quaternion.FromToRotation(current.normalized, dir);
            bone.rotation = Quaternion.Slerp(Quaternion.identity, turn, weight) * bone.rotation;
        }
    }
}
