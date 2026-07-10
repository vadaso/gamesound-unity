using System.Collections;
using UnityEngine;

namespace GameSound.Unity
{
    [AddComponentMenu("GameSound/GameSound Audio Source")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class GameSoundAudioSource : MonoBehaviour
    {
        [SerializeField] private GameSoundAsset sound;
        [SerializeField, HideInInspector] private GameSoundSoundReference soundReference = new GameSoundSoundReference();
        [SerializeField, HideInInspector] private bool playOnStart;
        [SerializeField] private bool syncClipOnValidate = true;

        [Header("Event Triggers")]
        [SerializeField] private GameSoundEmitterTrigger playTrigger = GameSoundEmitterTrigger.None;
        [SerializeField] private GameSoundEmitterTrigger stopTrigger = GameSoundEmitterTrigger.ObjectDisable;
        [SerializeField] private GameSoundStopMode stopMode = GameSoundStopMode.Immediate;
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.25f;
        [SerializeField, HideInInspector] private bool stopOnDisable = true;

        [Header("Playback")]
        [SerializeField] private bool loop;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 500f;

        private const float MinSupportedPitch = 0.01f;

        [Header("Variation")]
        [SerializeField] private bool randomizePitch;
        [SerializeField, Range(MinSupportedPitch, 3f)] private float randomPitchMin = 1f;
        [SerializeField, Range(MinSupportedPitch, 3f)] private float randomPitchMax = 1f;

        private Coroutine fadeRoutine;

        public GameSoundAsset Sound
        {
            get => sound;
            set => ApplyRemoteAsset(value);
        }

        public GameSoundSoundReference SoundReference
        {
            get
            {
                EnsureSoundReference();
                if (sound != null) soundReference.ApplyAsset(sound);
                return soundReference;
            }
        }

        public GameSoundEmitterTrigger PlayTrigger
        {
            get => playTrigger;
            set => playTrigger = value;
        }

        public GameSoundEmitterTrigger StopTrigger
        {
            get => stopTrigger;
            set => stopTrigger = value;
        }

        public GameSoundStopMode StopMode
        {
            get => stopMode;
            set => stopMode = value;
        }

        public float FadeOutSeconds
        {
            get => fadeOutSeconds;
            set => fadeOutSeconds = Mathf.Max(0f, value);
        }

        public bool PlayOnStart
        {
            get => playOnStart || playTrigger == GameSoundEmitterTrigger.ObjectStart;
            set
            {
                playOnStart = value;
                playTrigger = value ? GameSoundEmitterTrigger.ObjectStart : GameSoundEmitterTrigger.None;
            }
        }

        public bool Loop
        {
            get => loop;
            set
            {
                loop = value;
                ApplyToAudioSource();
            }
        }

        public float Volume
        {
            get => volume;
            set
            {
                volume = Mathf.Clamp01(value);
                ApplyToAudioSource();
            }
        }

        public float SpatialBlend
        {
            get => spatialBlend;
            set
            {
                spatialBlend = Mathf.Clamp01(value);
                ApplyToAudioSource();
            }
        }

        public float MinDistance
        {
            get => minDistance;
            set
            {
                minDistance = Mathf.Max(0f, value);
                if (maxDistance < minDistance) maxDistance = minDistance;
                ApplyToAudioSource();
            }
        }

        public float MaxDistance
        {
            get => maxDistance;
            set
            {
                maxDistance = Mathf.Max(minDistance, value);
                ApplyToAudioSource();
            }
        }

        public bool RandomizePitch
        {
            get => randomizePitch;
            set => randomizePitch = value;
        }

        public float RandomPitchMin
        {
            get => randomPitchMin;
            set
            {
                randomPitchMin = NormalizePitch(value);
                if (randomPitchMax < randomPitchMin) randomPitchMax = randomPitchMin;
            }
        }

        public float RandomPitchMax
        {
            get => randomPitchMax;
            set
            {
                randomPitchMax = NormalizePitch(value);
                if (randomPitchMax < randomPitchMin) randomPitchMin = randomPitchMax;
            }
        }

        protected virtual void Reset()
        {
            EnsureSoundReference();
            ApplyToAudioSource();
        }

        protected virtual void OnValidate()
        {
            EnsureSoundReference();
            if (sound != null) soundReference.ApplyAsset(sound);
            if (minDistance < 0f) minDistance = 0f;
            if (maxDistance < minDistance) maxDistance = minDistance;
            randomPitchMin = NormalizePitch(randomPitchMin);
            randomPitchMax = NormalizePitch(randomPitchMax);
            if (randomPitchMax < randomPitchMin) randomPitchMax = randomPitchMin;
            fadeOutSeconds = Mathf.Max(0f, fadeOutSeconds);

            if (syncClipOnValidate)
            {
                ApplyToAudioSource();
            }
        }

        protected virtual void Awake()
        {
            EnsureSoundReference();
            if (sound != null) soundReference.ApplyAsset(sound);
            ApplyToAudioSource();
        }

        protected virtual void OnEnable()
        {
            HandleTrigger(GameSoundEmitterTrigger.ObjectEnable);
        }

        protected virtual void Start()
        {
            if (playOnStart)
            {
                Play();
                return;
            }

            HandleTrigger(GameSoundEmitterTrigger.ObjectStart);
        }

        protected virtual void OnDisable()
        {
            if (stopOnDisable)
            {
                StopWithConfiguredMode();
                return;
            }

            HandleTrigger(GameSoundEmitterTrigger.ObjectDisable);
        }

        protected virtual void OnDestroy()
        {
            HandleTrigger(GameSoundEmitterTrigger.ObjectDestroy);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            HandleTrigger(GameSoundEmitterTrigger.TriggerEnter);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            HandleTrigger(GameSoundEmitterTrigger.TriggerExit);
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            HandleTrigger(GameSoundEmitterTrigger.TriggerEnter2D);
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            HandleTrigger(GameSoundEmitterTrigger.TriggerExit2D);
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            HandleTrigger(GameSoundEmitterTrigger.CollisionEnter);
        }

        protected virtual void OnCollisionExit(Collision collision)
        {
            HandleTrigger(GameSoundEmitterTrigger.CollisionExit);
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            HandleTrigger(GameSoundEmitterTrigger.CollisionEnter2D);
        }

        protected virtual void OnCollisionExit2D(Collision2D collision)
        {
            HandleTrigger(GameSoundEmitterTrigger.CollisionExit2D);
        }

        protected virtual void OnMouseEnter()
        {
            HandleTrigger(GameSoundEmitterTrigger.MouseEnter);
        }

        protected virtual void OnMouseExit()
        {
            HandleTrigger(GameSoundEmitterTrigger.MouseExit);
        }

        protected virtual void OnMouseDown()
        {
            HandleTrigger(GameSoundEmitterTrigger.MouseDown);
        }

        protected virtual void OnMouseUp()
        {
            HandleTrigger(GameSoundEmitterTrigger.MouseUp);
        }

        public void ApplyRemoteAsset(GameSoundAsset asset)
        {
            sound = asset;
            EnsureSoundReference();
            soundReference.ApplyAsset(asset);
            ApplyToAudioSource();
        }

        public void Play()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            ApplyToAudioSource();
            var source = GetComponent<AudioSource>();
            if (source == null || source.clip == null) return;

            if (randomizePitch)
            {
                var minPitch = NormalizePitch(randomPitchMin);
                var maxPitch = NormalizePitch(randomPitchMax);
                if (maxPitch < minPitch) maxPitch = minPitch;
                source.pitch = Random.Range(minPitch, maxPitch);
            }
            else
            {
                source.pitch = 1f;
            }
            source.volume = volume;
            source.Play();
        }

        public void Stop()
        {
            StopWithConfiguredMode();
        }

        public void StopImmediate()
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            var source = GetComponent<AudioSource>();
            if (source == null) return;
            source.Stop();
            source.volume = volume;
        }

        public void ApplyToAudioSource()
        {
            var source = GetComponent<AudioSource>();
            if (source == null) return;

            if (sound != null)
            {
                source.clip = sound.Clip;
            }
            else if (soundReference != null && soundReference.Clip != null)
            {
                source.clip = soundReference.Clip;
            }

            source.playOnAwake = PlayOnStart;
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
        }

        private void HandleTrigger(GameSoundEmitterTrigger trigger)
        {
            if (trigger == GameSoundEmitterTrigger.None) return;
            if (playTrigger == trigger)
            {
                Play();
            }

            if (stopTrigger == trigger)
            {
                StopWithConfiguredMode();
            }
        }

        private void StopWithConfiguredMode()
        {
            if (stopMode == GameSoundStopMode.None) return;
            if (stopMode == GameSoundStopMode.FadeOut && isActiveAndEnabled && fadeOutSeconds > 0f)
            {
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeOutAndStop());
                return;
            }

            StopImmediate();
        }

        private IEnumerator FadeOutAndStop()
        {
            var source = GetComponent<AudioSource>();
            if (source == null)
            {
                fadeRoutine = null;
                yield break;
            }

            var startVolume = source.volume;
            var elapsed = 0f;
            while (elapsed < fadeOutSeconds && source.isPlaying)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / fadeOutSeconds));
                yield return null;
            }

            source.Stop();
            source.volume = volume;
            fadeRoutine = null;
        }

        private static float NormalizePitch(float value)
        {
            return Mathf.Clamp(value, MinSupportedPitch, 3f);
        }

        private void EnsureSoundReference()
        {
            if (soundReference == null)
            {
                soundReference = new GameSoundSoundReference();
            }
        }
    }
}
