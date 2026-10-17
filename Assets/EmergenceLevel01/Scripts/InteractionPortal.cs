using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class InteractionPortal : MonoBehaviour
    {
        static readonly List<InteractionPortal> portals=new List<InteractionPortal>();
        public Transform destination;
        public Vector2 destinationPosition;
        public bool clearVelocity=true;
        [Min(0)] public float cooldownSeconds=.5f;
        public UnityEvent onTeleported=new UnityEvent();
        readonly HashSet<Collider2D> contacts=new HashSet<Collider2D>();
        static readonly Dictionary<int,float> cooldowns=new Dictionary<int,float>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetState(){portals.Clear();cooldowns.Clear();}
        void OnEnable(){portals.Add(this);}
        void OnDisable(){portals.Remove(this);contacts.Clear();}
        void OnTriggerEnter2D(Collider2D other){contacts.Add(other);}
        void OnTriggerExit2D(Collider2D other){contacts.Remove(other);}
        public bool TryTeleport(RobotEmotionGameplay actor){
            if(LocalTimeTarget.IsStopped(this) || !actor || actor.isNpc || !actor.CanUseEmotionAbilities)return false;
            bool touching=false;foreach(var c in contacts)if(c && c.enabled && c.GetComponentInParent<RobotEmotionGameplay>()==actor && GetComponent<Collider2D>().IsTouching(c)){touching=true;break;}
            if(!touching)return false;
            int id=actor.GetInstanceID();if(cooldowns.TryGetValue(id,out float until) && Time.time<until)return false;
            cooldowns[id]=Time.time+cooldownSeconds;
            var motor=actor.GetComponent<RobotMotor2D>();var body=actor.GetComponent<Rigidbody2D>();
            actor.CancelSwitch();motor.CancelJumpRequest();motor.SetMoveInput(0);motor.ResetGroundState();
            body.position=destination?(Vector2)destination.position:destinationPosition;if(clearVelocity)body.velocity=Vector2.zero;
            var transfer=actor.GetComponent<EmotionTransfer>();if(transfer)transfer.CancelTransfer();
            contacts.Clear();onTeleported.Invoke();return true;
        }
        public static bool TryContact(RobotEmotionGameplay actor){foreach(var portal in portals)if(portal.TryTeleport(actor))return true;return false;}
        void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
        void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawLine(transform.position,destination?destination.position:(Vector3)destinationPosition);}
    }
}
