using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    public sealed class DialogueTrigger : MonoBehaviour
    {
        public DialogueDirector director;
        public DialogueSequence sequence;
        public bool triggerOnEnter;
        public bool playOnce=true;
        public UnityEvent onCompleted=new UnityEvent();
        public bool HasPlayed {get;private set;}
        DialogueDirector listening;
        public bool TryTrigger()
        {
            if(!isActiveAndEnabled || listening || (playOnce && HasPlayed))return false;
            var target=director?director:DialogueDirector.Instance;if(!target)return false;
            if(!target.TryPlay(sequence))return false;
            HasPlayed=true;listening=target;listening.Finished+=Completed;listening.Cancelled+=Cancelled;return true;
        }
        public void Trigger(){TryTrigger();}
        void OnTriggerEnter2D(Collider2D other){if(triggerOnEnter && other.GetComponentInParent<RobotMotor2D>())TryTrigger();}
        void Completed(DialogueSequence finished){if(finished!=sequence)return;Detach();onCompleted.Invoke();}
        void Cancelled(DialogueSequence cancelled){if(cancelled!=sequence)return;HasPlayed=false;Detach();}
        void Detach(){if(listening){listening.Finished-=Completed;listening.Cancelled-=Cancelled;}listening=null;}
        void OnDisable(){Detach();}
    }
}
