using UnityEngine;
using TMPro;
using SlimeRancherVR;

namespace SlimeRancher.Area1
{
    [DefaultExecutionOrder(14000)]
    public sealed class Area1GunInventory : MonoBehaviour
    {
        public TMP_FontAsset font;
        SlimeVacuum vacuum;
        VacuumPickup pickup;
        Area1WaterVacuum water;
        RectTransform panel;
        readonly TMP_Text[] labels = new TMP_Text[5];
        readonly UnityEngine.UI.Image[] borders = new UnityEngine.UI.Image[5];
        readonly Transform[] models = new Transform[4];
        readonly RanchItemData[] shown = new RanchItemData[4];
        TMP_Text hintLabel;

        RectTransform Rect(string title, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(title, typeof(RectTransform));
            go.layer = 5;
            var r = go.GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.sizeDelta = size; r.anchoredPosition = position; return r;
        }
        UnityEngine.UI.Image Box(string title, Transform parent, Vector2 size, Vector2 position, Color color)
        {
            var image = Rect(title, parent, size, position).gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        TMP_Text Text(string title, Transform parent, Vector2 size, Vector2 position, float fontSize)
        {
            var text = Rect(title, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white; text.raycastTarget = false; return text;
        }
        void Awake()
        {
            vacuum = GetComponent<SlimeVacuum>(); pickup = GetComponent<VacuumPickup>();
            water = GetComponent<Area1WaterVacuum>();
            panel = Rect("Inventario de la aspiradora", transform, new Vector2(630, 245), Vector2.zero);
            var canvas = panel.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            Box("Fondo", panel, new Vector2(630,245), Vector2.zero, new Color(.025f,.045f,.065f,.97f));
            Text("Titulo",panel,new Vector2(610,30),new Vector2(0,102),22).text="INVENTARIO";
            for(int i=0;i<5;i++)
            {
                var card=Rect("Ranura "+(i+1),panel,new Vector2(112,150),new Vector2(-240+i*120,8));
                borders[i]=Box("Borde",card,new Vector2(112,150),Vector2.zero,Color.white);
                Box("Interior",card,new Vector2(106,144),Vector2.zero,i==4?new Color(.025f,.19f,.27f):new Color(.055f,.085f,.12f));
                Text("Numero",card,new Vector2(28,24),new Vector2(-37,59),16).text=(i+1).ToString();
                labels[i]=Text("Contenido",card,new Vector2(102,70),new Vector2(0,-38),i==4?19:17);
                if(i<4)
                {
                    models[i]=new GameObject("Miniatura").transform; models[i].SetParent(card,false);
                    models[i].localPosition=new Vector3(0,31,-45);
                }
                else
                {
                    var waterIcon=Box("Icono de agua",card,new Vector2(62,48),new Vector2(0,34),new Color(.08f,.7f,1f));
                    waterIcon.rectTransform.localRotation=Quaternion.Euler(0,0,45);
                }
            }
            hintLabel=Text("Ayuda",panel,new Vector2(610,34),new Vector2(0,-92),20);
        }
        // Copy geometry only: never instantiate item scripts, colliders, or rigidbodies.
        void CopyVisual(Transform source, Transform parent)
        {
            var node=new GameObject(source.name).transform; node.SetParent(parent,false);
            node.localPosition=source.localPosition; node.localRotation=source.localRotation; node.localScale=source.localScale;
            var filter=source.GetComponent<MeshFilter>(); var renderer=source.GetComponent<MeshRenderer>();
            if(filter && renderer && renderer.enabled && filter.sharedMesh)
            {
                node.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var copy=node.gameObject.AddComponent<MeshRenderer>(); copy.sharedMaterials=renderer.sharedMaterials;
                copy.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; copy.receiveShadows=false;
            }
            foreach(Transform child in source) if(child.gameObject.activeSelf) CopyVisual(child,node);
        }
        void RefreshModel(int index,RanchItemData data)
        {
            foreach(Transform child in models[index]) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            shown[index]=data;
            if(!data || !data.prefab) return;
            var fit=new GameObject("Modelo visual").transform; fit.SetParent(models[index],false);
            CopyVisual(data.prefab.transform,fit);
            var renderers=fit.GetComponentsInChildren<MeshRenderer>(); if(renderers.Length==0) return;
            // Calculate bounds in the miniature's local coordinates (independent of gun pose).
            var bounds=new Bounds(); bool first=true;
            foreach(var r in renderers)
            {
                var b=r.GetComponent<MeshFilter>().sharedMesh.bounds;
                for(int n=0;n<8;n++)
                {
                    var p=b.center+Vector3.Scale(b.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1));
                    p=fit.InverseTransformPoint(r.transform.TransformPoint(p));
                    if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);
                }
            }
            float scale=82/Mathf.Max(.001f,Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z)));
            fit.localScale=Vector3.one*scale;
            fit.localPosition=-bounds.center*scale;
            // Item prefabs face away from the world-space inventory camera by default.
            fit.localRotation=Quaternion.Euler(0,180,0);
        }
        void LateUpdate()
        {
            var game=RanchGame.Instance; var camera=Camera.main;
            if(!game || !camera) return;
            panel.gameObject.SetActive(true);
            // Fixed physical size, above the gun, facing the headset for readability.
            panel.position=transform.position+Vector3.up*.24f;
            panel.rotation=Quaternion.LookRotation(panel.position-camera.transform.position,camera.transform.up);
            var s=transform.lossyScale;
            panel.localScale=new Vector3(.0008f/s.x,.0008f/s.y,.0008f/s.z);
            for(int i=0;i<4;i++)
            {
                var slot=game.slots[i]; var data=slot.count>0?game.Data(slot.kind):null;
                if(shown[i]!=data) RefreshModel(i,data);
                labels[i].text=data?data.itemName+"\n<size=26><b>× "+slot.count+"</b></size>":"VACÍO";
                borders[i].color=i==game.selected&&(!water||!water.WaterSelected)?new Color(1,.74f,.16f):new Color(.3f,.7f,.8f);
            }
            if(water)
            {
                labels[4].text="AGUA\n<size=23><b>"+water.Amount+" / "+water.capacity+"</b></size>";
                borders[4].color=water.WaterSelected?new Color(1,.74f,.16f):new Color(.08f,.7f,1f);
            }
            else
            {
                labels[4].text="AGUA\nNO DISPONIBLE";
                borders[4].color=new Color(.3f,.45f,.5f);
            }
            hintLabel.text="A / B: cambiar ranura  ·  PC: tecla 5 para agua";
        }
    }
}
