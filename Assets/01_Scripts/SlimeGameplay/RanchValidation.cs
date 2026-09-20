#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;
namespace SlimeRancherVR {
public sealed class RanchValidation : MonoBehaviour {
 const string Out="C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/";
 string report="";RanchGame game;SlimeVacuum vac;
 void Check(bool pass,string label){if(!pass)throw new Exception(label);report+="PASS: "+label+"\n";}
 IEnumerator Start(){yield return new WaitForSeconds(.7f);var run=Run();while(true){object current;try{if(!run.MoveNext())break;current=run.Current;}catch(Exception e){Finish("FAIL: "+e);yield break;}yield return current;}Finish("\nQuest hardware not tested.");}
 void Finish(string result){if(vac)vac.SetSuction(false);Cursor.lockState=CursorLockMode.None;File.WriteAllText(Out+"Ranch_Test.txt",report+result);SessionState.SetBool("RanchValidation",false);EditorApplication.isPlaying=false;}
 IEnumerator Run(){
  game=RanchGame.Instance;vac=FindAnyObjectByType<SlimeVacuum>();Check(game&&vac,"Ranch and vacuum installed in INICIO");Check(game.catalog.Length==3&&game.catalog.All(d=>d&&d.prefab&&d.prefab.GetComponent<RanchItem>()),"All three ScriptableObject item definitions reference usable prefabs");
  var body=FindAnyObjectByType<BeatrixVRBody>();Check(body.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.GetTexture("_BaseMap"),"Beatrix color texture remains assigned");
  Check(!game.autoSave&&!game.autoLoad&&game.saveName.Contains("validation"),"Tests isolated from player save");
  FindAnyObjectByType<BeatrixDesktopPreview>().enabled=false;body.enabled=false;game.garden.enabled=false;vac.ReadInput=false;
  foreach(var item in FindObjectsByType<RanchItem>(FindObjectsSortMode.None)){item.gameObject.SetActive(false);Destroy(item.gameObject);}yield return null;
  vac.transform.SetParent(null,true);vac.transform.SetPositionAndRotation(new Vector3(0,1.5f,0),Quaternion.identity);
  foreach(var s in game.slots)s.count=0;
  for(int i=0;i<20;i++)Check(game.Add(RanchItemKind.Carrot),"Carrot stack accepts item "+(i+1));
  Check(game.Add(RanchItemKind.Carrot)&&game.slots[1].count==1,"Full stack spills to next empty slot");
  for(int i=0;i<4;i++){game.slots[i].kind=RanchItemKind.Carrot;game.slots[i].count=20;}
  Check(!game.CanStore(RanchItemKind.PinkSlime)&&!game.Add(RanchItemKind.PinkSlime),"Full inventory rejects collection");foreach(var s in game.slots)s.count=0;
  var slime=game.Spawn(RanchItemKind.PinkSlime,vac.muzzle.position+Vector3.forward*2,Quaternion.identity);slime.Body.useGravity=false;Physics.SyncTransforms();
  Check(vac.CanPull(slime),"Vacuum accepts visible target");slime.Place(vac.muzzle.position-Vector3.forward*2,Quaternion.identity);Physics.SyncTransforms();Check(!vac.CanPull(slime),"Vacuum rejects targets behind player");slime.Place(vac.muzzle.position+Vector3.forward*8,Quaternion.identity);Physics.SyncTransforms();Check(!vac.CanPull(slime),"Vacuum rejects targets outside range");slime.Place(vac.muzzle.position+Vector3.forward*2,Quaternion.identity);
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=vac.muzzle.position+Vector3.forward;wall.transform.localScale=new Vector3(1,1,.2f);Physics.SyncTransforms();Check(!vac.CanPull(slime),"Wall blocks vacuum");wall.SetActive(false);Destroy(wall);Physics.SyncTransforms();
  float before=Vector3.Distance(slime.transform.position,vac.muzzle.position);vac.SetSuction(true);for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();Check(Vector3.Distance(slime.transform.position,vac.muzzle.position)<before,"Suction pulls using physics");vac.SetSuction(false);yield return new WaitForSeconds(.2f);Check(slime.Body.useGravity,"Releasing suction restores gravity");slime.Place(vac.muzzle.position+Vector3.forward*1.2f,Quaternion.identity);slime.Body.linearVelocity=Vector3.zero;vac.SetSuction(true);
  for(int i=0;i<180&&vac.Count==0;i++)yield return new WaitForFixedUpdate();vac.SetSuction(false);Check(vac.Count==1,"Capture enters inventory");yield return null;Check(!slime,"Captured world object removed without duplication");
  game.Select(0);Check(vac.Shoot()&&vac.Count==0,"Left-fire action consumes one selected item");var launched=FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Single();Check(launched.data.kind==RanchItemKind.PinkSlime&&launched.Body.linearVelocity.z>5,"Ejected slime has correct type and launch velocity");Check(!vac.CanPull(launched),"Launch grace prevents immediate recapture");Check(!vac.Shoot(),"Empty slot cannot create an object");
  launched.Place(new Vector3(0,.4f,3),Quaternion.identity);launched.Body.linearVelocity=Vector3.zero;launched.hunger=0;
  var carrot=game.Spawn(RanchItemKind.Carrot,launched.transform.position+Vector3.right*.4f,Quaternion.identity);Physics.SyncTransforms();Check(launched.TryEat(carrot),"Pink slime eats nearby carrot");Check(!launched.TryEat(carrot),"Same food cannot be consumed twice");Check(launched.hunger>0,"Feeding starts satiety cooldown");var plort=FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Single(i=>i.data.kind==RanchItemKind.PinkPlort);Check(plort,"Feeding produces pink plort");Check(game.Collect(plort),"Plort can be collected");Check(!game.Collect(plort),"Collected plort cannot duplicate");
  game.Select(Array.FindIndex(game.slots,s=>s.count>0&&s.kind==RanchItemKind.PinkPlort));Check(vac.Shoot(),"Stored plort can be ejected");var sale=FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Single(i=>i.data.kind==RanchItemKind.PinkPlort);var market=FindAnyObjectByType<RanchMarket>();int money=game.coins;sale.Place(market.transform.position+Vector3.up,Quaternion.identity);sale.Body.linearVelocity=Vector3.zero;Physics.SyncTransforms();for(int i=0;i<4;i++)yield return new WaitForFixedUpdate();Check(game.coins==money+10,"Market trigger sells physical plort for 10 coins");Check(!sale||sale.Consumed,"Sold plort removed");Check(!market.Sell(launched),"Market rejects slimes");
  game.coins=149;Check(!game.BuyUpgrade()&&game.upgrade==0,"Insufficient coins cannot buy upgrade");game.coins=250;Check(game.BuyUpgrade()&&game.coins==100&&game.Limit(RanchItemKind.Carrot)==30,"Upgrade spends 150 and increases slot capacity");Check(!game.BuyUpgrade()&&game.coins==100,"Upgrade cannot charge twice");
  game.energy=50;Check(game.Sprint(true,1)&&game.energy==32,"Sprinting consumes energy");game.energy=0;Check(!game.Sprint(true,1),"Empty energy blocks sprint");game.health=100;game.Damage(25);Check(game.health==75,"Hazard damages health");game.Damage(25);Check(game.health==75,"Damage cooldown prevents per-frame damage");
  int count=FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Length;game.garden.remaining=0;game.garden.enabled=true;yield return null;yield return null;game.garden.enabled=false;Check(FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Length==count+1,"Garden regrows a carrot");
  game.Add(RanchItemKind.Carrot);game.clock=1785;game.energy=67;game.health=75;launched.hunger=15;int objects=FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Length;Check(game.SaveGame(),"JSON save succeeds");string good=File.ReadAllText(game.SavePath);game.coins=999;game.clock=0;foreach(var slot in game.slots)slot.count=0;launched.Consume();Check(game.LoadGame(),"JSON load succeeds");Check(game.coins==100&&game.upgrade==1&&game.clock==1785&&game.health==75&&game.energy==67,"Money, upgrade, time and vitals restored");Check(game.slots.Any(s=>s.kind==RanchItemKind.Carrot&&s.count==1),"Inventory restored");Check(FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Length==objects,"World objects restored without duplicates");Check(FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Any(i=>i.data.kind==RanchItemKind.PinkSlime&&i.hunger==15),"Slime satiety restored");
  File.WriteAllText(game.SavePath,"{\"version\":999}");Check(!game.LoadGame()&&game.coins==100&&FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Length==objects,"Invalid save rejected without changing current state");File.WriteAllText(game.SavePath,good);
  if(Mouse.current!=null){vac.ReadInput=true;Cursor.lockState=CursorLockMode.Locked;InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;Check(vac.IsSuctioning,"Right mouse button activates suction");int beforeShot=game.slots.Sum(s=>s.count);InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left));yield return null;yield return null;Check(!vac.IsSuctioning,"Left mouse button does not activate suction");Check(game.slots.Sum(s=>s.count)==beforeShot-1,"Left mouse button launches selected inventory item");InputSystem.QueueStateEvent(Mouse.current,new MouseState());vac.ReadInput=false;}
  Check(game.SavePath!=Path.Combine(Application.persistentDataPath,"INICIO_rancho_v1.json"),"User save was never overwritten by test");
 }
}
}
#endif


