using UnityEngine;
namespace Emergence.Level01
{
    public sealed class MemoryGridTarget : MonoBehaviour
    {
        [Tooltip("指定Grid或Tilemap对象。控制器必须放在目标之外，目标隐藏后仍能恢复。")]
        public GameObject target;
        public bool visibleInRange;
        bool initial,initialized;
        void Start(){if(target && target!=gameObject && !transform.IsChildOf(target.transform)){initial=target.activeSelf;initialized=true;}else{Debug.LogWarning("MemoryGridTarget must be outside its target hierarchy",this);enabled=false;}}
        void Update(){if(initialized && target)target.SetActive(EmotionResonance.InEnhancedRange(target.transform.position,RobotEmotion.Memory)?visibleInRange:initial);}
        void OnDisable(){if(initialized && target)target.SetActive(initial);}
    }
}
