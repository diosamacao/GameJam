using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RobotMotor2D : MonoBehaviour
    {
        [Min(0)] public float moveSpeed = 5f;
        [Tooltip("Fallback jump height without RobotEmotionGameplay. With emotions enabled, configure EmotionAbilities instead.")]
        [Min(0)] public float jumpHeight = 2f;
        [Min(0)] public float minimumJumpHeight=1;
        public bool variableJump=true;
        [Tooltip("由NPC移动组件驱动位置，保留身体动画。")]
        public bool externallyDriven;
        public SpriteRenderer visual;
        public Sprite idle;
        public Sprite[] walk;
        public Sprite jump, fall, land;
        [Min(1)] public float walkFps = 8f;
        [Min(0)] public float landingDuration = .1f;
        [Header("落地与动画判定")]
        [Range(0,1)] public float groundNormalThreshold=.65f;
        [Min(0)] public float groundedVelocityTolerance=.1f;
        [Min(0)] public float risingAnimationThreshold=.1f;
        [Range(0,1)] public float walkInputThreshold=.01f;
        public bool IsGrounded { get; private set; }
        public bool ControlsLocked { get; private set; }
        public bool InputBlocked => GameMenuUI.BlocksGameplay || ControlsLocked || DialogueDirector.IsBlockingInput || LevelFlowController.IsTransitioning;
        RobotEmotionGameplay emotionGameplay;
        public float EffectiveJumpHeight => emotionGameplay && emotionGameplay.enabled ? emotionGameplay.JumpHeight : jumpHeight;
        public void SetControlsLocked(bool value) { ControlsLocked=value;if(value){input=0;jumpQueued=false;if(body)body.velocity=new Vector2(0,body.velocity.y);} }
        Rigidbody2D body;
        readonly ContactPoint2D[] contacts = new ContactPoint2D[32];
        float input, frameTime, landingUntil, localClock;
        bool jumpQueued,jumpHeld,jumpActive;int jumpsUsed;float takeoffY,activeMinimum;
        bool groundObserved;float nextFootstep;
        bool directedJump;float directedVelocityX;
        public void CancelDirectedJump(){directedJump=false;}
        public bool LaunchDirectedJump(Vector2 target,float apexHeight,float maximumHorizontalSpeed){
            if(InputBlocked || LocalTimeTarget.IsStopped(this) || !IsGrounded || !body)return false;
            float gravity=-Physics2D.gravity.y*body.gravityScale;if(gravity<=0)return false;
            var delta=target-body.position;float height=Mathf.Max(0,delta.y)+Mathf.Max(.1f,apexHeight);
            float vy=Mathf.Sqrt(2*gravity*height);float flight=(vy+Mathf.Sqrt(Mathf.Max(0,vy*vy-2*gravity*delta.y)))/gravity;
            float vx=delta.x/flight;if(Mathf.Abs(vx)>maximumHorizontalSpeed)return false;
            directedJump=true;directedVelocityX=vx;jumpActive=jumpQueued=false;IsGrounded=false;body.velocity=new Vector2(vx,vy);GameAudio.Movement(GameSound.Jump,emotionGameplay,transform.position);return true;
        }
        public void SetJumpHeld(bool held){jumpHeld=held;}
        void Awake() { body = GetComponent<Rigidbody2D>(); emotionGameplay=GetComponent<RobotEmotionGameplay>(); }
        public void SetMoveInput(float value) { input = InputBlocked ? 0 : Mathf.Clamp(value, -1f, 1f); }
        public bool RequestJump()
        {
            var powers=GetComponent<EmotionResonance>();
            bool second=!IsGrounded && jumpsUsed==1 && powers && powers.Enhanced && powers.Emotion==RobotEmotion.Joy;
            if (InputBlocked || LocalTimeTarget.IsStopped(this) || !isActiveAndEnabled || !body || (!IsGrounded && !second) || jumpQueued || EffectiveJumpHeight <= 0 || Physics2D.gravity.y * body.gravityScale >= 0) return false;
            jumpQueued = true;jumpHeld=true;
            return true;
        }
        public void ResetGroundState(){directedJump=false;IsGrounded=false;jumpQueued=false;jumpActive=false;jumpsUsed=0;landingUntil=0;}
        public void CancelJumpRequest() { jumpQueued = false;jumpHeld=false; }
        void FixedUpdate()
        {
            if(LocalTimeTarget.IsStopped(this))return;
            if(externallyDriven){IsGrounded=true;return;}
            bool wasGrounded = IsGrounded;
            IsGrounded = false;
            int count = body.GetContacts(contacts);
            Vector2 supportVelocity=Vector2.zero;
            for(int i=0;i<count;i++){
                if(contacts[i].normal.y<=groundNormalThreshold)continue;
                var other=contacts[i].collider;
                if(other && other.attachedRigidbody==body)other=contacts[i].otherCollider;
                var platform=other?other.GetComponentInParent<LocalMovingPlatform>():null;
                Vector2 velocity=platform && platform.isActiveAndEnabled?platform.MotionVelocity:Vector2.zero;
                if(body.velocity.y>velocity.y+groundedVelocityTolerance)continue;
                IsGrounded=true;supportVelocity=velocity;break;
            }
            if (!wasGrounded && IsGrounded){landingUntil=localClock+landingDuration;if(groundObserved)GameAudio.Movement(GameSound.Land,emotionGameplay,transform.position);}
            groundObserved=true;
            if(IsGrounded && !InputBlocked && Mathf.Abs(input)>walkInputThreshold && Time.time>=nextFootstep){nextFootstep=Time.time+GameAudio.FootstepInterval;GameAudio.Movement(GameSound.Footstep,emotionGameplay,transform.position);}
            if(IsGrounded){jumpsUsed=0;jumpActive=false;directedJump=false;}
            float vy = body.velocity.y;
            var powers=GetComponent<EmotionResonance>();
            bool second=!IsGrounded && jumpsUsed==1 && powers && powers.Enhanced && powers.Emotion==RobotEmotion.Joy;
            if (!InputBlocked && jumpQueued && (IsGrounded || second) && EffectiveJumpHeight > 0)
            {
                float gravity = -Physics2D.gravity.y * body.gravityScale;
                if (gravity > 0)
                {
                    float maximum=second ? powers.SecondJumpMaximum : EffectiveJumpHeight;
                    activeMinimum=Mathf.Clamp(second ? powers.SecondJumpMinimum : (emotionGameplay ? emotionGameplay.MinimumJumpHeight : minimumJumpHeight),0,maximum);
                    GameAudio.Movement(second?GameSound.DoubleJump:GameSound.Jump,emotionGameplay,transform.position);
                    vy = Mathf.Sqrt(2f * gravity * maximum);takeoffY=body.position.y;jumpActive=true;jumpsUsed++;
                    IsGrounded = false;
                    landingUntil = 0;
                }
            }
            jumpQueued = false;
            if(variableJump && jumpActive && !jumpHeld && vy>0){
                float gravity=-Physics2D.gravity.y*body.gravityScale;
                vy=Mathf.Min(vy,Mathf.Sqrt(2*Mathf.Max(0,gravity)*Mathf.Max(0,activeMinimum-(body.position.y-takeoffY))));jumpActive=false;
            }
            body.velocity = new Vector2(directedJump?directedVelocityX:(InputBlocked ? 0 : input * moveSpeed)+(IsGrounded?supportVelocity.x:0), vy);
        }
        void Update()
        {
            if(LocalTimeTarget.IsStopped(this))return;
            localClock+=Time.deltaTime;
            if (!visual) return;
            if (input != 0) visual.flipX = input < 0;
            if (!IsGrounded && body && (body.velocity.y > risingAnimationThreshold ? jump : fall))
            { visual.sprite = body.velocity.y > risingAnimationThreshold ? jump : fall; frameTime = 0; }
            else if (IsGrounded && localClock < landingUntil && land)
            { visual.sprite = land; frameTime = 0; }
            else if (Mathf.Abs(input) > walkInputThreshold && walk != null && walk.Length > 0)
            { frameTime += Time.deltaTime; visual.sprite = walk[Mathf.FloorToInt(frameTime * walkFps) % walk.Length]; }
            else { frameTime = 0; visual.sprite = idle; }
        }
        void OnDisable()
        {
            input = 0; jumpQueued = false; IsGrounded = false;jumpActive=false;jumpsUsed=0;
            if (body) body.velocity = new Vector2(0, body.velocity.y);
            if (visual && idle) visual.sprite = idle;
        }
    }
}
