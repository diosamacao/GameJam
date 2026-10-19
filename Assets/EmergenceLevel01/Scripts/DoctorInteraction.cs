using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    public sealed class DoctorInteraction : StoryInteractable
    {
        [Tooltip("不指定则不播放对话。")]
        public DialogueSequence dialogue;
        [Tooltip("Blank表示不解锁。对话完整结束后才解锁；无对话时立即解锁。")]
        public RobotEmotion unlockEmotion=RobotEmotion.Blank;
        public bool interactOnce;
        public UnityEvent onCompleted=new UnityEvent();
        public bool Completed {get;private set;}
        DialogueDirector listening;DialogueSequence playing;
        protected override bool Available=>!listening && !(interactOnce && Completed);
        protected override bool Interact(RobotEmotionGameplay player){
            if(!dialogue){Complete();return true;}
            var director=DialogueDirector.Instance;
            if(!director)director=new GameObject("DialogueSystem",typeof(DialogueDirector)).GetComponent<DialogueDirector>();
            if(director.IsPlaying)return false;
            playing=dialogue;listening=director;director.Finished+=Finished;director.Cancelled+=Cancelled;
            if(!director.TryPlay(playing)){Detach();return false;}return true;
        }
        void Finished(DialogueSequence sequence){if(sequence!=playing)return;Detach();Complete();}
        void Cancelled(DialogueSequence sequence){if(sequence==playing)Detach();}
        void Complete(){if(EmotionProgress.IsPlayable(unlockEmotion))EmotionProgress.Unlock(unlockEmotion);Completed=true;onCompleted.Invoke();}
        void Detach(){if(listening){listening.Finished-=Finished;listening.Cancelled-=Cancelled;}listening=null;playing=null;}
        protected override void OnDisable(){Detach();base.OnDisable();}
        void Reset(){interactionHint="F 与博士交谈";}
    }
}
