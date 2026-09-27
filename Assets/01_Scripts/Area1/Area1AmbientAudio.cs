using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimeRancher.Area1
{
    // AREA1 music: the background music loops while playing; during a wave it cross-fades to the wave
    // music and back when the wave is won or lost (Area1WaveBoard calls SetWaveMusic).
    // Clips come from SonidosJuego ("Música de fondo", "Música de oleada"; "Ambiente Area1" as fallback).
    public sealed class Area1AmbientAudio : MonoBehaviour
    {
        const float FadeSeconds = 1.5f;
        static Area1AmbientAudio instance;
        static bool waveRequested;

        AudioSource background, wave;
        float backgroundTarget, waveTarget;
        bool inArea;

        // Switch between background and wave music (smooth cross-fade).
        public static void SetWaveMusic(bool on)
        {
            waveRequested = on;
            if (instance) instance.Refresh();
        }

        void Awake()
        {
            instance = this;
            waveRequested = false;
            background = CreateSource();
            wave = CreateSource();
        }

        AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.loop = true;
            source.priority = 0; // never cut off when many other sounds are playing
            source.volume = 0;
            return source;
        }

        void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SyncScene(SceneManager.GetActiveScene());
        }

        void OnDisable() => SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        void OnDestroy() { if (instance == this) instance = null; }

        void OnActiveSceneChanged(Scene previous, Scene current) => SyncScene(current);

        void SyncScene(Scene scene)
        {
            inArea = scene.name == "AREA1";
            if (!inArea) { background.Stop(); wave.Stop(); return; }
            var music = Area1Audio.Pick(b => b.musicaFondo) ?? Area1Audio.Pick(b => b.ambienteArea1);
            if (music == null) Debug.LogWarning("Música: la casilla 'Musica Fondo' de SonidosJuego está vacía.");
            Assign(background, music);
            Assign(wave, Area1Audio.Pick(b => b.musicaOleada));
            Refresh();
        }

        static void Assign(AudioSource source, Area1SoundBank.Sound sound)
        {
            var clip = sound != null ? sound.clip : null;
            if (source.clip == clip) return;
            source.Stop();
            source.clip = clip;
            if (clip && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        }

        void Refresh()
        {
            if (!inArea) return;
            var backgroundSound = Area1Audio.Pick(b => b.musicaFondo) ?? Area1Audio.Pick(b => b.ambienteArea1);
            var waveSound = Area1Audio.Pick(b => b.musicaOleada);
            bool waveOn = waveRequested && wave.clip;
            waveTarget = waveOn ? waveSound.volume : 0;
            backgroundTarget = !waveOn && backgroundSound != null ? backgroundSound.volume : 0;
            // The wave music starts from the beginning each wave.
            if (waveOn && !wave.isPlaying) { wave.time = 0; wave.Play(); }
            // After a wave the background continues where it was paused.
            if (backgroundTarget > 0 && !background.isPlaying && background.clip)
            {
                if (background.time > 0) background.UnPause(); else background.Play();
            }
        }

        void Update()
        {
            if (!inArea) return;
            float music = SlimeRancherVR.SlimeGameOptions.MusicVolume; // menu option, applied live
            Fade(background, backgroundTarget * music);
            Fade(wave, waveTarget * music);
        }

        static void Fade(AudioSource source, float target)
        {
            if (!source.clip) return;
            float step = Time.unscaledDeltaTime / FadeSeconds;
            source.volume = Mathf.MoveTowards(source.volume, target, step * Mathf.Max(target, .3f));
            // Stop the silent track (the background resumes where it was left).
            if (target <= 0 && source.volume <= .001f && source.isPlaying)
            {
                if (source.loop && source.clip) source.Pause();
            }
            else if (target > 0 && !source.isPlaying) source.UnPause();
        }
    }
}
