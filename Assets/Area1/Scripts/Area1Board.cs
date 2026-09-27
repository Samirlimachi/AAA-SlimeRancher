using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SlimeRancher.Area1
{
    // One pressable entry of a wall board. The objects live in the scene (built by
    // Area1BoardInstaller in the Editor); the board scripts only fill in texts and colors.
    [System.Serializable]
    public sealed class Area1BoardSlot
    {
        public Renderer button;
        public Text title, state, price;
        public GameObject coin;
    }

    public static class Area1Board
    {
        public static readonly Color Locked = new Color(.28f, .3f, .34f), Buyable = new Color(.95f, .68f, .12f),
            Owned = new Color(.2f, .68f, .35f), Done = new Color(.15f, .52f, .9f), Running = new Color(.85f, .2f, .22f);

        public static bool IsBuilt(Area1BoardSlot[] slots, int count, Text header, Object owner)
        {
            bool built = header && slots != null && slots.Length == count;
            if (built) foreach (var slot in slots) built &= slot != null && slot.button && slot.title && slot.state && slot.price && slot.coin;
            if (!built) Debug.LogWarning("Tablero sin construir: usa el menu Area1 > Construir tablero y tiendas.", owner);
            return built;
        }

        // Press = select (GRIP) with the ray or the hand on the button block.
        public static void Hook(Area1BoardSlot slot, UnityAction onPress)
        {
            var interactable = slot.button.GetComponent<XRSimpleInteractable>();
            if (!interactable) interactable = slot.button.gameObject.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(_ => onPress());
        }

        public static void Show(Area1BoardSlot slot, Color color, string title, string state, int price)
        {
            slot.button.material.color = color;
            slot.title.text = title;
            slot.state.text = state;
            bool priced = price > 0;
            slot.price.text = priced ? price.ToString() : "";
            if (slot.coin.activeSelf != priced) slot.coin.SetActive(priced);
        }
    }
}
