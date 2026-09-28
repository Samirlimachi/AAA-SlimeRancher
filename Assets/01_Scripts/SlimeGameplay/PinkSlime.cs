using UnityEngine;
using SlimeRancher.Area1;
namespace SlimeRancherVR {
// Hops around its home: picks wander points, faces where it goes, avoids walls and rocks, bounces off
// what it bumps into and picks a new goal when it gets stuck. Sometimes it just rests and hops in place.
[RequireComponent(typeof(Rigidbody),typeof(SphereCollider))]
public sealed class PinkSlime : MonoBehaviour {
 public SlimeData data;
 public Transform visual;
 [Tooltip("Distancia máxima a la que se aleja de su casa.")]
 public float wanderRadius=3.5f;
 [Tooltip("Velocidad horizontal de cada salto.")]
 public float hopForward=1.2f;
 public Rigidbody Body {get;private set;}
 public bool Captured {get;private set;}
 Vector3 initialPosition,visualScale,wanderTarget,facing,lastHopPosition;Quaternion initialRotation;float nextHop,lastPull=-10,retargetAt;int stuckHops;bool enemy;
 void Awake(){Body=GetComponent<Rigidbody>();initialPosition=transform.position;initialRotation=transform.rotation;visualScale=visual?visual.localScale:Vector3.one;nextHop=Time.time+Random.Range(1f,3f);wanderTarget=initialPosition;lastHopPosition=initialPosition;enemy=GetComponent<Area1EnemySlime>();}
 bool Grounded=>Physics.Raycast(transform.position,Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore);
 void PickWanderTarget(){var circle=Random.insideUnitCircle*wanderRadius;wanderTarget=initialPosition+new Vector3(circle.x,0,circle.y);retargetAt=Time.time+Random.Range(5f,9f);}
 void FixedUpdate(){
  if(Captured)return;
  if(transform.position.y<-5){ResetSlime();return;}
  if(Time.time>=nextHop&&Time.time-lastPull>.15f){
   nextHop=Time.time+Random.Range(1.2f,2.6f);
   if(Grounded){
    // Barely moved since the last hop: something is in the way, go somewhere else.
    var moved=transform.position-lastHopPosition;moved.y=0;
    stuckHops=moved.magnitude<.12f?stuckHops+1:0;
    lastHopPosition=transform.position;
    var toGoal=wanderTarget-transform.position;toGoal.y=0;
    if(toGoal.magnitude<.4f||Time.time>retargetAt||stuckHops>=2){PickWanderTarget();stuckHops=0;toGoal=wanderTarget-transform.position;toGoal.y=0;}
    var direction=Vector3.zero;
    // Enemy slimes only bounce; Area1EnemySlime steers and turns them.
    if(!enemy&&Random.value>.25f)direction=Area1Steering.FreeDirection(transform.position+Vector3.up*.1f,toGoal,.2f,.9f,transform); // 25%: rest, hop in place
    if(direction.sqrMagnitude>.01f)facing=direction;
    Body.AddForce(Vector3.up*data.hopStrength+direction*hopForward,ForceMode.VelocityChange);
    Area1Audio.Play(b=>b.slimeSalpicadura,transform.position);
   }
  }
  // Turn smoothly toward the hop direction, always upright.
  if(!enemy&&facing.sqrMagnitude>.01f)Body.MoveRotation(Quaternion.RotateTowards(Body.rotation,Quaternion.LookRotation(facing,Vector3.up),240*Time.fixedDeltaTime));
 }
 // Bumping into a wall or rock: bounce away and hop again soon in a new direction.
 void OnCollisionEnter(Collision collision){
  if(Captured||enemy||collision.contactCount==0||!Area1Steering.IsObstacle(collision.collider,transform))return;
  var normal=collision.GetContact(0).normal;normal.y=0;
  if(normal.sqrMagnitude<.25f)return; // floor or top of something
  normal.Normalize();
  facing=Vector3.Reflect(facing.sqrMagnitude>.01f?facing:-normal,normal);facing.y=0;
  wanderTarget=transform.position+facing*2;retargetAt=Time.time+4;
  nextHop=Mathf.Min(nextHop,Time.time+.35f);
 }
 public void Pull(Vector3 point,float speed){if(Captured||Body.isKinematic)return;lastPull=Time.time;Body.useGravity=false;Body.linearVelocity=Vector3.MoveTowards(Body.linearVelocity,(point-transform.position).normalized*speed,35*Time.fixedDeltaTime);}
 public void SetHome(Vector3 position){initialPosition=position;wanderTarget=position;lastHopPosition=position;}
 void Update(){
  if(Captured)return;
  if(Time.time-lastPull>.1f)Body.useGravity=true;
  if(visual){float squash=Mathf.Clamp(Body.linearVelocity.y*.025f,-.09f,.09f);visual.localScale=Vector3.Scale(visualScale,new Vector3(1-squash,1+squash,1-squash));}
 }
 public bool Capture(){if(Captured||!gameObject.activeInHierarchy)return false;Captured=true;Body.linearVelocity=Vector3.zero;gameObject.SetActive(false);return true;}
 public void ResetSlime(){Captured=false;gameObject.SetActive(true);Body.position=initialPosition;Body.rotation=initialRotation;Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;Body.useGravity=true;lastPull=-10;nextHop=Time.time+1;}
}
}
