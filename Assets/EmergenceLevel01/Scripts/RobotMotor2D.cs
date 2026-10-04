using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RobotMotor2D : MonoBehaviour
    {
        [Min(0)] public float moveSpeed = 5f;
        [Tooltip("Jump height in world units; can be changed while running.")]
        [Min(0)] public float jumpHeight = 2f;
        public SpriteRenderer visual;
        public Sprite idle;
        public Sprite[] walk;
        public Sprite jump, fall, land;
        [Min(1)] public float walkFps = 8f;
        [Min(0)] public float landingDuration = .1f;
        public bool IsGrounded { get; private set; }
        Rigidbody2D body;
        readonly ContactPoint2D[] contacts = new ContactPoint2D[32];
        float input, frameTime, landingUntil;
        bool jumpQueued;
        void Awake() { body = GetComponent<Rigidbody2D>(); }
        public void SetMoveInput(float value) { input = Mathf.Clamp(value, -1f, 1f); }
        public bool RequestJump()
        {
            if (!isActiveAndEnabled || !body || !IsGrounded || jumpQueued || jumpHeight <= 0 || Physics2D.gravity.y * body.gravityScale >= 0) return false;
            jumpQueued = true;
            return true;
        }
        public void CancelJumpRequest() { jumpQueued = false; }
        void FixedUpdate()
        {
            bool wasGrounded = IsGrounded;
            IsGrounded = false;
            int count = body.GetContacts(contacts);
            if (body.velocity.y <= .1f)
                for (int i = 0; i < count; i++)
                    if (contacts[i].normal.y > .65f) { IsGrounded = true; break; }
            if (!wasGrounded && IsGrounded) landingUntil = Time.time + landingDuration;
            float vy = body.velocity.y;
            if (jumpQueued && IsGrounded && jumpHeight > 0)
            {
                float gravity = -Physics2D.gravity.y * body.gravityScale;
                if (gravity > 0)
                {
                    vy = Mathf.Sqrt(2f * gravity * jumpHeight);
                    IsGrounded = false;
                    landingUntil = 0;
                }
            }
            jumpQueued = false;
            body.velocity = new Vector2(input * moveSpeed, vy);
        }
        void Update()
        {
            if (!visual) return;
            if (input != 0) visual.flipX = input < 0;
            if (!IsGrounded && body && (body.velocity.y > .1f ? jump : fall))
            { visual.sprite = body.velocity.y > .1f ? jump : fall; frameTime = 0; }
            else if (IsGrounded && Time.time < landingUntil && land)
            { visual.sprite = land; frameTime = 0; }
            else if (Mathf.Abs(input) > .01f && walk != null && walk.Length > 0)
            { frameTime += Time.deltaTime; visual.sprite = walk[Mathf.FloorToInt(frameTime * walkFps) % walk.Length]; }
            else { frameTime = 0; visual.sprite = idle; }
        }
        void OnDisable()
        {
            input = 0; jumpQueued = false; IsGrounded = false;
            if (body) body.velocity = new Vector2(0, body.velocity.y);
            if (visual && idle) visual.sprite = idle;
        }
    }
}
