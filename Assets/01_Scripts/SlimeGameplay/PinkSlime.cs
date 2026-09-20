using UnityEngine;
namespace SlimeRancherVR {
[RequireComponent(typeof(Rigidbody),typeof(SphereCollider))]
public sealed class PinkSlime : MonoBehaviour {
 public SlimeData data;
 public Transform visual;
 public Rigidbody Body {get;private set;}
 public bool Captured {get;private set;}
 Vector3 initialPosition,visualScale;Quaternion initialRotation;float nextHop,lastPull=-10;
 void Awake(){Body=GetComponent<Rigidbody>();initialPosition=transform.position;initialRotation=transform.rotation;visualScale=visual?visual.localScale:Vector3.one;nextHop=Time.time+Random.Range(1f,3f);}
 void FixedUpdate(){
  if(Captured)return;
  if(transform.position.y<-5){ResetSlime();return;}
  if(Time.time>=nextHop&&Time.time-lastPull>.15f){
   nextHop=Time.time+Random.Range(1.4f,3f);
   if(Physics.Raycast(transform.position,Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore)){
    var drift=Vector3.ClampMagnitude(initialPosition-transform.position,.5f);drift.y=0;
    Body.AddForce(Vector3.up*data.hopStrength+drift,ForceMode.VelocityChange);
   }
  }
 }
 public void Pull(Vector3 point,float speed){if(Captured)return;lastPull=Time.time;Body.useGravity=false;Body.linearVelocity=Vector3.MoveTowards(Body.linearVelocity,(point-transform.position).normalized*speed,35*Time.fixedDeltaTime);}
 public void SetHome(Vector3 position){initialPosition=position;}
 void Update(){
  if(Captured)return;
  if(Time.time-lastPull>.1f)Body.useGravity=true;
  if(visual){float squash=Mathf.Clamp(Body.linearVelocity.y*.025f,-.09f,.09f);visual.localScale=Vector3.Scale(visualScale,new Vector3(1-squash,1+squash,1-squash));}
 }
 public bool Capture(){if(Captured||!gameObject.activeInHierarchy)return false;Captured=true;Body.linearVelocity=Vector3.zero;gameObject.SetActive(false);return true;}
 public void ResetSlime(){Captured=false;gameObject.SetActive(true);Body.position=initialPosition;Body.rotation=initialRotation;Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;Body.useGravity=true;lastPull=-10;nextHop=Time.time+1;}
}
}
