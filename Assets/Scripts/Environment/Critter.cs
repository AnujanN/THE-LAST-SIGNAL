using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>A mouse that darts across the floor from cover to cover, squeaking, then is gone.</summary>
    public class Critter : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private AudioSource voice;
        [SerializeField] private float speed = 3.4f;

        private readonly Vector3[] path = new Vector3[4];
        private int index;
        private float stopTimer;

        public bool Busy => gameObject.activeSelf;

        public void Run(Vector3 from, Vector3 to)
        {
            // A slightly wandering line with one hesitation half-way.
            Vector3 side = Vector3.Cross(Vector3.up, (to - from).normalized);
            path[0] = from;
            path[1] = Vector3.Lerp(from, to, 0.35f) + side * Random.Range(-0.25f, 0.25f);
            path[2] = Vector3.Lerp(from, to, 0.65f) + side * Random.Range(-0.25f, 0.25f);
            path[3] = to;
            index = 1;
            stopTimer = 0f;
            transform.position = from;
            transform.rotation = Quaternion.LookRotation(path[1] - from);
            gameObject.SetActive(true);
            if (voice != null)
            {
                voice.pitch = Random.Range(0.9f, 1.2f);
                voice.PlayOneShot(Audio.PlaceholderClips.Squeak, 0.8f);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (stopTimer > 0f)
            {
                stopTimer -= dt;
                if (model != null) model.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 25f) * 12f, 0f); // sniffing
                return;
            }
            Vector3 target = path[index];
            Vector3 to = target - transform.position;
            float step = speed * dt;
            if (to.magnitude <= step)
            {
                transform.position = target;
                index++;
                if (index >= path.Length) { gameObject.SetActive(false); return; }
                if (index == 2 && Random.value < 0.5f) stopTimer = Random.Range(0.2f, 0.6f);
                return;
            }
            transform.position += to.normalized * step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 1f - Mathf.Exp(-20f * dt));
            if (model != null)
            {
                // Quick scurrying bob and wiggle.
                model.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 32f)) * 0.012f;
                model.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 18f) * 8f, 0f);
            }
        }
    }
}
