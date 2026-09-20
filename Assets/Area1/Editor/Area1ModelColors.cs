using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SlimeRancher.Area1.Editor
{
 public static class Area1ModelColors
 {
  const string Folder="Assets/Area1/OBJ";
  static Color C(float r,float g,float b)=>new Color(r,g,b);
  static float Oval(Vector3 p,float x,float y,float rx,float ry)=>Mathf.Pow((p.x-x)/rx,2)+Mathf.Pow((p.y-y)/ry,2);
  static Color Paint(string id,Vector3 p,Vector3 normal)
  {
   float y=(p.y+1)*.5f;
   if(id=="Zanahoria"){
    if(p.y>.34f)return Color.Lerp(C(.08f,.29f,.015f),C(.45f,.85f,.06f),Mathf.Clamp01((p.y-.34f)/.66f));
    float rings=Mathf.Pow(.5f+.5f*Mathf.Sin(p.y*24),18)*.17f;
    return Color.Lerp(C(.88f,.16f,.015f),C(1,.53f,.045f),y*.8f+normal.y*.1f)*(1-rings);
   }
   if(id=="Pollo"||id=="PolloViejo"){
    bool old=id=="PolloViejo";
    Color c=Color.Lerp(old?C(.42f,.30f,.19f):C(.88f,.7f,.4f),old?C(.86f,.79f,.62f):C(1,.97f,.81f),Mathf.Clamp01(y+.12f));
    if(p.z<-.3f&&p.y>.15f)c=old?C(.34f,.26f,.22f):C(.75f,.37f,.13f);
    if(p.y<(old?-.93f:-.82f))c=C(1,.56f,.04f);
    if(p.y>.6f&&p.z>-.1f&&Mathf.Abs(p.x)<.55f)c=old?C(.6f,.12f,.1f):C(.94f,.07f,.09f);
    if(p.z>(old?.40f:.48f)&&Mathf.Abs(p.x)<.19f&&p.y>(old?-.86f:-.62f)&&p.y<(old?-.4f:-.24f))c=C(.83f,.06f,.08f);
    if(p.z>(old?.43f:.57f)&&Mathf.Abs(p.x)<.24f&&p.y>(old?-.4f:-.24f)&&p.y<(old?-.06f:.02f))c=C(1,.56f,.025f);
    if(p.z>(old?.2f:.4f)&&(Oval(p,.32f,.20f,.135f,old?.055f:.14f)<1||Oval(p,-.32f,.20f,.135f,old?.055f:.14f)<1))c=C(.045f,.035f,.032f);
    if(!old&&p.z>.4f&&(Oval(p,.29f,.24f,.035f,.04f)<1||Oval(p,-.35f,.24f,.035f,.04f)<1))c=Color.white;
    return c;
   }
   if(id=="SlimeAzul"){
    Color c=Color.Lerp(C(.025f,.17f,.65f),C(.12f,.79f,.98f),y);
    if(p.y>.28f)c=Color.Lerp(c,C(.58f,.91f,1),Mathf.SmoothStep(0,1,(p.y-.28f)/.72f));
    if(p.z>.4f&&(Oval(p,.52f,-.05f,.12f,.1f)<1||Oval(p,-.52f,-.05f,.12f,.1f)<1))c=C(.015f,.045f,.12f);
    if(p.z>.55f&&Oval(p,0,-.3f,.22f,.13f)<1)c=C(.03f,.055f,.19f);
    if(p.z>.55f&&Oval(p,0,-.36f,.13f,.04f)<1)c=C(.98f,.25f,.5f);
    return c;
   }
   if(id=="SlimeEnemigo"){
    float swirl=Mathf.Sin(p.x*15+p.y*11+Mathf.Sin(p.z*10)*2);
    Color c=Color.Lerp(C(.025f,.012f,.045f),C(.22f,.045f,.32f),y);
    if(swirl>.91f)c=Color.Lerp(C(.36f,.09f,.55f),C(.08f,.75f,.7f),(p.y+1)*.5f);
    if(p.z>.45f&&(Oval(p,.37f,.28f,.16f,.12f)<1||Oval(p,-.37f,.28f,.16f,.12f)<1))c=C(1,.62f,.035f);
    if(p.z>.5f&&Mathf.Abs(p.x)<.6f&&Mathf.Abs(p.y-(-.18f+.1f*Mathf.Cos(p.x*10)))<.06f)c=C(.95f,.83f,.39f);
    return c;
   }
   if(id=="Plort")return Color.Lerp(C(.64f,.025f,.26f),C(1,.55f,.76f),y)*(.85f+.15f*Mathf.Abs(normal.x));
   // Vacuum: painted housing, dark grip, cream barrel rings and a cyan intake.
   Color col=C(.12f,.55f,.63f);
   if(p.y<-.28f)col=C(.045f,.085f,.13f);
   if(p.x>.38f)col=C(.89f,.85f,.67f);
   if(p.x>.8f)col=C(.08f,.19f,.25f);
   if(p.x>.88f&&normal.x<-.15f)col=C(.08f,.87f,.98f);
   if((Mathf.Abs(p.x-.23f)<.045f||Mathf.Abs(p.x+.22f)<.05f)&&p.y>-.18f)col=C(.98f,.58f,.12f);
   if(p.x<-.57f)col=C(.92f,.37f,.09f);
   return col;
  }
  public static string Identify(string path)
  {
   if(path.Contains("ASPIRADORA"))return "Aspiradora";
   if(path.Contains("POLLO VIEJO"))return "PolloViejo";
   if(path.Contains("POLLO OBJ"))return "Pollo";
   if(path.Contains("SLIME AZUL"))return "SlimeAzul";
   if(path.Contains("SLIME ENEMIGO"))return "SlimeEnemigo";
   if(path.Contains("Zanahoria"))return "Zanahoria";
   if(path.Contains("Plort/"))return "Plort";
   return null;
  }
  public static Mesh ColoredMesh(Mesh source,string id)
  {
   string path=Folder+"/"+id+"_Coloreado.asset";
   var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
   var copy=UnityEngine.Object.Instantiate(source);copy.name=id+" Coloreado";
   var b=source.bounds;var vertices=source.vertices;var normals=source.normals;var colors=new Color[vertices.Length];
   for(int i=0;i<vertices.Length;i++){
    var p=vertices[i]-b.center;p=new Vector3(p.x/b.extents.x,p.y/b.extents.y,p.z/b.extents.z);
    colors[i]=Paint(id,p,normals.Length==vertices.Length?normals[i]:Vector3.up);
   }
   copy.colors=colors;
   if(mesh){EditorUtility.CopySerialized(copy,mesh);UnityEngine.Object.DestroyImmediate(copy);}else{mesh=copy;AssetDatabase.CreateAsset(mesh,path);}
   mesh.colors=colors;mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);return mesh;
  }
  public static Material ColoredMaterial(string id)
  {
   string path=Folder+"/"+id+"_Coloreado.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!mat){mat=new Material(Shader.Find("Area1/Colores VR"));AssetDatabase.CreateAsset(mat,path);}
   mat.shader=Shader.Find("Area1/Colores VR");mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",id.Contains("Slime")||id=="Plort"?.65f:.25f);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
  }
  public static void ApplyTo(GameObject root)
  {
   foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true)){
    if(!filter.sharedMesh)continue;
    string path=AssetDatabase.GetAssetPath(filter.sharedMesh);string id=Identify(path);
    if(id==null){foreach(var key in new[]{"Aspiradora","PolloViejo","Pollo","SlimeAzul","SlimeEnemigo","Zanahoria","Plort"})if(path.EndsWith("/"+key+"_Coloreado.asset")){id=key;break;}}
    if(id==null)continue;
    filter.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+id+"_Coloreado.asset");
    var renderer=filter.GetComponent<MeshRenderer>();renderer.sharedMaterials=Enumerable.Repeat(ColoredMaterial(id),filter.sharedMesh.subMeshCount).ToArray();
    EditorUtility.SetDirty(filter);EditorUtility.SetDirty(renderer);
    if(PrefabUtility.IsPartOfPrefabInstance(filter)){PrefabUtility.RecordPrefabInstancePropertyModifications(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
   }
  }
  [MenuItem("Area1/Aplicar colores a modelos")]
  public static void Apply()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(Application.isPlaying||scene.path!="Assets/00_Scenes/AREA1.unity")throw new Exception("Abre AREA1 fuera de Play.");
   foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/04_Models/")&&p.EndsWith(".obj")).ToArray()){
    var id=Identify(path);if(id==null)continue;
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<MeshFilter>().sharedMesh;ColoredMesh(source,id);ColoredMaterial(id);
   }
   var game=UnityEngine.Object.FindAnyObjectByType<SlimeRancherVR.RanchGame>();
   foreach(var data in game.catalog){
    var path=AssetDatabase.GetAssetPath(data.prefab);var root=PrefabUtility.LoadPrefabContents(path);
    try{ApplyTo(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
   }
   foreach(var root in scene.GetRootGameObjects())ApplyTo(root);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
 }
}
