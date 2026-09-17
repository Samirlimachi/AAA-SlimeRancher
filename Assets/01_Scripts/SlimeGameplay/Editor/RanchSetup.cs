using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SlimeRancherVR;
[InitializeOnLoad]
public static class RanchSetup {
 const string Folder="Assets/02_Prefabs/SlimeGameplay/";
 const string Out="C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/";
 static RanchSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(EditorApplication.isPlaying&&File.Exists("Temp/RanchCapture.request")){File.Delete("Temp/RanchCapture.request");ScreenCapture.CaptureScreenshot(Out+"INICIO_Rancho.png");}
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/RanchPolish.request")){File.Delete("Temp/RanchPolish.request");var game=UnityEngine.Object.FindAnyObjectByType<RanchGame>();if(game){foreach(var text in game.GetComponentsInChildren<TextMesh>()){text.transform.localRotation=Quaternion.identity;if(!text.transform.Find("Fondo del cartel"))Shape("Fondo del cartel",PrimitiveType.Cube,text.transform,new Vector3(0,0,.15f),new Vector3(48,14,.15f),Mat("Carteles",new Color(.035f,.09f,.13f)),false);}EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}}
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/RanchSetup.request")){File.Delete("Temp/RanchSetup.request");try{Install();}catch(Exception e){File.WriteAllText(Out+"Ranch_Setup.txt","FAIL: "+e);Debug.LogException(e);}}
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/RanchTest.request")){File.Delete("Temp/RanchTest.request");SessionState.SetBool("RanchValidation",true);EditorApplication.isPlaying=true;}
  if(EditorApplication.isPlaying&&SessionState.GetBool("RanchValidation",false)&&!UnityEngine.Object.FindAnyObjectByType<RanchValidation>())new GameObject("Ranch integration validation").AddComponent<RanchValidation>();
 }
 static Material Mat(string name,Color color){string path=Folder+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.3f);EditorUtility.SetDirty(mat);return mat;}
 static GameObject Shape(string name,PrimitiveType shape,Transform parent,Vector3 position,Vector3 scale,Material material,bool collider=true){var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
 static void Sign(string message,Transform parent,Vector3 position,float size=.065f){var go=new GameObject(message);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,180,0);go.transform.localScale=Vector3.one*size;var text=go.AddComponent<TextMesh>();text.text=message;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.characterSize=.5f;text.fontSize=60;text.color=Color.white;}
 static RanchItemData Data(RanchItemKind kind,string name,Color color,float radius,int price){string path=Folder+kind+"Item.asset";var data=AssetDatabase.LoadAssetAtPath<RanchItemData>(path);if(!data){data=ScriptableObject.CreateInstance<RanchItemData>();AssetDatabase.CreateAsset(data,path);}data.kind=kind;data.itemName=name;data.color=color;data.radius=radius;data.saleValue=price;return data;}
 [MenuItem("Beatrix/Instalar ciclo del rancho en INICIO")]
 public static void Install(){
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/00_Scenes/INICIO.unity")throw new Exception("Abre INICIO");
  if(UnityEngine.Object.FindAnyObjectByType<RanchGame>()){File.WriteAllText(Out+"Ranch_Setup.txt","Already installed; existing scene preserved.");return;}
  var vac=UnityEngine.Object.FindAnyObjectByType<SlimeVacuum>();if(!vac)throw new Exception("Falta aspiradora");
  var pink=Data(RanchItemKind.PinkSlime,"Slime rosa",new Color(1,.16f,.5f),.265f,0);var carrot=Data(RanchItemKind.Carrot,"Zanahoria",new Color(1,.43f,.06f),.18f,0);var plort=Data(RanchItemKind.PinkPlort,"Plort rosa",new Color(1,.5f,.75f),.17f,10);
  var slimePrefab=PrefabUtility.LoadPrefabContents(Folder+"SlimeRosado.prefab");var item=slimePrefab.GetComponent<RanchItem>();if(!item)item=slimePrefab.AddComponent<RanchItem>();item.data=pink;pink.prefab=PrefabUtility.SaveAsPrefabAsset(slimePrefab,Folder+"SlimeRosado.prefab");PrefabUtility.UnloadPrefabContents(slimePrefab);
  foreach(var data in new[]{carrot,plort}){
   var root=new GameObject(data.itemName);var rb=root.AddComponent<Rigidbody>();rb.mass=.25f;rb.linearDamping=.5f;rb.angularDamping=1;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;var col=root.AddComponent<SphereCollider>();col.radius=data.radius;root.AddComponent<RanchItem>().data=data;
   if(data==carrot){Shape("Raíz naranja",PrimitiveType.Capsule,root.transform,Vector3.zero,new Vector3(.19f,.18f,.19f),Mat("Zanahoria",carrot.color),false);for(int i=0;i<3;i++){var leaf=Shape("Hojas",PrimitiveType.Cube,root.transform,new Vector3((i-1)*.055f,.22f,0),new Vector3(.05f,.22f,.04f),Mat("Hojas",new Color(.23f,.62f,.12f)),false);leaf.transform.localRotation=Quaternion.Euler(0,i*45,(i-1)*-20);}}
   else {var crystal=Shape("Cristal rosa",PrimitiveType.Cube,root.transform,Vector3.zero,Vector3.one*.23f,Mat("Plort",plort.color),false);crystal.transform.localRotation=Quaternion.Euler(35,45,0);}
   data.prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+data.kind+".prefab");UnityEngine.Object.DestroyImmediate(root);
  }
  foreach(var d in new[]{pink,carrot,plort})EditorUtility.SetDirty(d);
  var systems=new GameObject("Rancho - jugabilidad");var game=systems.AddComponent<RanchGame>();game.catalog=new[]{pink,carrot,plort};game.player=vac.playerRoot;game.sun=UnityEngine.Object.FindAnyObjectByType<Light>();game.worldRoot=new GameObject("Objetos del rancho").transform;game.worldRoot.SetParent(systems.transform);
  foreach(var slime in UnityEngine.Object.FindObjectsByType<PinkSlime>(FindObjectsSortMode.None)){var ri=slime.GetComponent<RanchItem>();if(!ri)ri=slime.gameObject.AddComponent<RanchItem>();ri.data=pink;}
  var preview=UnityEngine.Object.FindAnyObjectByType<BeatrixDesktopPreview>();if(preview)preview.speed=2.6f;
  var dirt=Mat("Tierra del rancho",new Color(.56f,.32f,.16f));var trim=Mat("Terminal azul",new Color(.035f,.2f,.28f));var yellow=Mat("Terminal dorado",new Color(.95f,.6f,.08f));var green=Mat("Huerto",new Color(.24f,.38f,.14f));var wood=Mat("Cerca",new Color(.42f,.22f,.1f));
  var floor=GameObject.Find("Suelo de prueba VR");if(floor)floor.GetComponent<Renderer>().sharedMaterial=dirt;
  var market=new GameObject("Mercado de plorts - lanza aquí");market.transform.SetParent(systems.transform);market.transform.position=new Vector3(5,0,3);
  Shape("Pedestal",PrimitiveType.Cube,market.transform,new Vector3(0,.35f,0),new Vector3(2,.7f,1.3f),trim);
  Shape("Boca del mercado",PrimitiveType.Cube,market.transform,new Vector3(0,1,0),new Vector3(1.8f,.7f,.4f),yellow,false);
  var sell=market.AddComponent<BoxCollider>();sell.isTrigger=true;sell.center=new Vector3(0,1,0);sell.size=new Vector3(1.8f,.9f,.65f);market.AddComponent<RanchMarket>();
  Sign("MERCADO DE PLORTS\nLanza plorts · +10 monedas",market.transform,new Vector3(0,2,0));
  var garden=new GameObject("Huerto de zanahorias");garden.transform.SetParent(systems.transform);garden.transform.position=new Vector3(-5,0,3);game.garden=garden.AddComponent<RanchGarden>();Shape("Tierra cultivada",PrimitiveType.Cube,garden.transform,new Vector3(0,.05f,0),new Vector3(2.5f,.1f,2.2f),green);Sign("HUERTO\nNueva zanahoria cada 30 s",garden.transform,new Vector3(0,1.7f,0));
  for(int i=0;i<6;i++){var food=(GameObject)PrefabUtility.InstantiatePrefab(carrot.prefab);food.transform.SetParent(game.worldRoot);food.transform.position=garden.transform.position+new Vector3((i%3-1)*.6f,.45f,(i/3-.5f)*.7f);}
  var shop=new GameObject("Mejora de depósitos");shop.transform.SetParent(systems.transform);shop.transform.position=new Vector3(5,0,-1);game.upgradeStation=shop.transform;Shape("Terminal de mejoras",PrimitiveType.Cube,shop.transform,new Vector3(0,.55f,0),new Vector3(1,1.1f,.7f),trim);Sign("DEPÓSITOS +10\n150 monedas · E / grip izquierdo",shop.transform,new Vector3(0,1.7f,0),.05f);
  var pen=new GameObject("Corral de almacenamiento");pen.transform.SetParent(systems.transform);pen.transform.position=new Vector3(0,0,-4.5f);
  for(int side=0;side<4;side++){var wall=new GameObject("Cerca del corral");wall.transform.SetParent(pen.transform,false);wall.transform.localRotation=Quaternion.Euler(0,side*90,0);var block=wall.AddComponent<BoxCollider>();block.center=new Vector3(0,.8f,1.6f);block.size=new Vector3(3.3f,1.6f,.12f);for(int j=0;j<3;j++)Shape("Barra",PrimitiveType.Cube,wall.transform,new Vector3(0,.25f+j*.6f,1.6f),new Vector3(3.3f,.08f,.12f),wood,false);for(int j=-1;j<=1;j+=2)Shape("Poste",PrimitiveType.Cube,wall.transform,new Vector3(j*1.6f,.8f,1.6f),new Vector3(.14f,1.6f,.14f),wood,false);}
  Sign("CORRAL\nLanza slimes por encima de la cerca",pen.transform,new Vector3(0,2.1f,1.6f),.05f);
  var water=Shape("Agua peligrosa",PrimitiveType.Cube,systems.transform,new Vector3(-6,.04f,-5.5f),new Vector3(3,.08f,2.5f),Mat("Agua",new Color(.04f,.42f,.7f)),false);var waterTrigger=water.AddComponent<BoxCollider>();waterTrigger.isTrigger=true;waterTrigger.size=new Vector3(1,15,1);water.AddComponent<RanchHazard>();Sign("AGUA PROFUNDA\nPeligro",systems.transform,new Vector3(-6,1.1f,-6.5f),.06f);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Out+"Ranch_Setup.txt","PASS: INICIO saved. Four inventory slots; food, feeding, pink plorts, market, garden, corral, upgrade, health/energy/day-night, JSON save.\n");
 }
}
