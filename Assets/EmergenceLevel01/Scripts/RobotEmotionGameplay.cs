using UnityEngine;
namespace Emergence.Level01
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(RobotMotor2D),typeof(RobotEmotionController))]
    public sealed class RobotEmotionGameplay : MonoBehaviour
    {
        public EmotionAbilityConfig abilities;
        public Grid levelGrid;
        [Header("未指定能力配置时的备用参数")]
        [Min(.1f)] public float fallbackHoldSeconds=.5f;
        [Min(0)] public float fallbackBaseJumpCells=2;
        [Min(0)] public float fallbackHappyJumpCells=3;
        [Min(0)] public float fallbackMinimumJumpCells=1, fallbackHappyMinimumJumpCells=1.5f;
        [Min(.01f)] public float fallbackCellHeight=1;
        [Min(0)] public float fallbackJumpMultiplier=1.2f;
        [Header("按键")]
        public KeyCode joyKey=KeyCode.Alpha1, angerKey=KeyCode.Alpha2, sadnessKey=KeyCode.Alpha3;
        public KeyCode courageKey=KeyCode.Alpha4, guardianKey=KeyCode.Alpha5, memoryKey=KeyCode.Alpha6;
        public KeyCode cancelSwitchKey=KeyCode.Escape;
        public bool keyboardInput=true;
        [Tooltip("NPC不能自行切换情感，只能接受玩家传递。")]
        public bool isNpc;
        [Tooltip("玩家只能切换已由博士解锁的情感。关闭仅用于关卡调试。")]
        public bool requireEmotionUnlocks=true;
        public bool IsEmotionUnlocked(RobotEmotion emotion)=>!requireEmotionUnlocks || EmotionProgress.IsUnlocked(emotion);
        public void UnlockEmotion(RobotEmotion emotion){EmotionProgress.Unlock(emotion);}
        bool receiving;
        public RobotEmotion EffectiveEmotion {
            get {var resonance=GetComponent<EmotionResonance>();return CurrentEmotion==RobotEmotion.Anger && resonance && resonance.Enhanced?RobotEmotion.StrongAnger:CurrentEmotion;}
        }
        [UnityEngine.Serialization.FormerlySerializedAs("showSelection")]
        public bool showSwitchProgress=true;
        public bool IsSwitching {get;private set;}
        public RobotEmotion CurrentEmotion {get;private set;}
        public RobotEmotion? TargetEmotion {get;private set;}
        public float Progress => IsSwitching ? Mathf.Clamp01((Time.time-started)/HoldSeconds) : 0;
        public bool EmotionMechanicsEnabled=>MemoryNpcStateController.AllowsEmotion(this);
        public bool CanUseEmotionAbilities => EmotionMechanicsEnabled && !DialogueDirector.IsBlockingInput && !LevelFlowController.IsTransitioning && isActiveAndEnabled && !IsSwitching;
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
            if(Time.time<safeUntil || !isActiveAndEnabled || LevelFlowController.IsTransitioning || LocalTimeTarget.IsStopped(this))return false;
            safeUntil=Time.time+electricGraceSeconds;
            CancelSwitch();
            var transfer=GetComponent<EmotionTransfer>();if(transfer)transfer.CancelTransfer();
            motor.SetMoveInput(0);motor.CancelJumpRequest();
            var target=initialPosition;
            if(!isNpc && !SceneCheckpointState.TryGet(gameObject.scene,out target))target=respawnPoint?(Vector2)respawnPoint.position:initialPosition;
            EmotionResonance.InvalidateStates();transform.position=new Vector3(target.x,target.y,transform.position.z);body.position=target;body.velocity=Vector2.zero;body.angularVelocity=0;
            var npc=GetComponent<EmotionNpc>();if(isNpc && npc)npc.ResetRouteAfterDeath();
            motor.ResetGroundState();onDied.Invoke();return true;
        }
        public float JumpHeight => (EmotionMechanicsEnabled && CurrentEmotion==RobotEmotion.Joy && !IsSwitching ? HappyCells : BaseCells)*CellHeight*(abilities?abilities.jumpHeightMultiplier:fallbackJumpMultiplier);
        public float MinimumJumpHeight => Mathf.Min(JumpHeight,(abilities?(EmotionMechanicsEnabled && CurrentEmotion==RobotEmotion.Joy?abilities.happyMinimumJumpCells:abilities.minimumJumpCells):(EmotionMechanicsEnabled && CurrentEmotion==RobotEmotion.Joy?fallbackHappyMinimumJumpCells:fallbackMinimumJumpCells))*CellHeight*(abilities?abilities.jumpHeightMultiplier:fallbackJumpMultiplier));
        public float GridCellHeight => CellHeight;
        float HoldSeconds => abilities ? Mathf.Max(.1f,abilities.holdSeconds):Mathf.Max(.1f,fallbackHoldSeconds);
        float BaseCells => abilities ? abilities.baseJumpCells:fallbackBaseJumpCells;
        float HappyCells => abilities ? abilities.happyJumpCells:fallbackHappyJumpCells;
        float CellHeight => levelGrid ? levelGrid.transform.TransformVector(new Vector3(0,levelGrid.cellSize.y,0)).magnitude : (abilities?abilities.fallbackCellHeight:fallbackCellHeight);
        RobotMotor2D motor;
        RobotEmotionController face;
        Rigidbody2D body;
        float started;
        [Header("情绪提示布局（像素）")]
        [Min(1)] public int hudFontSize=18;
        public float hudSideMargin=10, statusBottomOffset=82, statusHeight=30, chargeBottomOffset=150;
        public Rect chargePanel=new Rect(-180,-12,360,78);
        public Rect chargeLabel=new Rect(-175,-8,350,32);
        public Rect progressBar=new Rect(-160,26,320,6);
        public Rect chargeHint=new Rect(-175,34,350,30);
        public Color progressColor=new Color(.3f,.85f,1);
        static Rect Offset(Rect rect,float x,float y){rect.position+=new Vector2(x,y);return rect;}
        Font font;
        readonly ContactPoint2D[] contacts=new ContactPoint2D[64];
        void Awake(){motor=GetComponent<RobotMotor2D>();face=GetComponent<RobotEmotionController>();body=GetComponent<Rigidbody2D>();CurrentEmotion=!isNpc && requireEmotionUnlocks?EmotionProgress.Current:face.CurrentEmotion;if(!isNpc)face.SetVisualEmotion(CurrentEmotion);initialPosition=transform.position;if(!isNpc && SceneCheckpointState.TryGet(gameObject.scene,out var checkpoint)){transform.position=new Vector3(checkpoint.x,checkpoint.y,transform.position.z);body.position=checkpoint;body.velocity=Vector2.zero;}}
        void Start(){if(!isNpc){EmotionProgress.CheckEnhancementScene(gameObject.scene.name,abilities?abilities.enhancementUnlockScene:"Level03_InitialLab",!abilities || abilities.unlockInLaterNumberedLevels);if(!GetComponent<MemoryScreenEffect>())gameObject.AddComponent<MemoryScreenEffect>();}if(!levelGrid)levelGrid=FindObjectOfType<Grid>();}
        public bool BeginSwitch()=>BeginSwitch(NextEmotion(CurrentEmotion));
        public bool BeginSwitch(RobotEmotion target)
        {
            if(isNpc || !CanUseEmotionAbilities || !motor.IsGrounded || !IsEmotionUnlocked(target))return false;
            if(target==CurrentEmotion || (target!=RobotEmotion.Joy && target!=RobotEmotion.Anger && target!=RobotEmotion.Sadness && target!=RobotEmotion.Courage && target!=RobotEmotion.Guardian && target!=RobotEmotion.Memory))return false;
            EmotionResonance.InvalidateStates();IsSwitching=true;TargetEmotion=target;started=Time.time;motor.SetControlsLocked(true);
            face.SetVisualEmotion(RobotEmotion.Loading);return true;
        }
        static RobotEmotion NextEmotion(RobotEmotion current)
        {
            switch(current) {
                case RobotEmotion.Joy: return RobotEmotion.Anger;
                case RobotEmotion.Anger: return RobotEmotion.Sadness;
                case RobotEmotion.Sadness: return RobotEmotion.Courage;
                case RobotEmotion.StrongAnger: return RobotEmotion.Courage;
                case RobotEmotion.Courage: return RobotEmotion.Guardian;
                case RobotEmotion.Guardian: return RobotEmotion.Memory;
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
            EmotionResonance.InvalidateStates();IsSwitching=false;TargetEmotion=null;motor.SetControlsLocked(false);face.SetVisualEmotion(CurrentEmotion);
        }
        // External gameplay/debug entry point; cannot bypass an in-progress switch.
        public bool TrySetEmotion(RobotEmotion value)
        {
            if((isNpc && !receiving) || (!isNpc && !IsEmotionUnlocked(value)) || IsSwitching || value==RobotEmotion.Loading || !face.SetVisualEmotion(value))return false;
            CurrentEmotion=value;EmotionResonance.InvalidateStates();if(!isNpc && requireEmotionUnlocks)EmotionProgress.Remember(value);return true;
        }
        public bool ReceiveEmotion(RobotEmotionGameplay source){
            if(!CanUseEmotionAbilities || LocalTimeTarget.IsStopped(this) || !isNpc || !source || source.isNpc || !source.CanUseEmotionAbilities)return false;
            var transfer=source.GetComponent<EmotionTransfer>();
            if(!isActiveAndEnabled || !transfer || Vector2.Distance(transform.position,source.transform.position)>transfer.transferRadius)return false;
            if(source.CurrentEmotion==RobotEmotion.Blank || source.CurrentEmotion==RobotEmotion.Loading)return false;
            receiving=true;try{return TrySetEmotion(source.CurrentEmotion==RobotEmotion.StrongAnger?RobotEmotion.Anger:source.CurrentEmotion);}finally{receiving=false;}
        }
        void Update()
        {
            if(IsSwitching && (!motor.IsGrounded || DialogueDirector.IsBlockingInput))CancelSwitch();
            if(keyboardInput) {
                if(Input.GetKeyDown(joyKey))BeginSwitch(RobotEmotion.Joy);
                else if(Input.GetKeyDown(angerKey))BeginSwitch(RobotEmotion.Anger);
                else if(Input.GetKeyDown(sadnessKey))BeginSwitch(RobotEmotion.Sadness);
                else if(Input.GetKeyDown(courageKey))BeginSwitch(RobotEmotion.Courage);
                else if(Input.GetKeyDown(guardianKey))BeginSwitch(RobotEmotion.Guardian);
                else if(Input.GetKeyDown(memoryKey))BeginSwitch(RobotEmotion.Memory);
                if(IsSwitching && Input.GetKeyDown(cancelSwitchKey)){CancelSwitch();return;}
            }
            // Complete on the threshold frame, independently of key release.
            if(IsSwitching && Progress>=1)ReleaseSwitch();
        }

        void FixedUpdate()
        {
            if(!CanUseEmotionAbilities || !body || LocalTimeTarget.IsStopped(this))return;
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
        void OnEnable(){EmotionResonance.InvalidateStates();}
        void OnDisable(){EmotionResonance.InvalidateStates();CancelSwitch();}
        void OnGUI()
        {
            if(!showSwitchProgress || DialogueDirector.IsBlockingInput)return;
            if(!font)font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},hudFontSize);
            var style=new GUIStyle(GUI.skin.label){font=font,fontSize=hudFontSize,alignment=TextAnchor.MiddleCenter};
            if(!IsSwitching){var resonance=GetComponent<EmotionResonance>();var transfer=GetComponent<EmotionTransfer>();string suffix=resonance && resonance.Enhanced?"（增强）":"";string interaction=transfer?"  ·  "+transfer.interactKey+" 长按传递  ·  "+transfer.interactionKey+" 交互":"";GUI.Label(new Rect(hudSideMargin,Screen.height-statusBottomOffset,Screen.width-hudSideMargin*2,statusHeight),"当前："+EmotionName(CurrentEmotion)+suffix+"  ·  "+UnlockedHints()+interaction,style);return;}
            float x=Screen.width*.5f,y=Screen.height-chargeBottomOffset;
            GUI.Box(Offset(chargePanel,x,y),GUIContent.none);
            string name=EmotionName(TargetEmotion??CurrentEmotion);
            GUI.Label(Offset(chargeLabel,x,y),"下一情绪："+name+"  ·  "+Mathf.CeilToInt(Progress*100)+"%",style);
            var old=GUI.color;GUI.color=progressColor;
            GUI.DrawTexture(new Rect(x+progressBar.x,y+progressBar.y,progressBar.width*Progress,progressBar.height),Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(Offset(chargeHint,x,y),"蓄力满 "+HoldSeconds.ToString("0.##")+" 秒自动切换 · "+cancelSwitchKey+" 取消",style);
        }
        string UnlockedHints(){
            string text="";
            var emotions=new[]{RobotEmotion.Joy,RobotEmotion.Anger,RobotEmotion.Sadness,RobotEmotion.Courage,RobotEmotion.Guardian,RobotEmotion.Memory};
            var keys=new[]{joyKey,angerKey,sadnessKey,courageKey,guardianKey,memoryKey};
            for(int i=0;i<emotions.Length;i++)if(IsEmotionUnlocked(emotions[i]))text+=KeyLabel(keys[i])+EmotionName(emotions[i])+" ";
            return text.Length>0?text:"情感尚未解锁 · 与博士交谈";
        }
        static string KeyLabel(KeyCode key)=>key.ToString().Replace("Alpha", "");
        public static string EmotionName(RobotEmotion value){switch(value){case RobotEmotion.Joy:return "开心";case RobotEmotion.Anger:return "愤怒";case RobotEmotion.Sadness:return "悲伤";case RobotEmotion.StrongAnger:return "加强愤怒";case RobotEmotion.Courage:return "勇气";case RobotEmotion.Guardian:return "守护";case RobotEmotion.Memory:return "怀念";case RobotEmotion.Loading:return "加载中";default:return "空白";}}
        void OnDestroy(){if(font)Destroy(font);}
    }
}
