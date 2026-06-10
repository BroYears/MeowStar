using System;
using UnityEngine;
using Nyangsta.Core;
using Nyangsta.Save;

namespace Nyangsta.Audio
{
    /// <summary>
    /// All sound is generated procedurally at runtime (no imported audio files):
    /// short blips for taps and coins, an arpeggio for level-ups, and a gentle
    /// looping music-box melody for ambience. Volumes follow the save settings and
    /// a global mute toggle. Access via the <see cref="Sfx"/> facade.
    /// </summary>
    public class SfxManager : Singleton<SfxManager>
    {
        private const int SampleRate = 44100;

        private AudioSource[] _voices;        // round-robin SFX players
        private int _voiceIndex;
        private AudioSource _music;

        private AudioClip _tap, _coin, _purchase, _levelUp, _error, _whoosh, _ding, _huntStart, _unlock;
        private AudioClip _bgm;

        public bool Muted { get; private set; }

        private float SfxVolume => Muted ? 0f : SettingsVolume(s => s.sfxVolume, 0.7f);
        private float BgmVolume => Muted ? 0f : SettingsVolume(s => s.bgmVolume, 0.5f) * 0.32f;

        protected override void OnAwake()
        {
            _voices = new AudioSource[6];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
                _voices[i].spatialBlend = 0f;
            }
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            _music.spatialBlend = 0f;

            BuildClips();
        }

        private void Start()
        {
            _music.clip = _bgm;
            _music.volume = BgmVolume;
            if (_bgm != null) _music.Play();
        }

        // ------------------------------------------------------------- public API
        public void PlayTap()      => Play(_tap, 0.5f, UnityEngine.Random.Range(0.97f, 1.05f));
        public void PlayCoin()     => Play(_coin, 0.8f, UnityEngine.Random.Range(0.98f, 1.04f));
        public void PlayPurchase() => Play(_purchase, 0.85f);
        public void PlayLevelUp()  => Play(_levelUp, 0.9f);
        public void PlayError()    => Play(_error, 0.6f);
        public void PlayWhoosh()   => Play(_whoosh, 0.5f, UnityEngine.Random.Range(0.95f, 1.07f));
        public void PlayDing()     => Play(_ding, 0.7f, UnityEngine.Random.Range(0.99f, 1.03f));
        public void PlayHuntStart()=> Play(_huntStart, 0.8f);
        public void PlayUnlock()   => Play(_unlock, 0.9f);

        public bool ToggleMute()
        {
            Muted = !Muted;
            _music.volume = BgmVolume;
            return Muted;
        }

        private void Play(AudioClip clip, float gain, float pitch = 1f)
        {
            if (clip == null || _voices == null) return;
            float vol = SfxVolume * gain;
            if (vol <= 0.001f) return;
            var v = _voices[_voiceIndex];
            _voiceIndex = (_voiceIndex + 1) % _voices.Length;
            v.pitch = pitch;
            v.PlayOneShot(clip, vol);
        }

        private static float SettingsVolume(Func<SettingsData, float> pick, float fallback)
        {
            var sm = SaveManager.Instance;
            if (sm != null && sm.Data != null && sm.Data.settings != null)
                return Mathf.Clamp01(pick(sm.Data.settings));
            return fallback;
        }

        // ----------------------------------------------------------- synthesis
        private void BuildClips()
        {
            _tap       = Bell("tap", new[] { 880f }, 0.07f, 0.5f);
            _coin      = Sequence("coin", new[] { (988f, 0.05f), (1319f, 0.12f) }, 0.55f);
            _purchase  = Sequence("purchase", new[] { (659f, 0.07f), (988f, 0.07f), (1319f, 0.16f) }, 0.5f);
            _levelUp   = Sequence("levelup", new[] { (523f, 0.09f), (659f, 0.09f), (784f, 0.09f), (1047f, 0.26f) }, 0.5f);
            _ding      = Bell("ding", new[] { 1175f, 2350f }, 0.28f, 0.45f);
            _huntStart = Sequence("huntstart", new[] { (440f, 0.08f), (587f, 0.08f), (880f, 0.18f) }, 0.5f);
            _unlock    = Sequence("unlock", new[] { (784f, 0.1f), (1047f, 0.1f), (1319f, 0.28f) }, 0.5f);
            _error     = Square("error", 150f, 0.18f, 0.4f);
            _whoosh    = Noise("whoosh", 0.16f, 0.5f);
            _bgm       = MusicBox();
        }

        /// <summary>Bell/sine tone with optional harmonics and exponential decay.</summary>
        private static AudioClip Bell(string name, float[] partials, float dur, float amp)
        {
            int n = Mathf.CeilToInt(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 14f) * Attack(t, 0.004f);
                float s = 0f;
                for (int p = 0; p < partials.Length; p++)
                    s += Mathf.Sin(2f * Mathf.PI * partials[p] * t) / (p + 1);
                data[i] = s * env * amp / partials.Length;
            }
            return ToClip(name, data);
        }

        /// <summary>A series of bell notes played back-to-back (arpeggio/chime).</summary>
        private static AudioClip Sequence(string name, (float freq, float dur)[] notes, float amp)
        {
            int total = 0;
            foreach (var note in notes) total += Mathf.CeilToInt(SampleRate * note.dur);
            var data = new float[total];
            int off = 0;
            foreach (var note in notes)
            {
                int len = Mathf.CeilToInt(SampleRate * note.dur);
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = Mathf.Exp(-t * 10f) * Attack(t, 0.004f);
                    float s = Mathf.Sin(2f * Mathf.PI * note.freq * t)
                            + 0.4f * Mathf.Sin(2f * Mathf.PI * note.freq * 2f * t);
                    if (off + i < data.Length) data[off + i] = s * env * amp * 0.7f;
                }
                off += len;
            }
            return ToClip(name, data);
        }

        private static AudioClip Square(string name, float freq, float dur, float amp)
        {
            int n = Mathf.CeilToInt(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 8f) * Attack(t, 0.004f);
                float ph = (freq * t) % 1f;
                data[i] = (ph < 0.5f ? 1f : -1f) * env * amp * 0.5f;
            }
            return ToClip(name, data);
        }

        private static AudioClip Noise(string name, float dur, float amp)
        {
            int n = Mathf.CeilToInt(SampleRate * dur);
            var data = new float[n];
            var rng = new System.Random();
            float last = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 16f) * Attack(t, 0.003f);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last = Mathf.Lerp(last, white, 0.25f);   // low-passed -> airy whoosh
                data[i] = last * env * amp;
            }
            return ToClip(name, data);
        }

        /// <summary>Gentle looping music-box melody (C-major pentatonic), decays to silence at the loop point.</summary>
        private static AudioClip MusicBox()
        {
            float[] scale = { 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f };
            int[] pattern = { 0, 2, 4, 2, 3, 1, 4, 5, 4, 2, 0, 1, 2, 4, 3, 1 };
            float noteDur = 0.5f;
            int noteLen = Mathf.CeilToInt(SampleRate * noteDur);
            var data = new float[noteLen * pattern.Length];

            for (int k = 0; k < pattern.Length; k++)
            {
                float freq = scale[pattern[k]];
                int baseIdx = k * noteLen;
                for (int i = 0; i < noteLen; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = Mathf.Exp(-t * 5.5f) * Attack(t, 0.006f);
                    float s = Mathf.Sin(2f * Mathf.PI * freq * t)
                            + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2f * t)
                            + 0.12f * Mathf.Sin(2f * Mathf.PI * freq * 3f * t);
                    data[baseIdx + i] = s * env * 0.22f;
                }
            }
            return ToClip("bgm_musicbox", data, loop: true);
        }

        private static float Attack(float t, float attack)
            => attack <= 0f ? 1f : Mathf.Clamp01(t / attack);

        private static AudioClip ToClip(string name, float[] data, bool loop = false)
        {
            // Guard against clipping.
            float peak = 0f;
            for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            if (peak > 1f) { float inv = 0.98f / peak; for (int i = 0; i < data.Length; i++) data[i] *= inv; }

            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    /// <summary>Null-safe static facade so any system can trigger a sound in one line.</summary>
    public static class Sfx
    {
        public static void Tap()       => SfxManager.Instance?.PlayTap();
        public static void Coin()      => SfxManager.Instance?.PlayCoin();
        public static void Purchase()  => SfxManager.Instance?.PlayPurchase();
        public static void LevelUp()   => SfxManager.Instance?.PlayLevelUp();
        public static void Error()     => SfxManager.Instance?.PlayError();
        public static void Whoosh()    => SfxManager.Instance?.PlayWhoosh();
        public static void Ding()      => SfxManager.Instance?.PlayDing();
        public static void HuntStart() => SfxManager.Instance?.PlayHuntStart();
        public static void Unlock()    => SfxManager.Instance?.PlayUnlock();
        public static bool ToggleMute()=> SfxManager.Instance != null && SfxManager.Instance.ToggleMute();
        public static bool IsMuted     => SfxManager.Instance != null && SfxManager.Instance.Muted;
    }
}
