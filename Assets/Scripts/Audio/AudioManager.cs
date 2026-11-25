using UnityEngine;

namespace HorseFarm.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Range(0f, 1f)] public float musicVolume = 0.35f;
        [Range(0f, 1f)] public float sfxVolume = 0.65f;

        private AudioSource _calmMusicSource;
        private AudioSource _dangerMusicSource;
        private AudioSource _sfxSource;
        private bool _isDangerActive;

        private AudioClip _clipFootstep;
        private AudioClip _clipChew;
        private AudioClip _clipDrink;
        private AudioClip _clipNeigh;
        private AudioClip _clipGroom;
        private AudioClip _clipCoin;
        private AudioClip _clipClick;
        private AudioClip _clipLevelUp;
        private AudioClip _clipWolfHowl;
        private AudioClip _clipScream;
        private AudioClip _clipAlarm;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _calmMusicSource = gameObject.AddComponent<AudioSource>();
            _calmMusicSource.loop = true;
            _calmMusicSource.playOnAwake = false;
            _calmMusicSource.volume = musicVolume;

            _dangerMusicSource = gameObject.AddComponent<AudioSource>();
            _dangerMusicSource.loop = true;
            _dangerMusicSource.playOnAwake = false;
            _dangerMusicSource.volume = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;

            GenerateAudioClips();
        }

        private void Start()
        {
            if (_calmMusicSource.clip != null)
                _calmMusicSource.Play();
            if (_dangerMusicSource.clip != null)
                _dangerMusicSource.Play();
        }

        private void Update()
        {
            float targetCalm = _isDangerActive ? musicVolume * 0.25f : musicVolume;
            float targetDanger = _isDangerActive ? musicVolume * 0.95f : 0f;

            _calmMusicSource.volume = Mathf.MoveTowards(_calmMusicSource.volume, targetCalm, Time.deltaTime * 0.6f);
            _dangerMusicSource.volume = Mathf.MoveTowards(_dangerMusicSource.volume, targetDanger, Time.deltaTime * 0.8f);
        }

        public void SetDangerMusicState(bool active)
        {
            _isDangerActive = active;
        }

        public void PlayFootstep() => PlayClip(_clipFootstep, 0.35f);
        public void PlayChew() => PlayClip(_clipChew, 0.7f);
        public void PlayDrink() => PlayClip(_clipDrink, 0.7f);
        public void PlayNeigh() => PlayClip(_clipNeigh, 0.8f);
        public void PlayGroom() => PlayClip(_clipGroom, 0.7f);
        public void PlayCoin() => PlayClip(_clipCoin, 0.85f);
        public void PlayClick() => PlayClip(_clipClick, 0.5f);
        public void PlayLevelUp() => PlayClip(_clipLevelUp, 0.9f);
        public void PlayWolfHowl() => PlayClip(_clipWolfHowl, 0.95f);
        public void PlayScream() => PlayClip(_clipScream, 0.9f);
        public void PlayAlarm() => PlayClip(_clipAlarm, 0.8f);

        private void PlayClip(AudioClip clip, float scale = 1f)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, sfxVolume * scale);
        }

        private void GenerateAudioClips()
        {
            const int sampleRate = 22050;

            // 1. Calm acoustic plucked string loop (8 seconds)
            int calmSamples = sampleRate * 8;
            float[] calmData = new float[calmSamples];
            float[] notes = { 261.63f, 329.63f, 392.00f, 329.63f, 293.66f, 349.23f, 440.00f, 392.00f };
            int samplesPerNote = calmSamples / notes.Length;
            for (int i = 0; i < calmSamples; i++)
            {
                int noteIdx = (i / samplesPerNote) % notes.Length;
                float freq = notes[noteIdx];
                float localT = (i % samplesPerNote) / (float)sampleRate;
                float env = Mathf.Exp(-localT * 2.8f);
                float pad = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * (i / (float)sampleRate)) * 0.15f;
                float pluck = (Mathf.Sin(2f * Mathf.PI * freq * localT) + 0.35f * Mathf.Sin(4f * Mathf.PI * freq * localT)) * env * 0.25f;
                calmData[i] = Mathf.Clamp(pad + pluck, -0.8f, 0.8f);
            }
            _calmMusicSource.clip = CreateClip("CalmLoop", calmData, sampleRate);

            // 2. Danger tense pulsing low strings loop (4 seconds)
            int dangerSamples = sampleRate * 4;
            float[] dangerData = new float[dangerSamples];
            float[] dNotes = { 110f, 116.54f, 110f, 98f };
            int dStep = dangerSamples / 16;
            for (int i = 0; i < dangerSamples; i++)
            {
                int step = (i / dStep) % 16;
                float freq = dNotes[(step / 4) % dNotes.Length];
                float localT = (i % dStep) / (float)sampleRate;
                float env = Mathf.Exp(-localT * 7.5f);
                float wave = (Mathf.Sin(2f * Mathf.PI * freq * localT) + 0.5f * Mathf.Sin(2f * Mathf.PI * freq * 1.5f * localT)) * env * 0.35f;
                dangerData[i] = Mathf.Clamp(wave, -0.8f, 0.8f);
            }
            _dangerMusicSource.clip = CreateClip("DangerLoop", dangerData, sampleRate);

            _clipFootstep = CreateTone("Footstep", 95f, 65f, 0.08f, 0.35f, sampleRate);
            _clipChew = CreateTone("Chew", 240f, 180f, 0.22f, 0.3f, sampleRate);
            _clipDrink = CreateTone("Drink", 320f, 480f, 0.25f, 0.2f, sampleRate);
            _clipNeigh = CreateTone("Neigh", 620f, 440f, 0.45f, 0.15f, sampleRate);
            _clipGroom = CreateTone("Groom", 180f, 220f, 0.25f, 0.45f, sampleRate);
            _clipCoin = CreateTone("Coin", 880f, 1320f, 0.22f, 0.05f, sampleRate);
            _clipClick = CreateTone("Click", 600f, 750f, 0.05f, 0.05f, sampleRate);
            _clipLevelUp = CreateTone("LevelUp", 523.25f, 1046.5f, 0.55f, 0.05f, sampleRate);
            _clipWolfHowl = CreateTone("WolfHowl", 280f, 420f, 0.85f, 0.1f, sampleRate);
            _clipScream = CreateTone("Scream", 360f, 520f, 0.35f, 0.25f, sampleRate);
            _clipAlarm = CreateTone("Alarm", 740f, 520f, 0.4f, 0.1f, sampleRate);
        }

        private AudioClip CreateTone(string name, float startFreq, float endFreq, float duration, float noiseMix, int sampleRate)
        {
            int count = Mathf.Max(64, Mathf.RoundToInt(duration * sampleRate));
            float[] data = new float[count];
            uint rng = 12345u;
            for (int i = 0; i < count; i++)
            {
                float u = i / (float)count;
                float t = i / (float)sampleRate;
                float freq = Mathf.Lerp(startFreq, endFreq, u);
                float env = Mathf.Sin(u * Mathf.PI);
                rng = rng * 1664525u + 1013904223u;
                float noise = ((rng & 0xFFFF) / 32768f - 1f) * noiseMix;
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - noiseMix);
                data[i] = Mathf.Clamp((tone + noise) * env * 0.6f, -0.9f, 0.9f);
            }
            return CreateClip(name, data, sampleRate);
        }

        private AudioClip CreateClip(string name, float[] data, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
