using System.Collections.Generic;
using UnityEngine;
namespace Emergence.Level01
{
    public enum VerticalDoorState { Normal, Memory }
    public sealed class LabDoor : MonoBehaviour
    {
        public Transform upper, lower;
        public BoxCollider2D blocker;
        public float travel=26f/16f;
        public float seconds=.6f;
        [Tooltip("门开启到此进度时关闭碰撞，开始关闭时恢复碰撞。")][Range(0,1)] public float collisionReleaseProgress=.98f;
        public bool open;
        [Header("VerticalDoor 回忆状态")]
        public bool enableMemoryStates;
        public VerticalDoorState state=VerticalDoorState.Normal;
        [Range(0,1)] public float normalAlpha=1, memoryAlpha=.5f;
        [Min(0)] public float interactionRadius=2;
        static readonly List<LabDoor> doors=new List<LabDoor>();
        Vector3 upperClosed,lowerClosed;
        SpriteRenderer[] visuals;
        float progress;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetRegistry(){doors.Clear();}
        void OnEnable(){if(!doors.Contains(this))doors.Add(this);}
        void OnDisable(){doors.Remove(this);}
        void Awake(){if(upper)upperClosed=upper.localPosition;if(lower)lowerClosed=lower.localPosition;visuals=GetComponentsInChildren<SpriteRenderer>(true);if(enableMemoryStates)ApplyState();}
        public void SetOpen(bool value){open=value && (!enableMemoryStates || state==VerticalDoorState.Memory);UpdateCollision();}
        [ContextMenu("Open (Play mode)")] void Open(){SetOpen(true);}
        [ContextMenu("Close (Play mode)")] void Close(){SetOpen(false);}
        bool CanInteract(RobotEmotionGameplay player)=>enableMemoryStates && isActiveAndEnabled && player && !player.isNpc && player.CanUseEmotionAbilities && !LocalTimeTarget.IsStopped(this) && Vector2.Distance(transform.position,player.transform.position)<=interactionRadius;
        public bool TryToggleMemory(RobotEmotionGameplay player){if(!CanInteract(player) || player.CurrentEmotion!=RobotEmotion.Memory)return false;state=state==VerticalDoorState.Normal?VerticalDoorState.Memory:VerticalDoorState.Normal;ApplyState();return true;}
        public bool TryToggleOpen(RobotEmotionGameplay player){if(!CanInteract(player) || state!=VerticalDoorState.Memory)return false;SetOpen(!open);return true;}
        static LabDoor Nearest(RobotEmotionGameplay player,bool memoryOnly){LabDoor found=null;float best=float.PositiveInfinity;foreach(var door in doors){if(!door || !door.CanInteract(player) || (memoryOnly && door.state!=VerticalDoorState.Memory) || (door.state==VerticalDoorState.Normal && player.CurrentEmotion!=RobotEmotion.Memory))continue;float d=Vector2.Distance(player.transform.position,door.transform.position);if(d<best){best=d;found=door;}}return found;}
        public static bool TryRememberNearest(RobotEmotionGameplay player){if(!player || player.CurrentEmotion!=RobotEmotion.Memory)return false;var door=Nearest(player,false);return door && door.TryToggleMemory(player);}
        public static bool TryInteractNearest(RobotEmotionGameplay player){if(!player)return false;var door=Nearest(player,false);return door && (door.state==VerticalDoorState.Normal?door.TryToggleMemory(player):door.TryToggleOpen(player));}
        void UpdateCollision(){if(blocker)blocker.enabled=!open || (enableMemoryStates && state==VerticalDoorState.Normal) || progress<collisionReleaseProgress;}
        void ApplyState(){if(state==VerticalDoorState.Normal)open=false;UpdateCollision();if(visuals==null)visuals=GetComponentsInChildren<SpriteRenderer>(true);foreach(var visual in visuals)if(visual){var c=visual.color;c.a=state==VerticalDoorState.Memory?memoryAlpha:normalAlpha;visual.color=c;}}
        void Update(){
            if(enableMemoryStates){
                if(LocalTimeTarget.IsStopped(this))return;
                ApplyState();
            }
            progress=Mathf.MoveTowards(progress,open?1:0,LocalTimeTarget.DeltaTime(this)/Mathf.Max(.01f,seconds));
            if(upper)upper.localPosition=upperClosed+Vector3.up*travel*progress;
            if(lower)lower.localPosition=lowerClosed+Vector3.down*travel*progress;
            UpdateCollision();
        }
    }
}
