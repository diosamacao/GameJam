using System.Collections.Generic;
using UnityEngine;
namespace Emergence.Level01
{
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(RobotEmotionGameplay))]
    public sealed class EmotionResonance : MonoBehaviour
    {
        public static readonly List<EmotionResonance> Actors=new List<EmotionResonance>();
        [Min(.01f)] public float radius=5;
        [Header("共鸣光效")]
        public Color joyColor=new Color(1,.85f,.15f), angerColor=new Color(1,.2f,.15f), sadnessColor=new Color(.15f,.6f,1);
        public Color courageColor=new Color(1,.5f,.08f), guardianColor=new Color(.2f,1,.65f), memoryColor=new Color(.75f,.4f,1);
        [Min(.01f)] public float auraRadius=1.1f, auraWidth=.045f, pulseSpeed=3;
        [Range(0,1)] public float pulseAmount=.15f;
        public Vector2 visualCenter=new Vector2(0,.8f);
        [Header("开心二段跳（格数，不乘旧跳高倍率）")]
        [Min(0)] public float secondJumpMinimumCells=1, secondJumpMaximumCells=2;
        [Header("守护")]
        [Min(0)] public float shieldDistance=2;
        public Vector2 frontShieldSize=new Vector2(.15f,2);
        [Min(.1f)] public float fullShieldRadius=1.3f;
        [Min(.01f)] public float shieldLineWidth=.06f;
        [Range(12,128)] public int circleSegments=48;
        public int effectSortingOrder=35;
        public bool Enhanced {get;private set;}
        public RobotEmotionGameplay Actor {get;private set;}
        public RobotEmotion Emotion => Normalize(Actor.CurrentEmotion);
        public float SecondJumpMaximum=>Mathf.Max(0,secondJumpMaximumCells)*Actor.GridCellHeight;
        public float SecondJumpMinimum=>Mathf.Min(secondJumpMinimumCells,secondJumpMaximumCells)*Actor.GridCellHeight;
        LineRenderer aura,halo,shieldLine;GameObject shield;BoxCollider2D front;CircleCollider2D bubble;
        public static RobotEmotion Normalize(RobotEmotion value)=>value==RobotEmotion.StrongAnger?RobotEmotion.Anger:value;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetRegistry(){Actors.Clear();}
        void Awake(){Actor=GetComponent<RobotEmotionGameplay>();}
        void OnEnable(){if(!Actor)Actor=GetComponent<RobotEmotionGameplay>();if(!Actors.Contains(this))Actors.Add(this);}
        void OnDisable(){Actors.Remove(this);Enhanced=false;if(aura)aura.enabled=false;if(halo)halo.enabled=false;if(shield)shield.SetActive(false);}
        public bool Matches(EmotionResonance other){
            if(!other || other==this || !isActiveAndEnabled || !other.isActiveAndEnabled || !Actor.CanUseEmotionAbilities || !other.Actor.CanUseEmotionAbilities)return false;
            if(Actor.isNpc==other.Actor.isNpc || Emotion==RobotEmotion.Blank || Emotion==RobotEmotion.Loading || Emotion!=other.Emotion)return false;
            float range=Actor.isNpc?other.radius:radius;
            return ((Vector2)(transform.position-other.transform.position)).sqrMagnitude<=range*range;
        }
        public void RefreshState(){Enhanced=false;foreach(var other in Actors)if(Matches(other)){Enhanced=true;break;}}
        void Update(){RefreshState();DrawEffects();}
        public Color EmotionColor {get {switch(Emotion){case RobotEmotion.Joy:return joyColor;case RobotEmotion.Anger:return angerColor;case RobotEmotion.Sadness:return sadnessColor;case RobotEmotion.Courage:return courageColor;case RobotEmotion.Guardian:return guardianColor;case RobotEmotion.Memory:return memoryColor;default:return Color.white;}}}
        LineRenderer NewLine(string name,Transform parent){
            var go=new GameObject(name);go.transform.SetParent(parent,false);var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sortingOrder=effectSortingOrder;
            var face=GetComponent<RobotEmotionController>();if(face && face.bodyRenderer){line.sharedMaterial=face.bodyRenderer.sharedMaterial;line.sortingLayerID=face.bodyRenderer.sortingLayerID;}return line;
        }
        void Circle(LineRenderer line,float radius,float width,Color color){line.loop=true;line.positionCount=Mathf.Clamp(circleSegments,12,128);line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;for(int i=0;i<line.positionCount;i++){float a=i*Mathf.PI*2/line.positionCount;line.SetPosition(i,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius);}}
        void DrawEffects(){
            if(!aura){aura=NewLine("EmotionAura",transform);halo=NewLine("EmotionHalo",transform);}
            aura.enabled=halo.enabled=Enhanced;aura.transform.localPosition=halo.transform.localPosition=visualCenter;
            if(Enhanced){var color=EmotionColor;float pulse=1+Mathf.Sin(Time.time*pulseSpeed)*pulseAmount;Circle(aura,auraRadius*pulse,auraWidth,color);color.a*=.2f;Circle(halo,auraRadius*pulse,auraWidth*4,color);}
            bool protecting=Actor.CanUseEmotionAbilities && Emotion==RobotEmotion.Guardian;
            if(!protecting){if(shield)shield.SetActive(false);return;}
            if(!shield){shield=new GameObject("EmotionShield",typeof(EmotionShield));shield.transform.SetParent(transform,false);front=shield.AddComponent<BoxCollider2D>();bubble=shield.AddComponent<CircleCollider2D>();front.isTrigger=bubble.isTrigger=true;shieldLine=NewLine("ShieldVisual",shield.transform);}
            shield.SetActive(true);front.enabled=!Enhanced;bubble.enabled=Enhanced;var motor=GetComponent<RobotMotor2D>();float facing=motor.visual && motor.visual.flipX?-1:1;
            shield.transform.localPosition=(Vector3)visualCenter+(Enhanced?Vector3.zero:Vector3.right*facing*shieldDistance);
            if(Enhanced){bubble.radius=fullShieldRadius;Circle(shieldLine,fullShieldRadius,shieldLineWidth,EmotionColor);}
            else{front.size=frontShieldSize;shieldLine.loop=false;shieldLine.positionCount=2;shieldLine.SetPosition(0,Vector3.up*frontShieldSize.y*.5f);shieldLine.SetPosition(1,Vector3.down*frontShieldSize.y*.5f);shieldLine.startWidth=shieldLine.endWidth=frontShieldSize.x;shieldLine.startColor=shieldLine.endColor=EmotionColor;}
        }
        public static bool InEnhancedRange(Vector2 position,RobotEmotion emotion){foreach(var actor in Actors){actor.RefreshState();if(actor.Enhanced && actor.Emotion==emotion && Vector2.Distance(actor.transform.position,position)<=actor.radius)return true;}return false;}
        void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireSphere(transform.position,radius);}
    }
}
