using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Rigidbody2D),typeof(LocalTimeTarget))]
    public sealed class LocalMovingPlatform : MonoBehaviour
    {
        [Tooltip("相对起始位置的终点偏移。")]
        public Vector2 travel=new Vector2(4,0);
        [Min(0)] public float speed=2;
        public bool pingPong=true;
        Rigidbody2D body;LocalTimeTarget timeTarget;Vector2 start;bool returning;
        void Awake(){body=GetComponent<Rigidbody2D>();timeTarget=GetComponent<LocalTimeTarget>();start=body.position;}
        void FixedUpdate(){if(timeTarget.Frozen)return;var target=returning?start:start+travel;body.MovePosition(Vector2.MoveTowards(body.position,target,speed*Time.fixedDeltaTime));if(Vector2.Distance(body.position,target)<.01f && pingPong)returning=!returning;}
    }
}
