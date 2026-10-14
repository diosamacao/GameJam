using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    public sealed class EmotionBlock : MonoBehaviour
    {
        public EmotionBlockKind kind;
        public UnityEvent onRemoved=new UnityEvent();
        public bool Removed {get;private set;}
        bool vanishing;SpriteRenderer[] visuals;Color[] colors;Collider2D[] colliders;bool[] enabledStates;
        public bool TryInteract(RobotEmotionGameplay player)
        {
            if(Removed || vanishing || !isActiveAndEnabled || !player || !player.CanUseEmotionAbilities)return false;
            if(kind==EmotionBlockKind.Electric)return player.HandleElectricContact();
            if(!EmotionBlockRules.CanRemove(kind,player.CurrentEmotion))return false;
            if(kind==EmotionBlockKind.Vanishing){StartCoroutine(Vanish());return true;}
            foreach(var sr in GetComponentsInChildren<SpriteRenderer>())BlockShatterEffect.Spawn(sr.sprite,sr.transform.localToWorldMatrix,sr.sharedMaterial,sr.sortingLayerID,sr.sortingOrder,sr.color);
            Removed=true;onRemoved.Invoke();gameObject.SetActive(false);return true;
        }
        System.Collections.IEnumerator Vanish(){
            vanishing=true;visuals=GetComponentsInChildren<SpriteRenderer>();colors=new Color[visuals.Length];colliders=GetComponentsInChildren<Collider2D>();enabledStates=new bool[colliders.Length];
            for(int i=0;i<visuals.Length;i++){colors[i]=visuals[i].color;var c=colors[i];c.a=.3f;visuals[i].color=c;}
            for(int i=0;i<colliders.Length;i++){enabledStates[i]=colliders[i].enabled;colliders[i].enabled=false;}
            yield return new WaitForSeconds(3);Restore();
        }
        void Restore(){if(!vanishing)return;for(int i=0;i<visuals.Length;i++)if(visuals[i])visuals[i].color=colors[i];for(int i=0;i<colliders.Length;i++)if(colliders[i])colliders[i].enabled=enabledStates[i];vanishing=false;}
        void OnDisable(){StopAllCoroutines();Restore();}
    }
}
