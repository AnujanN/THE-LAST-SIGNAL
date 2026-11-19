using System.Collections;
using LastSignal.Audio;
using LastSignal.Interaction;
using LastSignal.UI;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// Loose notes are picked up and read in the hand: Alex's right hand reaches the sheet on the table, picks
    /// it up and brings it up in front of his face, holding its lower edge from behind so the writing stays
    /// clear. Closing the reader puts it back exactly where it was. The game is paused while reading, so
    /// everything here runs on unscaled time.
    /// </summary>
    public class NoteHolder : MonoBehaviour
    {
        [SerializeField] private Transform holdPoint;
        [SerializeField] private PlayerBody body;
        [Tooltip("Dim warm light so the sheet can be read even with the torch off.")]
        [SerializeField] private Light readingLight;
        [SerializeField] private float reachSeconds = 0.35f;
        [SerializeField] private float liftSeconds = 0.55f;
        [SerializeField] private float returnSeconds = 0.5f;
        [Tooltip("The sheet is shown this much larger while held (easier to read on a phone).")]
        [SerializeField] private float heldScale = 1.3f;

        private NoteInteractable note;
        private Transform sheet, grip, oldParent;
        private Vector3 oldPos, oldScale;
        private Quaternion oldRot;
        private Material oldMaterial;
        private Renderer sheetRenderer;
        private Collider[] colliders = new Collider[0];
        private Coroutine motion;
        private bool onTable;

        public bool Holding => note != null;
        public NoteInteractable Current => note;

        private void OnEnable()
        {
            NoteInteractable.HoldRequested = Hold;
            StoryHUD.NoteClosed += PutBack;
            if (readingLight != null) readingLight.enabled = false;
        }

        private void OnDisable()
        {
            if (NoteInteractable.HoldRequested == (System.Func<NoteInteractable, bool>)Hold) NoteInteractable.HoldRequested = null;
            StoryHUD.NoteClosed -= PutBack;
            if (note != null) Restore();
        }

        private bool Hold(NoteInteractable n)
        {
            if (note != null || holdPoint == null || n == null) return false;
            note = n;
            sheet = n.transform;
            oldParent = sheet.parent;
            oldPos = sheet.position;
            oldRot = sheet.rotation;
            oldScale = sheet.localScale;
            sheetRenderer = sheet.GetComponent<Renderer>();
            if (sheetRenderer != null && n.HeldMaterial != null)
            {
                oldMaterial = sheetRenderer.sharedMaterial;
                sheetRenderer.sharedMaterial = n.HeldMaterial;
            }
            // No collisions while it is in the hand (it would shove the player around).
            colliders = sheet.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders) c.enabled = false;
            if (n.Writing != null) n.Writing.gameObject.SetActive(true);

            // The hand first goes to the sheet where it lies, then holds its lower edge (see LateUpdate).
            if (grip == null) grip = new GameObject("NoteGrip").transform;
            onTable = true;
            PlaceGrip();
            if (body != null) { body.HoldOrient = 0f; body.HoldWith(grip); }
            if (readingLight != null) readingLight.enabled = true;

            AudioClip up = PlaceholderClips.PaperUp;
            if (up != null) AudioManager.Instance?.PlayUI(up, 0.8f);
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Lift());
            return true;
        }

        /// <summary>
        /// On the table: just above the sheet's lower right corner. In the hand: behind its lower edge, right of
        /// centre, a few centimetres up the back of the page (where the middle finger rests).
        /// </summary>
        private void PlaceGrip()
        {
            if (grip == null || sheet == null) return;
            Vector3 s = sheet.lossyScale;
            Vector3 right = sheet.right, up = sheet.forward, face = sheet.up;
            float halfW = Mathf.Abs(s.x) * 0.5f, halfH = Mathf.Abs(s.z) * 0.5f;
            grip.rotation = sheet.rotation;
            grip.position = onTable
                ? sheet.position + right * (halfW * 0.6f) - up * (halfH * 0.7f) + Vector3.up * 0.03f
                : sheet.position + right * (halfW * 0.35f) - up * (halfH - 0.045f) - face * 0.025f;
        }

        private void LateUpdate()
        {
            // Before PlayerBody (execution order 50) solves the arm for this frame.
            if (note != null) PlaceGrip();
        }

        private IEnumerator Lift()
        {
            // The hand reaches the sheet where it lies.
            for (float t = 0f; t < reachSeconds && (body == null || body.HoldWeight < 0.95f); t += Time.unscaledDeltaTime)
                yield return null;
            onTable = false;
            sheet.SetParent(holdPoint, true);
            Vector3 fromPos = sheet.localPosition, fromScale = sheet.localScale;
            Quaternion fromRot = sheet.localRotation;
            // Paper face (+Y) towards the eyes, top of the page (+Z) up; tilted back a little and slightly turned.
            Quaternion toRot = Quaternion.Euler(-14f, 0f, 3f) * Quaternion.LookRotation(Vector3.up, Vector3.back);
            Vector3 toScale = oldScale * heldScale;
            for (float t = 0f; t < liftSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / liftSeconds);
                // A small arc: up first, then towards the face.
                Vector3 p = Vector3.Lerp(fromPos, Vector3.zero, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.06f);
                sheet.localPosition = p;
                sheet.localRotation = Quaternion.Slerp(fromRot, toRot, k);
                sheet.localScale = Vector3.Lerp(fromScale, toScale, k);
                if (body != null) body.HoldOrient = k;
                yield return null;
            }
            if (body != null) body.HoldOrient = 1f;
            sheet.localPosition = Vector3.zero;
            sheet.localRotation = toRot;
            sheet.localScale = toScale;
            motion = null;
        }

        private void PutBack()
        {
            if (note == null) return;
            if (motion != null) StopCoroutine(motion);
            motion = StartCoroutine(Return());
        }

        private IEnumerator Return()
        {
            sheet.SetParent(null, true);
            Vector3 fromPos = sheet.position, fromScale = sheet.localScale;
            Quaternion fromRot = sheet.rotation;
            Vector3 toScale = oldParent != null ? DivideScale(oldScale, oldParent.lossyScale) : oldScale;
            for (float t = 0f; t < returnSeconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / returnSeconds);
                sheet.position = Vector3.Lerp(fromPos, oldPos, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.05f);
                sheet.rotation = Quaternion.Slerp(fromRot, oldRot, k);
                sheet.localScale = Vector3.Lerp(fromScale, toScale, k);
                if (body != null) body.HoldOrient = 1f - k;
                if (k > 0.7f) onTable = true;
                yield return null;
            }
            AudioClip down = PlaceholderClips.PaperDown;
            if (down != null) AudioManager.Instance?.PlayUI(down, 0.7f);
            Restore();
            motion = null;
        }

        private static Vector3 DivideScale(Vector3 local, Vector3 parentLossy) =>
            // Only used to keep the sheet's size steady while it is unparented mid-way.
            new Vector3(local.x * parentLossy.x, local.y * parentLossy.y, local.z * parentLossy.z);

        /// <summary>Back on the table exactly as it was.</summary>
        private void Restore()
        {
            if (sheet != null)
            {
                sheet.SetParent(oldParent, false);
                sheet.SetPositionAndRotation(oldPos, oldRot);
                sheet.localScale = oldScale;
            }
            if (sheetRenderer != null && oldMaterial != null) sheetRenderer.sharedMaterial = oldMaterial;
            foreach (Collider c in colliders) if (c != null) c.enabled = true;
            if (note != null && note.Writing != null) note.Writing.gameObject.SetActive(false);
            if (body != null) body.HoldWith(null);
            if (readingLight != null) readingLight.enabled = false;
            note = null;
            sheet = null;
            oldMaterial = null;
        }
    }
}
