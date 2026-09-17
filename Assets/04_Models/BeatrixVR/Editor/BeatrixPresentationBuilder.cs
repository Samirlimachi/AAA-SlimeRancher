using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SlimeRancherVR;
[InitializeOnLoad]
public static class BeatrixPresentationBuilder {
 const string B="Assets/04_Models/BeatrixVR/";
 static BeatrixPresentationBuilder(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/BeatrixShowcase.request"))return;File.Delete("Temp/BeatrixShowcase.request");try{Build();}catch(Exception e){File.WriteAllText("C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/Showcase_Error.txt",e.ToString());Debug.LogException(e);}}
 [MenuItem("Beatrix/Crear escena para presentar")]
 public static void Build(){
  string path="Assets/00_Scenes/AVANCE_BEATRIX.unity";
  if(File.Exists(path)){EditorSceneManager.OpenScene(path);return;}
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
  RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.60f,.68f);RenderSettings.skybox=null;
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(B+"BeatrixBody.prefab");var ch=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);
  PrefabUtility.UnpackPrefabInstance(ch,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  var body=ch.GetComponent<BeatrixVRBody>();if(body)UnityEngine.Object.DestroyImmediate(body);
  ch.transform.SetPositionAndRotation(new Vector3(.44f,0,0),Quaternion.Euler(0,165,0));
  foreach(var r in ch.GetComponentsInChildren<SkinnedMeshRenderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
  var control=new GameObject("Controles de presentación").AddComponent<BeatrixShowcase>();control.character=ch.transform;control.animator=ch.GetComponent<Animator>();
  var cam=new GameObject("Presentation Camera").AddComponent<Camera>();cam.transform.position=new Vector3(0,.94f,-3.5f);cam.transform.rotation=Quaternion.identity;cam.orthographic=true;cam.orthographicSize=1.14f;cam.backgroundColor=new Color(.035f,.065f,.10f);cam.clearFlags=CameraClearFlags.SolidColor;cam.nearClipPlane=.03f;cam.gameObject.AddComponent<AudioListener>();
  var light=new GameObject("Luz principal").AddComponent<Light>();light.type=LightType.Directional;light.intensity=2.3f;light.transform.rotation=Quaternion.Euler(35,-30,0);
  var fill=new GameObject("Luz de contorno").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=1.0f;fill.color=new Color(.47f,.74f,1);fill.transform.rotation=Quaternion.Euler(25,150,0);
  var pedestal=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pedestal.name="Base de exposición";pedestal.transform.position=new Vector3(.44f,-.055f,0);pedestal.transform.localScale=new Vector3(1.2f,.05f,1.2f);var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.08f,.22f,.25f));AssetDatabase.CreateAsset(material,B+"PresentationBase.mat");pedestal.GetComponent<Renderer>().sharedMaterial=material;
  EditorSceneManager.SaveScene(scene,path);AssetDatabase.SaveAssets();EditorSceneManager.CloseScene(scene,true);EditorSceneManager.OpenScene(path);
  Selection.activeGameObject=GameObject.Find("Beatrix Body");
  File.WriteAllText("C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/Showcase_Ready.txt",path);
 }
}
