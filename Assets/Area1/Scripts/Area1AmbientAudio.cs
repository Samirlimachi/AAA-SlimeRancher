using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimeRancher.Area1
{
    public sealed class Area1AmbientAudio : MonoBehaviour
    {
        AudioSource source;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.loop = true;
        }

        void OnEnable()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SyncScene(SceneManager.GetActiveScene());
        }

        void OnDisable() => SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        void OnActiveSceneChanged(Scene previous, Scene current) => SyncScene(current);

        void SyncScene(Scene scene)
        {
            if (scene.name != "AREA1")
            {
                source.Stop();
                return;
            }

            var sound = Area1Audio.Pick(b => b.ambienteArea1);
            if (sound == null)
            {
                source.Stop();
                return;
            }

            source.clip = sound.clip;
            source.volume = sound.volume;
            if (!source.isPlaying) source.Play();
        }
    }
}
