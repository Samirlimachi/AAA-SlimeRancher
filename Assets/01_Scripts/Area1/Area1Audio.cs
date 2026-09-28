using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlimeRancher.Area1
{
    // Plays the clips chosen in the "SonidosJuego" sound bank:
    //   Area1Audio.Play(b => b.aguaChoque, point);   3D, at the event position
    //   Area1Audio.Play2D(b => b.clickMenu);          in the player's ears (menus, own damage)
    // Empty slots are simply silent. Uses a small pool of AudioSources that survives scene changes.
    public static class Area1Audio
    {
        const float MaxHearing = 25;
        static Area1SoundBank bank;
        static readonly List<AudioSource> pool = new List<AudioSource>();
        static GameObject host;
        static int next;

        public static Area1SoundBank Bank => bank ? bank : bank = Resources.Load<Area1SoundBank>("SonidosJuego");

        public static void Play(Func<Area1SoundBank, Area1SoundBank.Sound> pick, Vector3 position, float volumeScale = 1, float delay = 0)
        {
            var sound = Pick(pick);
            if (sound == null) return;
            var listener = Camera.main;
            if (listener && Vector3.Distance(listener.transform.position, position) > MaxHearing) return;
            var source = Source();
            source.transform.position = position;
            source.spatialBlend = 1;
            Start(source, sound, volumeScale, delay);
        }

        public static void Play2D(Func<Area1SoundBank, Area1SoundBank.Sound> pick, float volumeScale = 1)
        {
            var sound = Pick(pick);
            if (sound == null) return;
            var source = Source();
            source.spatialBlend = 0;
            Start(source, sound, volumeScale, 0);
        }

        // Clip and volume of a slot (for looping sources owned by other scripts), or null if empty.
        public static Area1SoundBank.Sound Pick(Func<Area1SoundBank, Area1SoundBank.Sound> pick)
        {
            var current = Bank;
            if (!current) return null;
            var sound = pick(current);
            return sound != null && sound.clip ? sound : null;
        }

        static void Start(AudioSource source, Area1SoundBank.Sound sound, float volumeScale, float delay)
        {
            source.Stop();
            source.clip = sound.clip;
            source.volume = sound.volume * volumeScale;
            source.pitch = 1 + UnityEngine.Random.Range(-.04f, .04f); // tiny variation so repeats feel natural
            if (delay > 0) source.PlayDelayed(delay); else source.Play();
        }

        static AudioSource Source()
        {
            if (!host)
            {
                host = new GameObject("Sonidos del juego");
                UnityEngine.Object.DontDestroyOnLoad(host);
                pool.Clear();
            }
            // Reuse a source that finished; otherwise add one (up to 24), then take the oldest.
            foreach (var free in pool) if (free && !free.isPlaying) return free;
            if (pool.Count < 24)
            {
                var go = new GameObject("Sonido");
                go.transform.SetParent(host.transform, false);
                var created = go.AddComponent<AudioSource>();
                created.playOnAwake = false;
                created.minDistance = 1.5f;
                created.maxDistance = MaxHearing;
                created.rolloffMode = AudioRolloffMode.Linear;
                created.dopplerLevel = 0;
                pool.Add(created);
                return created;
            }
            next = (next + 1) % pool.Count;
            return pool[next];
        }
    }
}
