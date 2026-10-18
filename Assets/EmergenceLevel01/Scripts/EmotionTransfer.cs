using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(EmotionResonance))]
    public sealed class EmotionTransfer : MonoBehaviour
    {
        public KeyCode interactKey=KeyCode.E;
        [Min(0)] public float transferRadius=2;
        [Min(0)] public float holdSeconds=.5f;
        public bool showProgress=true;
        public Rect progressRect=new Rect(20,80,260,28);
        EmotionResonance source,target;RobotEmotion initialEmotion;float elapsed;bool consumed;
        public float Progress=>target?Mathf.Clamp01(elapsed/Mathf.Max(.001f,holdSeconds)):0;
        void Awake(){source=GetComponent<EmotionResonance>();}
        void Update(){
            if(!Input.GetKey(interactKey)){CancelTransfer();consumed=false;return;}
            if(source.Actor.isNpc || !source.Actor.CanUseEmotionAbilities){CancelTransfer();consumed=true;return;}
            if(Input.GetKeyDown(interactKey)){
                if(LabDoor.TryRememberNearest(source.Actor) || StoryInteractable.TryNearest(source.Actor) || InteractionPortal.TryContact(source.Actor) || (source.Emotion==RobotEmotion.Memory && MemoryInteractable.TryNearest(source))){consumed=true;return;}
                BeginTransfer();
            }
            if(!consumed)AdvanceTransfer(Time.deltaTime);
        }
        public bool BeginTransfer(){
            CancelTransfer();if(consumed || source.Actor.isNpc || !source.Actor.CanUseEmotionAbilities || source.Emotion==RobotEmotion.Blank || source.Emotion==RobotEmotion.Loading)return false;
            float distance=transferRadius;foreach(var npc in EmotionResonance.Actors){if(!npc || !npc.Actor.isNpc || !npc.Actor.CanUseEmotionAbilities)continue;float d=Vector2.Distance(transform.position,npc.transform.position);if(d<=distance){target=npc;distance=d;}}
            initialEmotion=source.Emotion;return target;
        }
        public bool AdvanceTransfer(float deltaTime){
            if(!target)return false;
            if(!source.Actor.CanUseEmotionAbilities || !target.isActiveAndEnabled || !target.Actor.CanUseEmotionAbilities || source.Emotion!=initialEmotion || Vector2.Distance(transform.position,target.transform.position)>transferRadius){CancelTransfer();consumed=true;return false;}
            elapsed+=Mathf.Max(0,deltaTime);if(elapsed<holdSeconds)return false;
            bool result=target.Actor.ReceiveEmotion(source.Actor);CancelTransfer();consumed=true;return result;
        }
        public void CancelTransfer(){target=null;elapsed=0;}
        public bool TransferNearest(){return BeginTransfer();}
        void OnDisable(){CancelTransfer();consumed=false;}
        void OnApplicationFocus(bool focus){if(!focus){CancelTransfer();consumed=true;}}
        void OnGUI(){if(showProgress && target)GUI.Box(progressRect,"传递情感 "+Mathf.CeilToInt(Progress*100)+"%");}
    }
}
