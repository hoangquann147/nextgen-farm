using System;
using UnityEngine;

namespace NextGenFarm
{
    // Two independent buses. Replace the generated clips with imported licensed audio as needed.
    public sealed class FarmAudio : MonoBehaviour
    {
        public AudioSource MusicSource { get; private set; }
        public AudioSource SfxSource { get; private set; }
        private FarmSettings settings;
        private FarmGridManager grid;
        private AudioClip music, click, action;
        public void Initialize(FarmSettings preferences, FarmGridManager farm)
        {
            settings = preferences; grid = farm;
            MusicSource = gameObject.AddComponent<AudioSource>();
            SfxSource = gameObject.AddComponent<AudioSource>();
            MusicSource.playOnAwake = SfxSource.playOnAwake = false;
            MusicSource.spatialBlend = SfxSource.spatialBlend = 0;
            music = MakeMusic(); click = MakeTone("UI tick", 660, .09f); action = MakeTone("Farm action", 330, .2f);
            MusicSource.clip = music; MusicSource.loop = true;
            settings.Changed += Apply; grid.ActionApplied += OnAction; Apply(); MusicSource.Play();
        }
        private void Apply()
        {
            // Multiplication preserves independent music/SFX levels when master changes.
            MusicSource.volume = settings.Data.master * settings.Data.music;
            SfxSource.volume = settings.Data.master * settings.Data.sfx;
        }
        public void Click() { if (SfxSource != null) SfxSource.PlayOneShot(click, .35f); }
        private void OnAction(Vector2Int cell, Core.FarmAction tool, int count) { SfxSource.PlayOneShot(action, .3f); }
        private static AudioClip MakeTone(string name, float frequency, float duration)
        {
            const int rate = 22050; var samples = new float[(int)(rate * duration)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * Mathf.Exp(-t * 32) * Mathf.Min(t * 200, 1) * .3f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private static AudioClip MakeMusic()
        {
            const int rate = 22050, seconds = 24; var samples = new float[rate * seconds];
            float[] notes = { 261.63f, 329.63f, 392, 523.25f, 440, 349.23f, 329.63f, 293.66f };
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate, beat = t % 3;
                int n = (int)(t / 3) % notes.Length;
                float envelope = Mathf.Min(beat * 4, 1) * Mathf.Exp(-beat * 1.5f);
                samples[i] = (Mathf.Sin(2 * Mathf.PI * notes[n] * t) + .25f * Mathf.Sin(4 * Mathf.PI * notes[n] * t)) * envelope * .08f;
            }
            var clip = AudioClip.Create("Valley arpeggio (procedural)", samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void OnDestroy()
        {
            if (settings != null) settings.Changed -= Apply;
            if (grid != null) grid.ActionApplied -= OnAction;
            if (MusicSource != null) MusicSource.Stop(); if (SfxSource != null) SfxSource.Stop();
            if (music != null) Destroy(music); if (click != null) Destroy(click); if (action != null) Destroy(action);
        }
    }
}
