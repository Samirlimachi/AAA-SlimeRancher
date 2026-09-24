using UnityEngine;
using UnityEngine.UI;
using SlimeRancherVR;
namespace SlimeRancher.Area1
{
    [DefaultExecutionOrder(13000)]
    public sealed class Area1HUD : MonoBehaviour
    {
        public RanchGame game;
        public Area1WaterVacuum water;
        UnityEngine.UI.Text waterText, waterHint;
        UnityEngine.UI.Image waterBorder, waterFill;
        public Camera viewCamera;
        [SerializeField] bool showInventoryOnScreen;
        Text coins, health, energy, clock;
        Image healthFill, energyFill;
        readonly Text[] names = new Text[4], counts = new Text[4];
        readonly Image[] borders = new Image[4], marks = new Image[4];
        public Material hudMaterial;
        Font font;
        RectTransform root;
        void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = viewCamera;
            canvas.sortingOrder = 100;
            root = GetComponent<RectTransform>(); root.sizeDelta = new Vector2(1280,720);
            transform.SetParent(viewCamera.transform,false);
            transform.localPosition = new Vector3(0,0,2);
            transform.localRotation = Quaternion.identity;
            gameObject.layer = 5;
            clock = Label("Dia y hora",new Vector2(36,624),new Vector2(200,70),25,TextAnchor.UpperLeft);
            coins = Label("Monedas",new Vector2(34,158),new Vector2(250,40),30,TextAnchor.MiddleLeft);
            coins.color = new Color(1,.82f,.29f);
            Bar("VIDA",new Vector2(34,102),new Color(.97f,.16f,.3f),out healthFill,out health);
            Bar("ESTAMINA",new Vector2(34,48),new Color(.05f,.73f,.95f),out energyFill,out energy);
            if(showInventoryOnScreen)
            {
                for(int i=0;i<4;i++)
                {
                    float x=390+i*132;
                    borders[i]=Panel("Ranura "+(i+1),new Vector2(x,38),new Vector2(122,130),Color.white);
                    Panel("Interior",new Vector2(x+4,42),new Vector2(114,122),new Color(.08f,.12f,.17f,.92f));
                    marks[i]=Panel("Tipo",new Vector2(x+43,134),new Vector2(36,23),Color.clear);
                    names[i]=Label("Objeto",new Vector2(x+7,82),new Vector2(108,50),20,TextAnchor.MiddleCenter);
                    counts[i]=Label("Cantidad",new Vector2(x+8,48),new Vector2(106,34),26,TextAnchor.MiddleCenter);
                    Label("Numero",new Vector2(x+8,135),new Vector2(30,25),18,TextAnchor.MiddleLeft).text=(i+1).ToString();
                }
                waterBorder=Panel("Deposito de agua",new Vector2(944,38),new Vector2(276,130),new Color(.2f,.75f,1));
                Panel("Agua fondo",new Vector2(948,42),new Vector2(268,122),new Color(.04f,.12f,.2f,.94f));
                waterFill=Panel("Agua nivel",new Vector2(960,52),new Vector2(244,9),new Color(.1f,.75f,1));
                waterText=Label("Agua cantidad",new Vector2(958,92),new Vector2(252,60),26,TextAnchor.MiddleCenter);
                waterHint=Label("Agua estado",new Vector2(954,63),new Vector2(256,30),17,TextAnchor.MiddleCenter);
                Label("Seleccionar agua",new Vector2(944,8),new Vector2(276,26),18,TextAnchor.MiddleCenter).text="X  ·  SELECCIONAR AGUA";
                Label("Cambiar deposito",new Vector2(390,8),new Vector2(518,26),18,TextAnchor.MiddleCenter).text="B  ·  CAMBIAR DEPOSITO";
            }
        }
        RectTransform Element(string title,Vector2 position,Vector2 size)
        {
            var go=new GameObject(title,typeof(RectTransform));go.layer=5;
            var rect=go.GetComponent<RectTransform>();rect.SetParent(root,false);rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.zero;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        Image Panel(string title,Vector2 pos,Vector2 size,Color color)
        {var image=Element(title,pos,size).gameObject.AddComponent<Image>();image.material=hudMaterial;image.color=color;image.raycastTarget=false;return image;}
        Text Label(string title,Vector2 pos,Vector2 size,int sizeFont,TextAnchor alignment)
        {var text=Element(title,pos,size).gameObject.AddComponent<Text>();text.material=hudMaterial;text.font=font;text.fontSize=sizeFont;text.alignment=alignment;text.color=Color.white;text.raycastTarget=false;return text;}
        void Bar(string title,Vector2 pos,Color color,out Image fill,out Text value)
        {
            Panel(title+" borde",pos,new Vector2(255,44),Color.white);
            Panel(title+" fondo",pos+Vector2.one*3,new Vector2(249,38),new Color(.08f,.12f,.17f,.95f));
            fill=Panel(title+" relleno",pos+Vector2.one*3,new Vector2(249,38),color);
            value=Label(title,pos+new Vector2(10,3),new Vector2(235,38),22,TextAnchor.MiddleCenter);
        }
        void LateUpdate()
        {
            if(!game||!viewCamera)return;
            // Fit within both desktop and headset vertical/horizontal fields of view.
            float h=4*Mathf.Tan(viewCamera.fieldOfView*.5f*Mathf.Deg2Rad);
            float scale=Mathf.Min(h/720,h*viewCamera.aspect/1280)*.93f;
            transform.localScale=Vector3.one*scale;
            if(water && waterText)
            {
                waterText.text="AGUA  "+water.Amount+" / "+water.capacity;
                waterHint.text=water.IsFilling?"CARGANDO DEL ESTANQUE":water.HasSource&&water.Amount>=water.capacity?"DEPOSITO LLENO":water.WaterSelected?"AGUA SELECCIONADA":"ASPIRA DEL ESTANQUE";
                waterFill.rectTransform.sizeDelta=new Vector2(244*Mathf.Clamp01((float)water.Amount/water.capacity),9);
                waterBorder.color=water.WaterSelected?new Color(1,.78f,.2f):new Color(.2f,.75f,1);
            }
            coins.text="MONEDAS   "+game.coins;
            health.text="VIDA   "+Mathf.CeilToInt(game.health);
            energy.text="ESTAMINA   "+Mathf.CeilToInt(game.energy);
            healthFill.rectTransform.sizeDelta=new Vector2(249*Mathf.Clamp01(game.health/100),38);
            energyFill.rectTransform.sizeDelta=new Vector2(249*Mathf.Clamp01(game.energy/100),38);
            int minutes=Mathf.FloorToInt(game.clock%1440);
            clock.text="Dia "+(1+Mathf.FloorToInt(game.clock/1440))+"\n"+(minutes/60).ToString("00")+":"+(minutes%60).ToString("00");
            for(int i=0;i<4 && names[i];i++)
            {
                var slot=game.slots[i];var data=slot.count>0?game.Data(slot.kind):null;
                names[i].text=data?data.itemName:"Vacio";
                counts[i].text=data?"x "+slot.count+" / "+game.Limit(slot.kind):"—";
                marks[i].color=data?data.color:Color.clear;
                borders[i].color=i==game.selected&&(!water||!water.WaterSelected)?new Color(1,.78f,.2f):new Color(.8f,.86f,.9f);
            }
        }
    }
}

