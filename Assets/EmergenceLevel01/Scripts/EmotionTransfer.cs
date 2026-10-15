using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(EmotionResonance))]
    public sealed class EmotionTransfer : MonoBehaviour
    {
        public KeyCode interactKey=KeyCode.E;
        public bool automaticTransfer;
        EmotionResonance source;
        void Awake(){source=GetComponent<EmotionResonance>();}
        void Update(){if(source.Actor.isNpc || !source.Actor.CanUseEmotionAbilities)return;if(Input.GetKeyDown(interactKey)){if(source.Emotion==RobotEmotion.Memory && MemoryInteractable.TryNearest(source))return;TransferNearest();}else if(automaticTransfer)TransferNearest();}
        public bool TransferNearest(){
            EmotionResonance nearest=null;float distance=source.radius;
            foreach(var npc in EmotionResonance.Actors){if(!npc || !npc.Actor.isNpc)continue;float d=Vector2.Distance(transform.position,npc.transform.position);if(d<=distance){nearest=npc;distance=d;}}
            return nearest && nearest.Actor.ReceiveEmotion(source.Actor);
        }
    }
}
