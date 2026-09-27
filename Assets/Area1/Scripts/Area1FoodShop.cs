using UnityEngine;
using UnityEngine.UI;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    // Wall shop: slime food and hearts (walk over one to heal) bought with coins are dropped on the
    // floor in front of the shop.
    public sealed class Area1FoodShop : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Offer
        {
            public RanchItemKind kind;
            public int price;
        }

        public Offer[] offers =
        {
            new Offer { kind = RanchItemKind.Carrot, price = 5 },
            new Offer { kind = RanchItemKind.Chicken, price = 15 },
            new Offer { kind = RanchItemKind.ElderChicken, price = 30 },
            new Offer { kind = RanchItemKind.Heart, price = 25 }
        };

        [Header("Where bought food appears (local to the shop, in front of it)")]
        public float dropDistance = 1.1f;
        public float dropHeight = .35f;
        public Vector2 dropArea = new Vector2(.7f, .25f);

        [Header("Scene objects (Area1 > Construir tablero y tiendas)")]
        public Area1BoardSlot[] slots;
        public Text header, status;
        bool built;
        float nextPress, nextRefresh;
        RanchGame Game => RanchGame.Instance;

        void Awake()
        {
            built = Area1Board.IsBuilt(slots, offers.Length, header, this);
            if (!built) return;
            status.text = "Apunta a un producto y presiona GRIP.\nLo que compres aparece en el suelo, frente a la tienda.\nPasa sobre un corazón para recuperar vida.";
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                Area1Board.Hook(slots[i], () => Buy(index));
            }
        }

        void Buy(int index)
        {
            if (Time.time < nextPress || !Game || !built) return;
            nextPress = Time.time + .4f;
            var offer = offers[index];
            var data = Game.Data(offer.kind);
            if (!data) return;
            if (Game.coins < offer.price)
                Game.Notify("Necesitas " + offer.price + " monedas para comprar " + data.itemName + ".");
            else if (data.prefab)
            {
                // Drop it on the floor in front of the shop (the board faces its -Z side).
                var local = new Vector3(Random.Range(-dropArea.x, dropArea.x), dropHeight, -dropDistance + Random.Range(-dropArea.y, dropArea.y));
                var item = Game.Spawn(offer.kind, transform.TransformPoint(local), Quaternion.Euler(0, Random.Range(0, 360f), 0));
                item.graceUntil = Time.time + .5f;
                Game.coins -= offer.price;
                Game.Notify(data.itemName + " comprado por " + offer.price + " monedas. Está en el suelo frente a la tienda.");
            }
            Refresh();
        }

        void Update()
        {
            if (Time.time >= nextRefresh) Refresh();
        }

        void Refresh()
        {
            nextRefresh = Time.time + .25f;
            if (!Game || !built) return;
            header.text = "Monedas: " + Game.coins;
            for (int i = 0; i < offers.Length; i++)
            {
                var data = Game.Data(offers[i].kind);
                bool affordable = Game.coins >= offers[i].price;
                Area1Board.Show(slots[i], affordable ? Area1Board.Owned : Area1Board.Locked,
                    data ? data.itemName.ToUpper() : offers[i].kind.ToString(), affordable ? "COMPRAR 1" : "SIN MONEDAS", offers[i].price);
            }
        }
    }
}
