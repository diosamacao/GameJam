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
        public bool IsVanishing=>vanishing;
        public bool BeginVanish(){if(Removed || vanishing || !isActiveAndEnabled || LocalTimeTarget.IsStopped(this))return false;StartCoroutine(Vanish());return true;}
        public bool TryInteract(RobotEmotionGameplay player)
        {
            if(Removed || vanishing || LocalTimeTarget.IsStopped(this) || !isActiveAndEnabled || !player || !player.CanUseEmotionAbilities)return false;
            if(kind==EmotionBlockKind.Electric)return player.HandleElectricContact();
            if(!EmotionBlockRules.CanRemove(kind,player.EffectiveEmotion))return false;
            if(kind==EmotionBlockKind.Vanishing)return BeginVanish();
            foreach(var sr in GetComponentsInChildren<SpriteRenderer>())BlockShatterEffect.Spawn(sr.sprite,sr.transform.localToWorldMatrix,sr.sharedMaterial,sr.sortingLayerID,sr.sortingOrder,sr.color,shatter);
            Removed=true;onRemoved.Invoke();gameObject.SetActive(false);return true;
        }
        [Header("悲伤方块")]
        [Tooltip("触碰后渐隐时长（秒），期间保留碰撞。")][Min(0)] public float fadeSeconds=.5f;
        [Tooltip("渐隐后的不透明度。")][Range(0,1)] public float fadedAlpha=.3f;
        [Tooltip("关闭碰撞后到恢复的时长（秒）。")][Min(0)] public float restoreSeconds=3;
        [Header("破坏表现")]
        public BlockShatterSettings shatter=new BlockShatterSettings();
        System.Collections.IEnumerator Vanish(){
            vanishing=true;visuals=GetComponentsInChildren<SpriteRenderer>();colors=new Color[visuals.Length];colliders=GetComponentsInChildren<Collider2D>();enabledStates=new bool[colliders.Length];
            for(int i=0;i<visuals.Length;i++)colors[i]=visuals[i].color;
            for(int i=0;i<colliders.Length;i++)enabledStates[i]=colliders[i].enabled;
            float elapsed=0;
            while(elapsed<fadeSeconds || LocalTimeTarget.IsStopped(this)){if(!LocalTimeTarget.IsStopped(this)){elapsed+=Time.deltaTime;SetFade(elapsed/Mathf.Max(.001f,fadeSeconds));}yield return null;}
            SetFade(1);
            for(int i=0;i<colliders.Length;i++)if(colliders[i])colliders[i].enabled=false;
            elapsed=0;while(elapsed<restoreSeconds || LocalTimeTarget.IsStopped(this)){elapsed+=LocalTimeTarget.DeltaTime(this);yield return null;}Restore();
        }
        void SetFade(float progress){for(int i=0;i<visuals.Length;i++)if(visuals[i]){var color=colors[i];color.a=Mathf.Lerp(colors[i].a,fadedAlpha,progress);visuals[i].color=color;}}
        void Restore(){if(!vanishing)return;for(int i=0;i<visuals.Length;i++)if(visuals[i])visuals[i].color=colors[i];for(int i=0;i<colliders.Length;i++)if(colliders[i])colliders[i].enabled=enabledStates[i];vanishing=false;}
        void OnDisable(){StopAllCoroutines();Restore();}
    }
}
