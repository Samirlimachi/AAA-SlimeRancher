using UnityEngine;
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
        bool previousSlot, previousWater;
        float nextShot;
        void Update()
        {
            if (!vacuum || !game) return;
            var left = RanchVRControls.ReadHand(XRNode.LeftHand, true);
            var right = RanchVRControls.ReadHand(XRNode.RightHand, true);
            if (right.secondary && !previousSlot) { game.Select(game.selected + 1); if (water) water.SelectWater(false); }
            if (left.primary && !previousWater && water) water.SelectWater(!water.WaterSelected);
            previousWater = left.primary;
            previousSlot = right.secondary;
            var pickup = vacuum.GetComponent<VacuumPickup>();
            bool held = pickup && pickup.IsHeld;
            var hand = pickup && pickup.IsLeftHand ? left : right;
            var other = pickup && pickup.IsLeftHand ? right : left;
            bool suction = held && hand.tracked && hand.trigger > .2f;
            bool source = water && water.SetSuction(suction);
            vacuum.SetSuction(suction && !source);
            if (held && other.tracked && other.trigger > .2f && !suction && Time.time >= nextShot)
            {
                if (water && water.WaterSelected) water.Shoot(); else vacuum.Shoot();
                nextShot = Time.time + .3f;
            }
        }
        void LateUpdate()
        {
            if (!game || !wristDisplay) return;
            var slot = game.slots[game.selected];
            wristDisplay.text = "BEATRIX | DEPOSITO " + (game.selected + 1) + "/4\n" +
                (slot.count > 0 ? game.Data(slot.kind).itemName + "  " + slot.count + "/" + game.Limit(slot.kind) : "Vacio") +
                "\nA: saltar | B: deposito\nGrip: equipar / soltar\nGatillo herramienta: aspirar\nOtro gatillo: lanzar";
            if (head) wristDisplay.transform.rotation = head.rotation;
        }
        void OnDisable() { if (vacuum) vacuum.SetSuction(false); if (water) water.SetSuction(false); }
    }
}
