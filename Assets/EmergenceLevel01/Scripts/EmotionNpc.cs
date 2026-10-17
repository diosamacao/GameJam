using System;
using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    public enum NpcRouteMovement { Walk, Jump }
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(RobotMotor2D),typeof(EmotionResonance),typeof(LocalTimeTarget))]
    public sealed class EmotionNpc : MonoBehaviour
    {
        [Serializable] public sealed class Waypoint {
            public Transform target;
            public Vector2 position;
            public NpcRouteMovement movement;
            [Min(0)] public float waitSeconds;
            [Min(.1f)] public float jumpApexHeight=1;
            [Min(.1f)] public float maximumJumpSpeed=8;
            public Vector2 Position=>target?(Vector2)target.position:position;
        }
        public Waypoint[] route=new Waypoint[0];
        public bool loopRoute;
        public Transform destination;
        [Tooltip("无路线或调用MoveTo时使用的世界坐标。")]
        public Vector2 destinationPosition;
        [Min(0)] public float speed=2;
        [Min(.01f)] public float gravityScale=3;
        [Min(.001f)] public float arrivalDistance=.1f;
        [Min(.01f)] public float verticalTolerance=.2f;
        public bool activateOnStart;
        public UnityEvent onArrived=new UnityEvent();
        public bool Moving {get;private set;}
        public int RouteIndex {get;private set;}
        public bool JumpBlocked {get;private set;}
        Rigidbody2D body;RobotMotor2D motor;bool useRoute,jumped,waiting;float remainingWait;
        void Awake(){
            body=GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Dynamic;body.gravityScale=gravityScale;body.constraints|=RigidbodyConstraints2D.FreezeRotation;
            motor=GetComponent<RobotMotor2D>();motor.externallyDriven=false;motor.moveSpeed=speed;
            var actor=GetComponent<RobotEmotionGameplay>();actor.isNpc=true;actor.keyboardInput=false;actor.showSwitchProgress=false;
            var keyboard=GetComponent<RobotKeyboardInput>();if(keyboard)keyboard.enabled=false;
        }
        void Start(){if(activateOnStart)Activate();}
        [ContextMenu("Activate NPC (Play mode)")] public void Activate(){RouteIndex=0;useRoute=route!=null && route.Length>0;jumped=waiting=false;remainingWait=0;Moving=true;}
        public void StopMoving(){Moving=false;if(motor){motor.SetMoveInput(0);motor.CancelDirectedJump();}}
        public void MoveTo(Transform target){destination=target;Activate();useRoute=false;}
        void FixedUpdate(){
            if(LocalTimeTarget.IsStopped(this))return;
            motor.moveSpeed=speed;
            if(!Moving || motor.InputBlocked){motor.SetMoveInput(0);return;}
            if(waiting){motor.SetMoveInput(0);remainingWait-=Time.fixedDeltaTime;if(remainingWait<=0)Advance();return;}
            var point=useRoute?route[RouteIndex]:null;
            if(useRoute && point==null){Advance();return;}
            Vector2 target=point!=null?point.Position:destination?(Vector2)destination.position:destinationPosition;
            Vector2 delta=target-body.position;
            if(motor.IsGrounded && Mathf.Abs(delta.x)<=arrivalDistance && Mathf.Abs(delta.y)<=verticalTolerance){
                motor.SetMoveInput(0);jumped=false;
                if(point!=null && point.waitSeconds>0){waiting=true;remainingWait=point.waitSeconds;}else Advance();return;
            }
            if(point!=null && point.movement==NpcRouteMovement.Jump){
                motor.SetMoveInput(0);
                if(motor.IsGrounded){if(jumped)jumped=false;JumpBlocked=!motor.LaunchDirectedJump(target,point.jumpApexHeight,point.maximumJumpSpeed);jumped=!JumpBlocked;}
            }else motor.SetMoveInput(Mathf.Abs(delta.x)<=arrivalDistance?0:Mathf.Sign(delta.x));
        }
        void Advance(){waiting=false;jumped=false;JumpBlocked=false;if(useRoute && ++RouteIndex<route.Length)return;if(useRoute && loopRoute){RouteIndex=0;return;}StopMoving();onArrived.Invoke();}
        void OnDisable(){StopMoving();}
        void OnDrawGizmosSelected(){var from=transform.position;Gizmos.color=Color.green;if(route!=null && route.Length>0){foreach(var point in route){if(point==null)continue;var to=(Vector3)point.Position;Gizmos.color=point.movement==NpcRouteMovement.Jump?Color.yellow:Color.green;Gizmos.DrawLine(from,to);Gizmos.DrawWireSphere(to,.15f);from=to;}}else{var to=destination?destination.position:(Vector3)destinationPosition;Gizmos.DrawLine(from,to);Gizmos.DrawWireSphere(to,.15f);}}
    }
}
