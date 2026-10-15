using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    [RequireComponent(typeof(RobotMotor2D),typeof(EmotionResonance))]
    public sealed class EmotionNpc : MonoBehaviour
    {
        public Transform destination;
        [Tooltip("未指定Destination时使用的世界坐标。")]
        public Vector2 destinationPosition;
        [Min(0)] public float speed=2;
        [Min(.001f)] public float arrivalDistance=.05f;
        [Min(0)] public float collisionSkin=.02f;
        public LayerMask obstacleLayers=~0;
        public bool activateOnStart;
        public UnityEvent onArrived=new UnityEvent();
        public bool Moving {get;private set;}
        Rigidbody2D body;RobotMotor2D motor;readonly RaycastHit2D[] hits=new RaycastHit2D[32];
        void Awake(){body=GetComponent<Rigidbody2D>();motor=GetComponent<RobotMotor2D>();motor.externallyDriven=true;var actor=GetComponent<RobotEmotionGameplay>();actor.isNpc=true;actor.keyboardInput=false;actor.showSwitchProgress=false;var keyboard=GetComponent<RobotKeyboardInput>();if(keyboard)keyboard.enabled=false;}
        void Start(){if(activateOnStart)Activate();}
        [ContextMenu("Activate NPC (Play mode)")]
        public void Activate(){Moving=true;}
        public void StopMoving(){Moving=false;if(motor)motor.SetMoveInput(0);}
        public void MoveTo(Transform target){destination=target;Activate();}
        void FixedUpdate(){
            if(!Moving || DialogueDirector.IsBlockingInput){motor.SetMoveInput(0);return;}
            var timeTarget=GetComponent<LocalTimeTarget>();if(timeTarget && timeTarget.Frozen)return;
            Vector2 target=destination?(Vector2)destination.position:destinationPosition;Vector2 delta=target-body.position;
            if(delta.magnitude<=arrivalDistance){StopMoving();onArrived.Invoke();return;}
            var filter=new ContactFilter2D();filter.SetLayerMask(obstacleLayers);filter.useTriggers=false;
            float step=Mathf.Min(delta.magnitude,Mathf.Max(0,speed)*Time.fixedDeltaTime);
            int count=body.Cast(delta.normalized,filter,hits,step+collisionSkin);
            for(int i=0;i<count;i++)if(hits[i].collider && Vector2.Dot(hits[i].normal,delta.normalized)<-.01f)step=Mathf.Min(step,Mathf.Max(0,hits[i].distance-collisionSkin));
            body.MovePosition(body.position+delta.normalized*step);motor.SetMoveInput(step>0?Mathf.Sign(delta.x):0);
        }
        void OnDisable(){StopMoving();}
        void OnDrawGizmosSelected(){Gizmos.color=Color.green;Vector3 target=destination?destination.position:(Vector3)destinationPosition;Gizmos.DrawLine(transform.position,target);Gizmos.DrawWireSphere(target,.15f);}
    }
}
