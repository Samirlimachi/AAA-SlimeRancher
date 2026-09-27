using System.Collections.Generic;
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
        [Tooltip("En el visor, alto (en grados) que ocupa el HUD. Las esquinas del campo completo quedan fuera de los lentes.")]
        [SerializeField, Range(20, 80)] float headsetVerticalDegrees = 40;
        Text coins, health, energy, clock;
        Image healthFill, energyFill;
        readonly Text[] names = new Text[4], counts = new Text[4];
        readonly Image[] borders = new Image[4], marks = new Image[4];
        public Material hudMaterial;
        [Header("Iconos 3D (Area1 > Construir tablero y tiendas los asigna)")]
        public GameObject coinModel;
        public Texture coinTexture;
        public GameObject heartModel;
        public Texture heartTexture;
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
            coins = Label("Monedas",new Vector2(92,158),new Vector2(250,40),30,TextAnchor.MiddleLeft);
            coins.color = new Color(1,.82f,.29f);
            Bar("VIDA",new Vector2(92,102),new Color(.97f,.16f,.3f),out healthFill,out health);
            Bar("ESTAMINA",new Vector2(92,48),new Color(.05f,.73f,.95f),out energyFill,out energy);
            // Icons left of each value: swaying coin and heart models, drawn lightning bolt.
            Icon3D(coinModel,coinTexture,root,UiPoint(60,178),44);
            Icon3D(heartModel,heartTexture,root,UiPoint(60,124),46);
            var bolt=Element("Icono estamina",new Vector2(38,48),new Vector2(44,44)).gameObject.AddComponent<Image>();
            bolt.sprite=BoltSprite();bolt.material=hudMaterial;bolt.color=new Color(1,.86f,.25f);bolt.raycastTarget=false;
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
        RectTransform Element(string title,Vector2 position,Vector2 size,RectTransform parent=null)
        {
            var go=new GameObject(title,typeof(RectTransform));go.layer=5;
            var rect=go.GetComponent<RectTransform>();rect.SetParent(parent?parent:root,false);rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.zero;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        Image Panel(string title,Vector2 pos,Vector2 size,Color color,RectTransform parent=null)
        {var image=Element(title,pos,size,parent).gameObject.AddComponent<Image>();image.material=hudMaterial;image.color=color;image.raycastTarget=false;return image;}
        Text Label(string title,Vector2 pos,Vector2 size,int sizeFont,TextAnchor alignment,RectTransform parent=null)
        {var text=Element(title,pos,size,parent).gameObject.AddComponent<Text>();text.material=hudMaterial;text.font=font;text.fontSize=sizeFont;text.alignment=alignment;text.color=Color.white;text.raycastTarget=false;return text;}
        void Bar(string title,Vector2 pos,Color color,out Image fill,out Text value)
        {
            Panel(title+" borde",pos,new Vector2(255,44),Color.white);
            Panel(title+" fondo",pos+Vector2.one*3,new Vector2(249,38),new Color(.08f,.12f,.17f,.95f));
            fill=Panel(title+" relleno",pos+Vector2.one*3,new Vector2(249,38),color);
            value=Label(title,pos+new Vector2(10,3),new Vector2(235,38),22,TextAnchor.MiddleCenter);
        }
        // HUD layout point (bottom-left origin, 1280x720) to a local position under the centred root.
        static Vector3 UiPoint(float x,float y)=>new Vector3(x-640,y-360,-5);

        // Model copy with the always-on-top HUD shader, fitted to `size` HUD units and gently swaying.
        Material Icon3D(GameObject model,Texture texture,Transform parent,Vector3 localPosition,float size)
        {
            var shader=Shader.Find("Area1/HUD Model");
            if(!model||!shader)return null;
            var holder=new GameObject("Icono "+model.name);holder.layer=5;
            holder.transform.SetParent(parent,false);holder.transform.localPosition=localPosition;
            var sway=holder.AddComponent<Area1ModelSpin>();sway.bobHeight=0;sway.swayDegrees=25;
            var fit=Instantiate(model,holder.transform,false).transform;
            var material=new Material(shader){mainTexture=texture};
            foreach(var r in fit.GetComponentsInChildren<Renderer>(true))
            {
                var materials=new Material[Mathf.Max(1,r.sharedMaterials.Length)];
                for(int i=0;i<materials.Length;i++)materials[i]=material;
                r.sharedMaterials=materials;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;r.gameObject.layer=5;
            }
            var bounds=new Bounds();bool first=true;
            foreach(var f in fit.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!f.sharedMesh)continue;var b=f.sharedMesh.bounds;
                for(int n=0;n<8;n++)
                {
                    var p=b.center+Vector3.Scale(b.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1));
                    p=holder.transform.InverseTransformPoint(f.transform.TransformPoint(p));
                    if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                }
            }
            if(!first)
            {
                float scale=size/Mathf.Max(.0001f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
                fit.localScale*=scale;fit.localPosition=(fit.localPosition-bounds.center)*scale;
            }
            return material;
        }

        static Sprite BoltSprite()
        {
            const int size=64,samples=4;
            var shape=new[]{new Vector2(.62f,1),new Vector2(.16f,.43f),new Vector2(.45f,.43f),new Vector2(.34f,0),new Vector2(.84f,.6f),new Vector2(.55f,.6f)};
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                int inside=0;
                for(int sy=0;sy<samples;sy++)for(int sx=0;sx<samples;sx++)
                    if(Inside(shape,new Vector2((x+(sx+.5f)/samples)/size,(y+(sy+.5f)/samples)/size)))inside++;
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255*inside/(samples*samples)));
            }
            texture.SetPixels32(pixels);texture.Apply();
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        }

        static bool Inside(Vector2[] polygon,Vector2 point)
        {
            bool inside=false;
            for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
                if((polygon[i].y>point.y)!=(polygon[j].y>point.y)&&point.x<(polygon[j].x-polygon[i].x)*(point.y-polygon[i].y)/(polygon[j].y-polygon[i].y)+polygon[i].x)inside=!inside;
            return inside;
        }

        // ---- Player hurt: red vignette pulse and the health bar blinks white ----
        const float HurtTime=.6f;
        Image hurtVignette;
        float hurtStart=-10,lastHealth=-1;

        void BuildHurt()
        {
            hurtVignette=Element("Dano vignette",Vector2.zero,new Vector2(1280,720)).gameObject.AddComponent<Image>();
            hurtVignette.sprite=VignetteSprite();hurtVignette.material=hudMaterial;hurtVignette.raycastTarget=false;
            hurtVignette.color=new Color(1,.1f,.08f,0);
            hurtVignette.transform.SetAsFirstSibling(); // behind the rest of the HUD
        }

        void AnimateHurt()
        {
            if(lastHealth>=0&&game.health<lastHealth-.01f)hurtStart=Time.time;
            lastHealth=game.health;
            float t=Time.time-hurtStart;
            float k=t<HurtTime?1-t/HurtTime:0;
            hurtVignette.color=new Color(1,.1f,.08f,.75f*k*k);
            healthFill.color=Color.Lerp(new Color(.97f,.16f,.3f),Color.white,k>.5f&&Mathf.Repeat(t*12,1)<.5f?.8f:0);
        }

        // Transparent centre, red towards the edges.
        static Sprite VignetteSprite()
        {
            const int w=128,h=72;
            var texture=new Texture2D(w,h,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float dx=(x+.5f)/w*2-1,dy=(y+.5f)/h*2-1;
                float edge=Mathf.Clamp01((Mathf.Sqrt(dx*dx+dy*dy)-.55f)/.6f);
                pixels[y*w+x]=new Color32(255,255,255,(byte)(255*edge*edge));
            }
            texture.SetPixels32(pixels);texture.Apply();
            return Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(.5f,.5f),100);
        }

        // ---- Wave result banner ----
        // Victory: gold, pops in with a bounce, confetti, coin models, rising fanfare.
        // Defeat: dark red, drops in shaking, slow grey/red ash, broken gems, falling tones.
        const float CelebrationTime=4.5f;
        static readonly Color[] ConfettiColors={new Color(1,.82f,.2f),new Color(1,.35f,.6f),new Color(.3f,.85f,1),new Color(.45f,1,.45f),Color.white};
        static readonly Color[] AshColors={new Color(.35f,.33f,.36f),new Color(.55f,.1f,.12f),new Color(.2f,.18f,.2f)};
        static readonly Color Gold=new Color(1,.78f,.2f),Blood=new Color(.85f,.12f,.14f);
        RectTransform banner,confettiRoot;
        CanvasGroup bannerGroup,confettiGroup;
        Text bannerTitle,bannerSubtitle;
        Image bannerGlow,bannerBack,bannerStripe;
        readonly List<Image> bannerGems=new List<Image>();
        readonly List<RectTransform> confetti=new List<RectTransform>();
        readonly List<Vector3> confettiMotion=new List<Vector3>(); // x speed, y speed, spin
        readonly List<Material> bannerIcons=new List<Material>();
        readonly List<GameObject> bannerIconObjects=new List<GameObject>();
        float celebrationStart=-100;
        bool victory;
        AudioSource fanfareSource;
        AudioClip fanfare,defeatTune;

        public void Celebrate(string title,string subtitle)=>ShowResult(title,subtitle,true);
        public void ShowDefeat(string title,string subtitle)=>ShowResult(title,subtitle,false);

        void ShowResult(string title,string subtitle,bool won)
        {
            if(!banner)BuildCelebration();
            victory=won;
            bannerTitle.text=title;bannerSubtitle.text=subtitle;
            bannerTitle.color=won?new Color(1,.85f,.3f):new Color(1,.4f,.36f);
            bannerSubtitle.color=won?Color.white:new Color(.9f,.8f,.8f);
            bannerBack.color=won?new Color(.05f,.09f,.16f,.96f):new Color(.12f,.03f,.05f,.96f);
            bannerStripe.color=won?Gold:Blood;
            for(int i=0;i<bannerGems.Count;i++)
            {
                bannerGems[i].color=won?new Color(1,.85f,.3f):new Color(.3f,.28f,.3f);
                // Defeat: gems knocked crooked.
                bannerGems[i].rectTransform.localRotation=Quaternion.Euler(0,0,won?45:45+(i-1)*25);
            }
            foreach(var icon in bannerIconObjects)icon.SetActive(won);
            celebrationStart=Time.time;
            banner.gameObject.SetActive(true);confettiRoot.gameObject.SetActive(true);
            for(int i=0;i<confetti.Count;i++)
            {
                confetti[i].anchoredPosition=new Vector2(Random.Range(60f,1220f),Random.Range(720f,1000f));
                confettiMotion[i]=won
                    ?new Vector3(Random.Range(-40f,40f),-Random.Range(170f,300f),Random.Range(-360f,360f))
                    :new Vector3(Random.Range(-10f,10f),-Random.Range(60f,120f),Random.Range(-60f,60f));
                confetti[i].GetComponent<Image>().color=won?ConfettiColors[Random.Range(0,ConfettiColors.Length)]:AshColors[Random.Range(0,AshColors.Length)];
            }
            var winSound=won?Area1Audio.Pick(b=>b.ganoOleada):null;
            if(winSound!=null)fanfareSource.PlayOneShot(winSound.clip,winSound.volume);
            else fanfareSource.PlayOneShot(won?fanfare:defeatTune,.6f);
        }

        void BuildCelebration()
        {
            confettiRoot=Element("Confeti",Vector2.zero,new Vector2(1280,720));
            confettiGroup=confettiRoot.gameObject.AddComponent<CanvasGroup>();
            for(int i=0;i<48;i++)
            {
                var piece=Panel("Papel",Vector2.zero,new Vector2(Random.Range(9f,14f),Random.Range(16f,24f)),Color.white,confettiRoot);
                piece.rectTransform.pivot=new Vector2(.5f,.5f);
                confetti.Add(piece.rectTransform);confettiMotion.Add(Vector3.zero);
            }
            banner=Element("Resultado de oleada",new Vector2(640,420),new Vector2(900,220));
            banner.pivot=new Vector2(.5f,.5f);
            bannerGroup=banner.gameObject.AddComponent<CanvasGroup>();
            bannerGlow=Panel("Brillo",new Vector2(-12,-12),new Vector2(924,244),Gold,banner);
            bannerBack=Panel("Fondo",new Vector2(0,0),new Vector2(900,220),new Color(.05f,.09f,.16f,.96f),banner);
            bannerStripe=Panel("Franja",new Vector2(0,150),new Vector2(900,6),Gold,banner);
            // Three diamonds on top (drawn, so they never depend on font glyphs).
            for(int i=-1;i<=1;i++)
            {
                var gem=Panel("Estrella",new Vector2(450+i*70,188),new Vector2(i==0?30:22,i==0?30:22),new Color(1,.85f,.3f),banner);
                gem.rectTransform.pivot=new Vector2(.5f,.5f);gem.rectTransform.localRotation=Quaternion.Euler(0,0,45);
                bannerGems.Add(gem);
            }
            bannerTitle=Label("Titulo",new Vector2(20,70),new Vector2(860,80),56,TextAnchor.MiddleCenter,banner);
            bannerTitle.fontStyle=FontStyle.Bold;
            bannerTitle.resizeTextForBestFit=true;bannerTitle.resizeTextMinSize=24;bannerTitle.resizeTextMaxSize=56;
            bannerSubtitle=Label("Detalle",new Vector2(20,18),new Vector2(860,50),30,TextAnchor.MiddleCenter,banner);
            bannerSubtitle.resizeTextForBestFit=true;bannerSubtitle.resizeTextMinSize=16;bannerSubtitle.resizeTextMaxSize=30;
            // Coin models on both sides of the title (positions relative to the banner centre). Victory only.
            foreach(var x in new[]{-380f,380f})
            {
                var material=Icon3D(coinModel,coinTexture,banner,new Vector3(x,20,-5),110);
                if(!material)continue;
                bannerIcons.Add(material);bannerIconObjects.Add(banner.GetChild(banner.childCount-1).gameObject);
            }
            fanfareSource=gameObject.AddComponent<AudioSource>();fanfareSource.playOnAwake=false;fanfareSource.spatialBlend=0;
            fanfare=Tune("Fanfarria",new[]{523.25f,659.25f,783.99f,1046.5f},.14f,.45f);
            defeatTune=Tune("Derrota",new[]{392f,329.63f,261.63f,196f},.26f,.8f);
            banner.gameObject.SetActive(false);confettiRoot.gameObject.SetActive(false);
        }

        void AnimateCelebration()
        {
            if(!banner||!banner.gameObject.activeSelf)return;
            float t=Time.time-celebrationStart;
            if(t>=CelebrationTime){banner.gameObject.SetActive(false);confettiRoot.gameObject.SetActive(false);return;}
            if(victory)
            {
                banner.localScale=Vector3.one*(t<.45f?EaseOutBack(t/.45f):1+Mathf.Sin(t*3)*.012f);
                banner.anchoredPosition=new Vector2(640,420);
                bannerGlow.color=Color.Lerp(Gold,Color.white,(Mathf.Sin(t*6)+1)*.18f);
            }
            else
            {
                // Falls from above, lands with a decaying shake, then breathes slowly in dark red.
                float drop=t<.35f?1-Mathf.Pow(1-t/.35f,3):1;
                float shake=t>.35f&&t<1.1f?Mathf.Sin(t*55)*14*(1-(t-.35f)/.75f):0;
                banner.localScale=Vector3.one;
                banner.anchoredPosition=new Vector2(640+shake,420+(1-drop)*160);
                bannerGlow.color=Color.Lerp(Blood,new Color(.3f,.02f,.04f),(Mathf.Sin(t*2.5f)+1)*.35f);
            }
            float alpha=Mathf.Clamp01((CelebrationTime-t)/.6f)*(victory?1:Mathf.Clamp01(t/.2f));
            bannerGroup.alpha=confettiGroup.alpha=alpha;
            foreach(var icon in bannerIcons)icon.SetFloat("_Alpha",alpha);
            for(int i=0;i<confetti.Count;i++)
            {
                var motion=confettiMotion[i];
                float sway=victory?Mathf.Sin(t*4+i)*30:Mathf.Sin(t*1.5f+i)*12;
                confetti[i].anchoredPosition+=new Vector2(motion.x+sway,motion.y)*Time.deltaTime;
                confetti[i].localRotation=Quaternion.Euler(0,0,motion.z*t);
            }
        }

        static float EaseOutBack(float x){const float c1=1.70158f,c3=c1+1;return 1+c3*Mathf.Pow(x-1,3)+c1*Mathf.Pow(x-1,2);}

        // Short tone sequence generated in code, like the vacuum sounds (rising = win, falling = lose).
        static AudioClip Tune(string name,float[] notes,float step,float tail)
        {
            const int rate=44100;
            int length=(int)(rate*(step*(notes.Length-1)+tail));var data=new float[length];
            for(int n=0;n<notes.Length;n++)
            {
                int start=(int)(rate*step*n),count=(int)(rate*(n==notes.Length-1?tail:step+.08f));
                for(int i=0;i<count&&start+i<length;i++){float time=(float)i/rate;data[start+i]+=Mathf.Sin(2*Mathf.PI*notes[n]*time)*Mathf.Exp(-time*5)*.35f;}
            }
            var clip=AudioClip.Create(name,length,1,rate,false);clip.SetData(data,0);return clip;
        }

        void LateUpdate()
        {
            if(!game||!viewCamera)return;
            if(!hurtVignette)BuildHurt();
            AnimateHurt();
            AnimateCelebration();
            // Desktop: fit the whole camera view. Headset: keep the HUD in the central, readable
            // part of the lenses (the 16:9 layout spans headsetVerticalDegrees high at 2 m).
            float scale;
            if(UnityEngine.XR.XRSettings.isDeviceActive)
                scale=4*Mathf.Tan(headsetVerticalDegrees*.5f*Mathf.Deg2Rad)/720;
            else
            {
                float h=4*Mathf.Tan(viewCamera.fieldOfView*.5f*Mathf.Deg2Rad);
                scale=Mathf.Min(h/720,h*viewCamera.aspect/1280)*.93f;
            }
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
            healthFill.rectTransform.sizeDelta=new Vector2(249*Mathf.Clamp01(game.health/game.maxHealth),38);
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

