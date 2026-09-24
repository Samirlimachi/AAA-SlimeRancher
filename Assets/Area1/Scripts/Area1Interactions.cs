using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // The existing XRI providers remain the sole owners of movement, gravity and jumping.
    [DefaultExecutionOrder(12000)]
    public sealed class Area1Interactions : MonoBehaviour
    {
        public SlimeVacuum vacuum;
        public RanchGame game;
        public Area1WaterVacuum water;
        public TextMesh wristDisplay;
        public Transform head;
        bool previousPrimary, previousSecondary;
        float nextShot;
        public void SelectNext()
        {
            if (!game) return;
            if (water && water.WaterSelected)
            {
                water.SelectWater(false);
                game.Select(0);
            }
            else if (water && game.selected >= game.slots.Length - 1)
            {
                water.SelectWater(true);
            }
            else
            {
                game.Select(game.selected + 1);
                if (water) water.SelectWater(false);
            }
        }
        void Update()
        {
            if (!vacuum || !game) return;
            var left = RanchVRControls.ReadHand(XRNode.LeftHand, true);
            var right = RanchVRControls.ReadHand(XRNode.RightHand, true);
            // Direct PC shortcut for testing the fifth slot without VR controllers.
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.digit5Key.wasPressedThisFrame && water)
            {
                game.Select(game.slots.Length - 1);
                water.SelectWater(true);
            }
            // RanchVRControls has already moved the regular 1-4 selection this frame.
            // Extend that existing A/B cycle with water as the fifth entry.
            if (right.primary && !previousPrimary && water)
            {
                if (water.WaterSelected)
                {
                    water.SelectWater(false);
                    game.Select(0);
                }
                else if (game.selected == 0)
                {
                    game.Select(game.slots.Length - 1);
                    water.SelectWater(true);
                }
            }
            if (right.secondary && !previousSecondary && water)
            {
                if (water.WaterSelected)
                {
                    water.SelectWater(false);
                    game.Select(game.slots.Length - 1);
                }
                else if (game.selected == game.slots.Length - 1)
                {
                    water.SelectWater(true);
                }
            }
            previousPrimary = right.primary;
            previousSecondary = right.secondary;
            var pickup = vacuum.GetComponent<VacuumPickup>();
            bool held = pickup && pickup.IsHeld;
            var hand = pickup && pickup.IsLeftHand ? left : right;
            var other = pickup && pickup.IsLeftHand ? right : left;
            bool toolTrigger = held && hand.tracked && hand.trigger > .2f;
            bool waterMode = water && water.WaterSelected;
            if (waterMode)
            {
                vacuum.SetSuction(false);
                water.SetSuction(false);
                if (toolTrigger && Time.time >= nextShot && water.Shoot())
                    nextShot = Time.time + .3f;
            }
            else
            {
                bool source = water && water.SetSuction(toolTrigger);
                vacuum.SetSuction(toolTrigger && !source);
                if (held && other.tracked && other.trigger > .2f && !toolTrigger && Time.time >= nextShot && vacuum.Shoot())
                    nextShot = Time.time + .3f;
            }
        }
        void LateUpdate()
        {
            if (!game || !wristDisplay) return;
            string inventory = "INVENTARIO";
            for (int i = 0; i < game.slots.Length; i++)
            {
                var slot = game.slots[i];
                string value = slot.count > 0 ? game.Data(slot.kind).itemName + " x" + slot.count : "Vacio";
                inventory += "\n" + (i == game.selected && (!water || !water.WaterSelected) ? "> " : "  ") + (i + 1) + ". " + value;
            }
            if (water)
                inventory += "\n" + (water.WaterSelected ? "> " : "  ") + "5. AGUA " + water.Amount + "/" + water.capacity;
            wristDisplay.text = inventory;
            wristDisplay.transform.localScale = Vector3.one * .00135f;
            var pickup = vacuum ? vacuum.GetComponent<VacuumPickup>() : null;
            var inventoryRenderer = wristDisplay.GetComponent<Renderer>();
            if (inventoryRenderer) inventoryRenderer.enabled = pickup && pickup.IsHeld;
            if (head) wristDisplay.transform.rotation = head.rotation;
        }
        void OnDisable() { if (vacuum) vacuum.SetSuction(false); if (water) water.SetSuction(false); }
    }
}
