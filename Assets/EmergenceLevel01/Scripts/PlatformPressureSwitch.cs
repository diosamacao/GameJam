using System.Collections.Generic;
using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class PlatformPressureSwitch : MonoBehaviour
    {
        [Tooltip("平台的 Activation 决定单次或持续模式；一个开关可控制多个平台。")]
        public LocalMovingPlatform[] platforms;
        public bool allowNpc;
        [Min(0)] public float feetTolerance=.15f;
        public SpriteRenderer indicator;
        public Color idleColor=Color.gray, pressedColor=Color.cyan;
        public bool Pressed { get; private set; }
        readonly HashSet<Collider2D> occupants=new HashSet<Collider2D>();
        bool Eligible(Collider2D other){var actor=other.GetComponentInParent<RobotEmotionGameplay>();return actor && (allowNpc || !actor.isNpc) && other.bounds.min.y>=GetComponent<Collider2D>().bounds.center.y-feetTolerance;}
        void OnTriggerEnter2D(Collider2D other){if(Eligible(other)){occupants.Add(other);Refresh();}}
        void OnTriggerStay2D(Collider2D other){if(Eligible(other))occupants.Add(other);else occupants.Remove(other);Refresh();}
        void OnTriggerExit2D(Collider2D other){occupants.Remove(other);Refresh();}
        void FixedUpdate(){occupants.RemoveWhere(c=>!c || !c.enabled || !c.gameObject.activeInHierarchy || !Eligible(c) || !GetComponent<Collider2D>().IsTouching(c));Refresh();}
        void Refresh(){SetPressed(occupants.Count>0);}
        public void SetPressed(bool value){if(Pressed==value)return;Pressed=value;if(indicator)indicator.color=value?pressedColor:idleColor;if(platforms!=null)foreach(var platform in platforms)if(platform)platform.SetSwitch(this,value);}
        void OnDisable(){occupants.Clear();SetPressed(false);}
        void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
    }
}
