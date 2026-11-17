using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// A brief, harmless shadow figure (Level 1 scare). Appears when Show() is called and
    /// vanishes when the player gets close or after a few seconds. Happens once.
    /// </summary>
    public class ShadowSighting : MonoBehaviour
    {
        [SerializeField] private string id = "shadow";
        [SerializeField] private GameObject figure;
        [SerializeField] private float vanishDistance = 6f;
        [SerializeField] private float maxSeconds = 4f;

        private Transform player;
        private float timer;
        private bool showing;

        private void Awake()
        {
            if (figure != null) figure.SetActive(false);
        }

        public void Show()
        {
            if (showing || WorldFlags.Has("seen:" + id) || figure == null) return;
            WorldFlags.Set("seen:" + id);
            PlayerController p = FindAnyObjectByType<PlayerController>();
            player = p != null ? p.transform : null;
            figure.SetActive(true);
            showing = true;
            timer = maxSeconds;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, figure.transform.position, 0.6f);
        }

        private void Update()
        {
            if (!showing) return;
            timer -= Time.deltaTime;
            bool close = player != null && Vector3.Distance(player.position, figure.transform.position) < vanishDistance;
            if (timer <= 0f || close)
            {
                figure.SetActive(false);
                showing = false;
                AudioManager.Instance?.PlayUI(PlaceholderClips.StaticBurst, 0.3f);
            }
        }
    }
}
