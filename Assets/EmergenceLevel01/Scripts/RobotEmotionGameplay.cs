using UnityEngine;
namespace Emergence.Level01
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(RobotMotor2D),typeof(RobotEmotionController))]
    public sealed class RobotEmotionGameplay : MonoBehaviour
    {
        public EmotionAbilityConfig abilities;
        public Grid levelGrid;
        public bool keyboardInput=true;
        [UnityEngine.Serialization.FormerlySerializedAs("showSelection")]
        public bool showSwitchProgress=true;
        public bool IsSwitching {get;private set;}
        public RobotEmotion CurrentEmotion {get;private set;}
        public RobotEmotion? TargetEmotion {get;private set;}
        public float Progress => IsSwitching ? Mathf.Clamp01((Time.time-started)/HoldSeconds) : 0;
        public bool CanUseEmotionAbilities => !DialogueDirector.IsBlockingInput && isActiveAndEnabled && !IsSwitching;
        public bool CanBreak => CanUseEmotionAbilities && (CurrentEmotion==RobotEmotion.Anger || CurrentEmotion==RobotEmotion.StrongAnger);
        [Tooltip("Optional checkpoint. If unset, respawn at the initial player position.")]
        public Transform respawnPoint;
        [Min(.1f)] public float electricGraceSeconds=1;
        public UnityEngine.Events.UnityEvent onElectrocuted=new UnityEngine.Events.UnityEvent();
        public UnityEngine.Events.UnityEvent onDied=new UnityEngine.Events.UnityEvent();
        Vector2 initialPosition;
        float safeUntil;
        public void SetRespawnPoint(Transform point){respawnPoint=point;}
        public bool HandleElectricContact(){
            if(!CanUseEmotionAbilities || CurrentEmotion==RobotEmotion.Courage || Time.time<safeUntil)return false;
            if(!DieAndRespawn())return false;
            onElectrocuted.Invoke();return true;
        }
        // Lethal hazards bypass emotion immunity and cancel any pending switch.
        public bool DieAndRespawn(){
            if(Time.time<safeUntil || !isActiveAndEnabled)return false;
            safeUntil=Time.time+electricGraceSeconds;
            CancelSwitch();
            motor.SetMoveInput(0);motor.CancelJumpRequest();
            body.position=respawnPoint?(Vector2)respawnPoint.position:initialPosition;body.velocity=Vector2.zero;
            motor.ResetGroundState();onDied.Invoke();return true;
        }
        public float JumpHeight => (CurrentEmotion==RobotEmotion.Joy && !IsSwitching ? HappyCells : BaseCells)*CellHeight*1.2f;
        float HoldSeconds => abilities ? Mathf.Max(.1f,abilities.holdSeconds):1;
        float BaseCells => abilities ? abilities.baseJumpCells:2f;
        float HappyCells => abilities ? abilities.happyJumpCells:3f;
        float CellHeight => levelGrid ? levelGrid.transform.TransformVector(new Vector3(0,levelGrid.cellSize.y,0)).magnitude : (abilities?abilities.fallbackCellHeight:1);
        RobotMotor2D motor;
        RobotEmotionController face;
        Rigidbody2D body;
        float started;
        Font font;
        readonly ContactPoint2D[] contacts=new ContactPoint2D[64];
        void Awake(){motor=GetComponent<RobotMotor2D>();face=GetComponent<RobotEmotionController>();body=GetComponent<Rigidbody2D>();CurrentEmotion=face.CurrentEmotion;initialPosition=transform.position;}
        void Start(){if(!levelGrid)levelGrid=FindObjectOfType<Grid>();}
        public bool BeginSwitch()
        {
            if(DialogueDirector.IsBlockingInput || !isActiveAndEnabled || IsSwitching || !motor.IsGrounded)return false;
            IsSwitching=true;TargetEmotion=NextEmotion(CurrentEmotion);started=Time.time;motor.SetControlsLocked(true);
            face.SetVisualEmotion(RobotEmotion.Blank);return true;
        }
        static RobotEmotion NextEmotion(RobotEmotion current)
        {
            switch(current) {
                case RobotEmotion.Joy: return RobotEmotion.Anger;
                case RobotEmotion.Anger: return RobotEmotion.Sadness;
                case RobotEmotion.Sadness: return RobotEmotion.StrongAnger;
                case RobotEmotion.StrongAnger: return RobotEmotion.Courage;
                default: return RobotEmotion.Joy;
            }
        }
        public bool ReleaseSwitch()
        {
            if(!IsSwitching)return false;
            var target=TargetEmotion;
            bool commit=Progress>=1 && motor.IsGrounded && target.HasValue;
            CancelSwitch();
            return commit && TrySetEmotion(target.Value);
        }
        public void CancelSwitch()
        {
            if(!IsSwitching)return;
            IsSwitching=false;TargetEmotion=null;motor.SetControlsLocked(false);face.SetVisualEmotion(CurrentEmotion);
        }
        // External gameplay/debug entry point; cannot bypass an in-progress switch.
        public bool TrySetEmotion(RobotEmotion value)
        {
            if(IsSwitching || !face.SetVisualEmotion(value))return false;
            CurrentEmotion=value;return true;
        }
        void Update()
        {
            if(IsSwitching && !motor.IsGrounded)CancelSwitch();
            if(keyboardInput) {
                // A fresh key-down is required, so holding Q cannot restart the cycle.
                if(Input.GetKeyDown(KeyCode.Q))BeginSwitch();
                if(IsSwitching && Input.GetKeyDown(KeyCode.Escape)){CancelSwitch();return;}
                if(IsSwitching && (Input.GetKeyUp(KeyCode.Q) || !Input.GetKey(KeyCode.Q)))ReleaseSwitch();
            }
            // Complete on the threshold frame, independently of key release.
            if(IsSwitching && Progress>=1)ReleaseSwitch();
        }

        void FixedUpdate()
        {
            if(!CanUseEmotionAbilities || !body)return;
            int n=body.GetContacts(contacts);
            for(int i=0;i<n;i++) {
                var hit=contacts[i];var other=hit.collider;
                if(other && other.attachedRigidbody==body)other=hit.otherCollider;
                if(!other)continue;
                var emotionBlock=other.GetComponentInParent<EmotionBlock>();
                if(emotionBlock && emotionBlock.TryInteract(this) && emotionBlock.kind==EmotionBlockKind.Electric)return;
                var emotionMap=other.GetComponentInParent<EmotionBlockMap>();
                if(emotionMap){bool respawned;emotionMap.TryInteractContact(hit.point,hit.normal,this,out respawned);if(respawned)return;}
                var block=other.GetComponentInParent<EmotionBreakable>();
                if(block)block.TryBreak(this);
                var tiles=other.GetComponentInParent<EmotionBreakableTilemap>();
                if(tiles)tiles.TryBreakContact(hit.point,hit.normal,this);
            }
        }
        void OnApplicationFocus(bool focus){if(!focus)CancelSwitch();}
        void OnDisable(){CancelSwitch();}
        void OnGUI()
        {
            if(!showSwitchProgress || DialogueDirector.IsBlockingInput)return;
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},18);
            var style=new GUIStyle(GUI.skin.label){font=font,fontSize=18,alignment=TextAnchor.MiddleCenter};
            if(!IsSwitching){GUI.Label(new Rect(10,Screen.height-82,Screen.width-20,30),"当前："+EmotionName(CurrentEmotion)+"  ·  长按 Q 切换  ·  空格跳跃",style);return;}
            float x=Screen.width*.5f,y=Screen.height-150;
            GUI.Box(new Rect(x-180,y-12,360,78),GUIContent.none);
            string name=EmotionName(TargetEmotion??CurrentEmotion);
            GUI.Label(new Rect(x-175,y-8,350,32),"下一情绪："+name+"  ·  "+Mathf.CeilToInt(Progress*100)+"%",style);
            var old=GUI.color;GUI.color=new Color(.3f,.85f,1);
            GUI.DrawTexture(new Rect(x-160,y+26,320*Progress,6),Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(new Rect(x-175,y+34,350,30),"蓄力满 1 秒自动切换 · Esc 取消",style);
        }
        public static string EmotionName(RobotEmotion value){switch(value){case RobotEmotion.Joy:return "开心";case RobotEmotion.Anger:return "愤怒";case RobotEmotion.Sadness:return "悲伤";case RobotEmotion.StrongAnger:return "加强愤怒";case RobotEmotion.Courage:return "勇气";default:return "空白";}}
        void OnDestroy(){if(font)Destroy(font);}
    }
}
