using UnityEngine;

namespace SlimeRancher.Area1
{
    // One slot per game event. The asset lives in Resources ("SonidosJuego") so every scene finds it;
    // drag any AudioClip into a slot to change that sound, or empty it to silence the event.
    [CreateAssetMenu(menuName = "Slime Rancher/Sonidos del juego")]
    public sealed class Area1SoundBank : ScriptableObject
    {
        [System.Serializable]
        public sealed class Sound
        {
            public AudioClip clip;
            [Range(0, 1)] public float volume = 1;
        }

        [Header("Aspiradora y agua")]
        [Tooltip("Mientras se mantiene el gatillo para aspirar objetos (se repite).")]
        public Sound aspiradoraSuccionando = new Sound();
        [Tooltip("Al expulsar un objeto del inventario.")]
        public Sound aspiradoraBota = new Sound();
        [Tooltip("Cuando el agua disparada choca contra algo.")]
        public Sound aguaChoque = new Sound();

        [Header("Slimes")]
        [Tooltip("Cada salto de un slime.")]
        public Sound slimeSalpicadura = new Sound { volume = .5f };
        [Tooltip("Un slime malo ataca a un slime bueno.")]
        public Sound ataqueSlime = new Sound();
        [Tooltip("Un slime se come una zanahoria o un pollo.")]
        public Sound slimeComeVegetal = new Sound();
        [Tooltip("Un slime suelta un plort.")]
        public Sound slimeSueltaPlort = new Sound();

        [Header("Plorts y pollos")]
        [Tooltip("Un plort cae y golpea el suelo.")]
        public Sound plortCaeSuelo = new Sound { volume = .7f };
        [Tooltip("Un plort entra al recolector.")]
        public Sound plortEntraRecolector = new Sound();
        [Tooltip("Aparece un pollo nuevo.")]
        public Sound spawnPollo = new Sound();
        [Tooltip("Cacareo ocasional de un pollo suelto.")]
        public Sound cacareoPollo1 = new Sound { volume = .7f };
        public Sound cacareoPollo2 = new Sound { volume = .7f };
        public Sound cacareoPollo3 = new Sound { volume = .7f };

        [Header("Ambiente")]
        [Tooltip("Música o sonido de fondo en bucle durante AREA1.")]
        public Sound ambienteArea1 = new Sound { volume = .35f };

        [Header("Jugador")]
        [Tooltip("Pasos al caminar (con joystick o caminando de verdad).")]
        public Sound jugadorCamina = new Sound { volume = .6f };
        [Tooltip("Al saltar.")]
        public Sound jugadorSalto = new Sound();
        [Tooltip("Al caer al suelo después de un salto o caída.")]
        public Sound jugadorAterrizaje = new Sound();
        [Tooltip("Un enemigo te hace daño.")]
        public Sound jugadorRecibeDanio = new Sound();

        [Header("Menú y oleadas")]
        [Tooltip("Cualquier botón del menú de inicio o de pausa.")]
        public Sound clickMenu = new Sound();
        [Tooltip("Al ganar una oleada.")]
        public Sound ganoOleada = new Sound();
    }
}
