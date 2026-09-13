using UnityEngine;

namespace ChulaEarthquakeVR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FurnitureImpactAudio : MonoBehaviour
    {
        private Rigidbody body;
        private MovableFurniture furniture;
        private float nextScrape;
        private float nextImpact;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            furniture = GetComponent<MovableFurniture>();
        }

        private void Update()
        {
            if (body == null || furniture == null || !furniture.IsInteracting || body.linearVelocity.magnitude < 0.18f) return;
            if (Time.unscaledTime < nextScrape) return;
            nextScrape = Time.unscaledTime + 0.28f;
            GameplayAudioDirector.PlayCue(GameplayAudioCue.FurnitureScrape, 0.13f,
                Mathf.Lerp(0.85f, 1.12f, Mathf.Clamp01(body.linearVelocity.magnitude / 3f)));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.unscaledTime < nextImpact || collision.relativeVelocity.magnitude < 0.75f) return;
            nextImpact = Time.unscaledTime + 0.16f;
            GameplayAudioDirector.PlayCue(GameplayAudioCue.FurnitureImpact,
                Mathf.Lerp(0.10f, 0.35f, Mathf.Clamp01(collision.relativeVelocity.magnitude / 5f)));
        }
    }
}
