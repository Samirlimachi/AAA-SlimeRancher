using UnityEngine;
using UnityEngine.EventSystems;
using SlimeRancher.Area1;

namespace SlimeRancherVR
{
    public sealed class SlimeMenuButtonAudio : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData) => Area1Audio.Play2D(b => b.clickMenu, .22f);

        public void PlayClick() => Area1Audio.Play2D(b => b.clickMenu, .8f);

        public static void PlayOpen() => Area1Audio.Play2D(b => b.clickMenu, .5f);
    }
}
