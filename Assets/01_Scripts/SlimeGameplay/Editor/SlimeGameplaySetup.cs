using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SlimeRancherVR;
[InitializeOnLoad]
public static class SlimeGameplaySetup {
 const string Folder="Assets/02_Prefabs/SlimeGameplay/";
 const string Models="Assets/04_Models/";
 const string Out="C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/";
 static double next;
 static SlimeGameplaySetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.5;if(!File.Exists("Temp/SlimeSetup.request"))return;var cmd=File.ReadAllText("Temp/SlimeSetup.request").Trim();File.Delete("Temp/SlimeSetup.request");try{if(cmd=="polish")Polish();else Install();}catch(Exception e){File.WriteAllText(Out+"Slime_Setup_Error.txt",e.ToString());Debug.LogException(e);}}
 static Material Mat(string name,Color color,Texture texture=null){var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.25f);if(texture)m.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(m);return m;}
 static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);return bounds;}
 static void Polish(){
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/00_Scenes/INICIO.unity")throw new Exception("Se requiere INICIO.");
  foreach(var s in UnityEngine.Object.FindObjectsByType<PinkSlime>(FindObjectsSortMode.None))s.transform.rotation=Quaternion.Euler(0,90,0);
  var vacuum=UnityEngine.Object.FindAnyObjectByType<SlimeVacuum>();vacuum.inventoryText.transform.localScale=Vector3.one*.0035f;
  var ps=vacuum.demoSlimes;vacuum.demoSlimes=Array.Empty<PinkSlime>();PrefabUtility.SaveAsPrefabAsset(vacuum.playerRoot.gameObject,Models+"BeatrixVR/BeatrixXRPlayer.prefab");vacuum.demoSlimes=ps;
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Out+"Slime_Polish.txt","Face orientation and readable tool HUD saved.");
 }
 [MenuItem("Beatrix/Instalar aspiradora y slimes rosados")]
 public static void Install(){
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/00_Scenes/INICIO.unity")throw new Exception("Abre INICIO antes de instalar la jugabilidad.");
  var body=UnityEngine.Object.FindAnyObjectByType<BeatrixVRBody>();if(!body)throw new Exception("No está Beatrix en INICIO.");
  // Recover the actual embedded Blender color texture, including existing prefab users.
  AssetDatabase.ImportAsset(Models+"BeatrixVR/Models/Beatrix_BaseColor.png",ImportAssetOptions.ForceSynchronousImport);
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Models+"BeatrixVR/Models/Beatrix_BaseColor.png");
  var bm=AssetDatabase.LoadAssetAtPath<Material>(Models+"BeatrixVR/BeatrixMaterial0.mat");bm.SetColor("_BaseColor",Color.white);bm.SetTexture("_BaseMap",tex);bm.SetFloat("_Smoothness",.2f);EditorUtility.SetDirty(bm);
  foreach(var r in body.GetComponentsInChildren<SkinnedMeshRenderer>())r.sharedMaterials=Enumerable.Repeat(bm,r.sharedMaterials.Length).ToArray();
  if(UnityEngine.Object.FindAnyObjectByType<SlimeVacuum>()){AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);File.WriteAllText(Out+"Slime_Setup.txt","Existing gameplay preserved; Beatrix texture restored.");return;}
  var data=AssetDatabase.LoadAssetAtPath<SlimeData>(Folder+"SlimeRosadoData.asset");if(!data){data=ScriptableObject.CreateInstance<SlimeData>();AssetDatabase.CreateAsset(data,Folder+"SlimeRosadoData.asset");}
  var tool=new GameObject("Aspiradora de Beatrix");tool.transform.SetParent(body.rightTarget,false);tool.transform.localPosition=new Vector3(0,-.035f,.16f);
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(Models+"Aspiradora/Aspiradora_VR.fbx");if(!source)throw new Exception("Falta Aspiradora_VR.fbx");
  var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);visual.name="Modelo Aspiradora";visual.transform.SetParent(tool.transform,false);visual.transform.localPosition=new Vector3(0,.075f,.05f);
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Models+"Aspiradora/futuristic-toy-ray-gun-3d-model-thmoiwrojeo_Material_01_BaseColor.png");var gunMat=Mat("Aspiradora",Color.white,texture);
  foreach(var r in visual.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(gunMat,r.sharedMaterials.Length).ToArray();
  var muzzle=new GameObject("Boquilla").transform;muzzle.SetParent(tool.transform,false);muzzle.localPosition=new Vector3(0,.075f,.38f);
  var vac=tool.AddComponent<SlimeVacuum>();vac.data=data;vac.muzzle=muzzle;vac.playerRoot=body.trackingOrigin;
  var beamGO=new GameObject("Flujo de aspiracion");beamGO.transform.SetParent(tool.transform,false);var beam=beamGO.AddComponent<LineRenderer>();beam.positionCount=2;beam.useWorldSpace=true;beam.enabled=false;beam.numCapVertices=4;
  var beamMat=Mat("Suction",new Color(.2f,.95f,1));beamMat.shader=Shader.Find("Universal Render Pipeline/Unlit");beam.sharedMaterial=beamMat;beam.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;vac.stream=beam;
  var hud=new GameObject("Inventario VR");hud.transform.SetParent(tool.transform,false);hud.transform.localPosition=new Vector3(0,.22f,.015f);hud.transform.localRotation=Quaternion.Euler(30,0,0);hud.transform.localScale=Vector3.one*.0015f;
  var text=hud.AddComponent<TextMesh>();text.fontSize=48;text.characterSize=1;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=Color.white; text.text="ROSADOS  0 / 20";vac.inventoryText=text;
  var slimeSource=AssetDatabase.LoadAssetAtPath<GameObject>(Models+"SlimeRosado.fbx");if(!slimeSource)throw new Exception("Falta SlimeRosado.fbx");
  var root=new GameObject("Slimes rosados - zona inicial");
  var slimeRoot=new GameObject("Slime Rosado");var model=(GameObject)PrefabUtility.InstantiatePrefab(slimeSource,scene);model.transform.SetParent(slimeRoot.transform,false);
  foreach(var c in model.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
  var bounds=BoundsOf(model);model.transform.localScale*=.55f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);bounds=BoundsOf(model);model.transform.position-=bounds.center;
  var pink=Mat("SlimeRosado",data.color);pink.SetFloat("_Smoothness",.65f);var face=Mat("SlimeCara",new Color(.025f,.01f,.03f));
  foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name.Contains("Parts")?face:pink).ToArray();
  var rb=slimeRoot.AddComponent<Rigidbody>();rb.mass=.5f;rb.linearDamping=.8f;rb.angularDamping=2;rb.constraints=RigidbodyConstraints.FreezeRotation;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
  var col=slimeRoot.AddComponent<SphereCollider>();col.radius=.265f;
  var slime=slimeRoot.AddComponent<PinkSlime>();slime.data=data;slime.visual=model.transform;
  var prefab=PrefabUtility.SaveAsPrefabAsset(slimeRoot,Folder+"SlimeRosado.prefab");UnityEngine.Object.DestroyImmediate(slimeRoot);
  var list=new System.Collections.Generic.List<PinkSlime>();
  Vector3 origin=body.trackingOrigin.position;
  for(int i=0;i<8;i++){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.transform.SetParent(root.transform);go.transform.position=origin+new Vector3((i%4-1.5f)*1.15f,.3f,2.5f+i/4*1.6f);go.transform.rotation=Quaternion.Euler(0,180,0);list.Add(go.GetComponent<PinkSlime>());}
  vac.demoSlimes=list.ToArray();
  // Low barriers keep the test population near the player and provide real occluders.
  var wallMat=Mat("Corral",new Color(.13f,.35f,.3f));
  foreach(var spec in new[]{new Vector4(0,.35f,6.2f,0),new Vector4(-3.4f,.35f,3.2f,1),new Vector4(3.4f,.35f,3.2f,1)}){
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Barrera corral";wall.transform.SetParent(root.transform);wall.transform.position=origin+new Vector3(spec.x,spec.y,spec.z);wall.transform.localScale=spec.w==0?new Vector3(7,.7f,.15f):new Vector3(.15f,.7f,6);wall.GetComponent<Renderer>().sharedMaterial=wallMat;
  }
  PrefabUtility.SaveAsPrefabAsset(tool,Folder+"Aspiradora.prefab");
  // The complete player prefab owns all controller references; scene population stays in INICIO.
  var savedSlimes=vac.demoSlimes;vac.demoSlimes=Array.Empty<PinkSlime>();PrefabUtility.SaveAsPrefabAsset(body.trackingOrigin.gameObject,Models+"BeatrixVR/BeatrixXRPlayer.prefab");vac.demoSlimes=savedSlimes;
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.52f,.55f);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText(Out+"Slime_Setup.txt","INICIO saved\nBeatrix original color texture restored\nVacuum model: "+source.name+"\nGun bounds: "+BoundsOf(visual).size+"\nPink slimes: "+list.Count+"\nRange: "+data.suctionRange+"\nCapacity: "+data.capacity+"\n");
 }
}


