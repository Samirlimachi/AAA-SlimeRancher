using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SlimeRancherVR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace SlimeRancher.Area1.Editor
{
 public static class Area1OBJIntegration
 {
  const string Folder = "Assets/Area1/OBJ";
  static string Model(string fragment) => AssetDatabase.GetAllAssetPaths().First(p=>p.StartsWith("Assets/04_Models/") && p.EndsWith(".obj") && p.Contains(fragment));
  static Material MaterialFor(string id, Color color, string textureFragment=null)
  {
   string path=Folder+"/"+id+".mat";
   var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
   mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.25f);mat.enableInstancing=true;
   if(textureFragment!=null){
    string tex=AssetDatabase.GetAllAssetPaths().FirstOrDefault(p=>p.StartsWith("Assets/04_Models/")&&p.EndsWith(".png")&&p.Contains(textureFragment));
    if(tex!=null){mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));mat.SetColor("_BaseColor",Color.white);}
   }
   EditorUtility.SetDirty(mat);return mat;
  }
  static Transform Visual(GameObject root,string source,float size,Material mat,Vector3 rotation,Vector3 center)
  {
   var holder=new GameObject("Modelo OBJ").transform;holder.SetParent(root.transform,false);
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source));
   model.transform.SetParent(holder,false);model.transform.localRotation=Quaternion.Euler(rotation);
   foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=Enumerable.Repeat(mat,Math.Max(1,r.sharedMaterials.Length)).ToArray();
   var filters=model.GetComponentsInChildren<MeshFilter>(true);
   var bounds=new Bounds();bool first=true;
   foreach(var f in filters) {
    if(!f.sharedMesh)continue;var b=f.sharedMesh.bounds;
    for(int i=0;i<8;i++){var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
     p=holder.InverseTransformPoint(f.transform.TransformPoint(p));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
   }
   if(first)throw new Exception("Modelo sin malla: "+source);
   float scale=size/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
   model.transform.localScale*=scale;model.transform.localPosition=center-bounds.center*scale;
   return holder;
  }
  static void ReplaceItem(GameObject root,string model,float size,Material mat)
  {
   // Keep the gameplay root and its colliders, replacing only the visual children.
   foreach(Transform child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
   var visual=Visual(root,model,size,mat,Vector3.zero,Vector3.zero);
   var slime=root.GetComponent<PinkSlime>();if(slime)slime.visual=visual;
   var grab=root.GetComponent<XRGrabInteractable>();if(!grab)grab=root.AddComponent<XRGrabInteractable>();
   grab.colliders.Clear();grab.colliders.AddRange(root.GetComponents<Collider>());
   if(!root.GetComponent<Area1GrabbableItem>())root.AddComponent<Area1GrabbableItem>();
   EditorUtility.SetDirty(root);
  }
  [MenuItem("Area1/Integrar modelos OBJ en VR")]
  public static void Apply()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(scene.path!="Assets/00_Scenes/AREA1.unity"||Application.isPlaying)throw new Exception("Abre AREA1 fuera de Play.");
   Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
   var game=UnityEngine.Object.FindFirstObjectByType<RanchGame>();
   var kinds=new[]{RanchItemKind.PinkSlime,RanchItemKind.Carrot,RanchItemKind.PinkPlort,RanchItemKind.Chicken,RanchItemKind.ElderChicken};
   var models=new[]{Model("SLIME ROSA/"),Model("Zanahoria/"),Model("Plort/"),Model("POLLO OBJ/"),Model("POLLO VIEJO/")};
   var sizes=new[]{.64f,.4f,.3f,.5f,.5f};
   var mats=new[]{MaterialFor("Rosa",new Color(1,.3f,.55f),"Cheerful_Pink_Blob"),MaterialFor("Zanahoria",new Color(1,.32f,.025f)),MaterialFor("Plort",new Color(1,.25f,.6f)),MaterialFor("Pollo",new Color(.95f,.86f,.62f)),MaterialFor("PolloViejo",new Color(.55f,.46f,.35f))};
   for(int i=0;i<kinds.Length;i++){
    var data=game.Data(kinds[i]);string path=AssetDatabase.GetAssetPath(data.prefab);
    var prefab=PrefabUtility.LoadPrefabContents(path);
    try{ReplaceItem(prefab,models[i],sizes[i],mats[i]);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
    foreach(var item in UnityEngine.Object.FindObjectsByType<RanchItem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x.gameObject.scene==scene&&x.data==data).ToArray()){
     if(PrefabUtility.IsPartOfPrefabInstance(item.gameObject))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(item.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
     ReplaceItem(item.gameObject,models[i],sizes[i],mats[i]);
    }
   }
   foreach(var kind in new[]{RanchItemKind.BlueSlime,RanchItemKind.EnemySlime}){
    bool enemy=kind==RanchItemKind.EnemySlime;
    string id=enemy?"SlimeEnemigo":"SlimeAzul";var data=game.Data(kind);
    if(!data){data=ScriptableObject.CreateInstance<RanchItemData>();data.kind=kind;AssetDatabase.CreateAsset(data,Folder+"/"+id+".asset");game.catalog=game.catalog.Concat(new[]{data}).ToArray();}
    data.itemName=enemy?"Slime enemigo":"Slime azul";data.radius=.32f;data.color=enemy?new Color(.16f,.045f,.22f):new Color(.08f,.45f,1);
    var source=game.Data(RanchItemKind.PinkSlime).prefab;
    var root=UnityEngine.Object.Instantiate(source);root.name=id;root.GetComponent<RanchItem>().data=data;
    ReplaceItem(root,Model(enemy?"SLIME ENEMIGO/":"SLIME AZUL/"),.64f,MaterialFor(id,data.color));
    if(enemy)root.AddComponent<Area1EnemySlime>();
    data.prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+id+".prefab");UnityEngine.Object.DestroyImmediate(root);EditorUtility.SetDirty(data);
    if(!UnityEngine.Object.FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Any(x=>x.data==data)){
     var instance=(GameObject)PrefabUtility.InstantiatePrefab(data.prefab);instance.transform.SetParent(game.worldRoot,false);instance.transform.position=enemy?new Vector3(4,.4f,4):new Vector3(0,.4f,4);
    }
   }
   if(!UnityEngine.Object.FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Any(x=>x.data.kind==RanchItemKind.PinkPlort)){
    var plort=(GameObject)PrefabUtility.InstantiatePrefab(game.Data(RanchItemKind.PinkPlort).prefab);plort.transform.SetParent(game.worldRoot,false);plort.transform.position=new Vector3(3.5f,.3f,-.4f);
   }
   var vac=UnityEngine.Object.FindFirstObjectByType<SlimeVacuum>();
   foreach(Transform child in vac.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Modelo")).ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
   Visual(vac.gameObject,Model("ASPIRADORA OBJ/"),.65f,MaterialFor("Aspiradora",new Color(.15f,.65f,.72f)),new Vector3(0,90,0),new Vector3(0,.075f,.05f));
   vac.muzzle=vac.transform.Find("Boquilla");vac.playerRoot=game.player;vac.stream=vac.GetComponentInChildren<LineRenderer>(true);vac.inventoryText=vac.GetComponentInChildren<TextMesh>(true);
   var beatrixMat=MaterialFor("Beatrix",Color.white,"_BaseColor");
   var body=UnityEngine.Object.FindFirstObjectByType<BeatrixVRBody>();
   if(body){
    var bodyMat=MaterialFor("TrackedBody",Color.white); var original=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/TrackedBodyBaseColor.png"); if(original)bodyMat.SetTexture("_BaseMap",original);
    foreach(var r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { if(r.sharedMesh)r.sharedMaterial=bodyMat; else { if(body.headRenderer==r)body.headRenderer=null; UnityEngine.Object.DestroyImmediate(r); } }
    if(body.animator&&!body.animator.runtimeAnimatorController){
     string controllerPath=Folder+"/Beatrix.controller";var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
     if(!controller){controller=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);}
     body.animator.runtimeAnimatorController=controller;
    }
   }
   var display=GameObject.Find("Beatrix OBJ - referencia");
   if(!display){display=new GameObject("Beatrix OBJ - referencia");display.transform.position=new Vector3(4,0,1.5f);Visual(display,Model("BEATRIX OBJ/"),1.65f,beatrixMat,new Vector3(0,90,0),new Vector3(0,.825f,0));var c=display.AddComponent<CapsuleCollider>();c.height=1.65f;c.radius=.25f;c.center=new Vector3(0,.825f,0);}
   EditorUtility.SetDirty(game);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Area1ModelColors.Apply();
  }
 }
}
