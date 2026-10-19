using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Emergence.Level01
{
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("Emergence/Emotion Switch Trigger")]
    public sealed class EmotionSwitchTrigger : MonoBehaviour
    {
        [Tooltip("支持六种玩法情感和 Blank（无情感）；加载表情不能作为目标。")]
        public RobotEmotion targetEmotion=RobotEmotion.Joy;
        [Tooltip("开启时自动解锁目标情感；关闭时只能切换玩家已解锁的情感。")]
        public bool unlockTargetEmotion=true;
        [Tooltip("开启后本场景实例只成功触发一次；关闭则离开后再次进入可重复触发。")]
        public bool triggerOnce=true;
        public UnityEvent onTriggered=new UnityEvent();
        public bool HasTriggered {get;private set;}
        readonly HashSet<Collider2D> contacts=new HashSet<Collider2D>();
        readonly HashSet<RobotEmotionGameplay> completed=new HashSet<RobotEmotionGameplay>();

        public bool TryTrigger(RobotEmotionGameplay player){
            if(!isActiveAndEnabled || (triggerOnce && HasTriggered) || !player || player.isNpc || !player.isActiveAndEnabled)return false;
            if(GameMenuUI.BlocksGameplay || DialogueDirector.IsBlockingInput || LevelFlowController.IsTransitioning || LocalTimeTarget.IsStopped(this) || LocalTimeTarget.IsStopped(player) || !player.EmotionMechanicsEnabled)return false;
            if(targetEmotion!=RobotEmotion.Blank && !EmotionProgress.IsPlayable(targetEmotion))return false;
            if(!unlockTargetEmotion && !player.IsEmotionUnlocked(targetEmotion))return false;
            if(unlockTargetEmotion)player.UnlockEmotion(targetEmotion);
            player.CancelSwitch();
            if(!player.TrySetEmotion(targetEmotion))return false;
            HasTriggered=true;onTriggered.Invoke();return true;
        }
        public void ResetTrigger(){HasTriggered=false;completed.Clear();}
        void Contact(Collider2D other){
            var player=other.GetComponentInParent<RobotEmotionGameplay>();
            if(!player || player.isNpc)return;
            contacts.Add(other);
            if(!completed.Contains(player) && TryTrigger(player))completed.Add(player);
        }
        void OnTriggerEnter2D(Collider2D other){Contact(other);}
        // Retry blocked entries after dialogue/time-stop ends; never repeatedly overwrite a successful entry.
        void OnTriggerStay2D(Collider2D other){Contact(other);}
        void OnTriggerExit2D(Collider2D other){
            contacts.Remove(other);var player=other.GetComponentInParent<RobotEmotionGameplay>();if(!player)return;
            foreach(var contact in contacts)if(contact && contact.GetComponentInParent<RobotEmotionGameplay>()==player)return;
            completed.Remove(player);
        }
        void OnDisable(){contacts.Clear();completed.Clear();}
        void Reset(){var area=GetComponent<BoxCollider2D>();area.isTrigger=true;area.size=new Vector2(2,3);}
        void OnDrawGizmosSelected(){var area=GetComponent<BoxCollider2D>();if(!area)return;Gizmos.color=Color.cyan;Gizmos.matrix=transform.localToWorldMatrix;Gizmos.DrawWireCube(area.offset,area.size);}
    }
}
