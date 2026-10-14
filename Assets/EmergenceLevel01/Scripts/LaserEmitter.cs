using System.Collections.Generic;
using UnityEngine;
namespace Emergence.Level01
{
    public sealed class LaserEmitter : MonoBehaviour
    {
        public Vector2 direction=Vector2.right;
        [Min(.1f)] public float maxDistance=30;
        [Min(.01f)] public float beamWidth=.08f;
        public LayerMask blockingLayers=~0;
        public Material beamMaterial;
        public bool laserEnabled=true;
        public float BeamLength {get;private set;}
        public Vector2 BeamOrigin => transform.TransformPoint((Vector3)(direction.normalized*.49f));
        public Vector2 BeamDirection => transform.TransformVector(direction).normalized;
        readonly List<RaycastHit2D> hits=new List<RaycastHit2D>();
        LineRenderer beam,core;
        void Awake(){beam=MakeLine("Laser",new Color(1,.06f,.12f),beamWidth,30);core=MakeLine("LaserCore",new Color(1,.85f,.85f),beamWidth*.3f,31);}
        LineRenderer MakeLine(string name,Color color,float width,int order){
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=2;line.startWidth=line.endWidth=width;
            line.startColor=line.endColor=color;line.sharedMaterial=beamMaterial;line.sortingOrder=order;return line;
        }
        void FixedUpdate(){Trace(true);}
        // A box cast matches the visible beam thickness, so edge contact is lethal too.
        public void Trace(bool applyDamage){
            if(!laserEnabled){BeamLength=0;Show(false);return;}
            var origin=BeamOrigin;var dir=BeamDirection;
            var filter=new ContactFilter2D();filter.SetLayerMask(blockingLayers);filter.useTriggers=false;
            hits.Clear();Physics2D.BoxCast(origin,new Vector2(.002f,beamWidth),Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg,dir,filter,hits,maxDistance);
            RaycastHit2D nearest=default;float distance=maxDistance;
            foreach(var hit in hits){
                if(!hit.collider || hit.collider.transform.IsChildOf(transform))continue;
                if(hit.distance<=distance){distance=hit.distance;nearest=hit;}
            }
            BeamLength=distance;Show(true);
            var end=origin+dir*distance;SetLine(beam,origin,end);SetLine(core,origin,end);
            if(applyDamage && nearest.collider){var player=nearest.collider.GetComponentInParent<RobotEmotionGameplay>();if(player)player.DieAndRespawn();}
        }
        static void SetLine(LineRenderer line,Vector2 a,Vector2 b){if(line){line.SetPosition(0,a);line.SetPosition(1,b);}}
        void Show(bool value){if(beam)beam.enabled=value;if(core)core.enabled=value;}
        void OnDisable(){Show(false);}
    }
}
