using System.Collections.Generic;
using UnityEngine;
namespace Emergence.Level01
{
    public enum PlatformActivation { Automatic, Single, Continuous }
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(Rigidbody2D),typeof(LocalTimeTarget))]
    public sealed class LocalMovingPlatform : MonoBehaviour
    {
        [Tooltip("相对起始位置的终点偏移。")]
        public Vector2 travel=new Vector2(4,0);
        [Min(0)] public float speed=2;
        public bool pingPong=true;
        public PlatformActivation activation=PlatformActivation.Automatic;
        [Min(0)] public float endpointWaitSeconds=1;
        [Min(.0001f)] public float arrivalTolerance=.01f;
        public bool oneWay=true;
        [Range(1,180)] public float surfaceArc=160;
        Rigidbody2D body;LocalTimeTarget timeTarget;Vector2 start;
        bool returning, atDestination;float wait;
        readonly Queue<System.Action> deferred=new Queue<System.Action>();
        readonly HashSet<PlatformPressureSwitch> switches=new HashSet<PlatformPressureSwitch>();
        public Vector2 Origin=>start;
        public bool Returning=>returning;
        public float RemainingWait=>wait;
        public Vector2 MotionVelocity {get;private set;}
        void Awake(){body=GetComponent<Rigidbody2D>();timeTarget=GetComponent<LocalTimeTarget>();start=body.position;ConfigureOneWay();}
        public void ConfigureOneWay(){var effector=GetComponent<PlatformEffector2D>();if(!effector && oneWay)effector=gameObject.AddComponent<PlatformEffector2D>();if(effector){effector.enabled=oneWay;effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=surfaceArc;}foreach(var c in GetComponents<Collider2D>())if(!c.isTrigger)c.usedByEffector=oneWay;}
        public void SetSwitch(PlatformPressureSwitch source,bool pressed){
            if(LocalTimeTarget.IsStopped(this)){deferred.Enqueue(()=>SetSwitch(source,pressed));return;}
            bool changed=pressed?switches.Add(source):switches.Remove(source);
            if(!changed)return;
            if(activation==PlatformActivation.Single && pressed){atDestination=!atDestination;returning=!atDestination;wait=0;}
            if(activation==PlatformActivation.Continuous){if(switches.Count==0){returning=true;wait=0;}else if(pressed && Vector2.Distance(body.position,start)<=arrivalTolerance){returning=false;wait=0;}}
        }
        void FixedUpdate(){
            MotionVelocity=Vector2.zero;
            if(timeTarget.Frozen)return;
            // Process only the current batch: a newly frozen event may enqueue itself again.
            int pendingCount=deferred.Count;
            for(int i=0;i<pendingCount;i++)deferred.Dequeue()();
            if(LocalTimeTarget.IsStopped(this))return;
            switches.RemoveWhere(s=>!s || !s.isActiveAndEnabled);
            bool held=switches.Count>0;
            if(activation==PlatformActivation.Continuous && !held){returning=true;wait=0;}
            if(activation==PlatformActivation.Single)returning=!atDestination;
            if(wait>0){wait=Mathf.Max(0,wait-Time.fixedDeltaTime);return;}
            Vector2 target=returning?start:start+travel;
            if(Vector2.Distance(body.position,target)<=arrivalTolerance){
                body.MovePosition(target);
                if((activation==PlatformActivation.Automatic && pingPong) || (activation==PlatformActivation.Continuous && held)){returning=!returning;wait=endpointWaitSeconds;}
                return;
            }
            var next=Vector2.MoveTowards(body.position,target,speed*Time.fixedDeltaTime);
            MotionVelocity=(next-body.position)/Time.fixedDeltaTime;
            body.MovePosition(next);
        }
        void OnDisable(){deferred.Clear();switches.Clear();MotionVelocity=Vector2.zero;}
    }
}
