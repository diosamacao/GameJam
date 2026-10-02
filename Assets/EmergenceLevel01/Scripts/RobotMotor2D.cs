using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RobotMotor2D : MonoBehaviour
    {
        [Min(0)] public float moveSpeed = 5f;
        public SpriteRenderer visual;
        public Sprite idle;
        public Sprite[] walk;
        [Min(1)] public float walkFps = 8f;
        Rigidbody2D body;
        float input, frameTime;
        void Awake() { body = GetComponent<Rigidbody2D>(); }
        public void SetMoveInput(float value) { input = Mathf.Clamp(value, -1f, 1f); }
        void FixedUpdate() { body.velocity = new Vector2(input * moveSpeed, body.velocity.y); }
        void Update()
        {
            if (!visual) return;
            if (input != 0) visual.flipX = input < 0;
            if (Mathf.Abs(input) > .01f && walk != null && walk.Length > 0)
            { frameTime += Time.deltaTime; visual.sprite = walk[Mathf.FloorToInt(frameTime * walkFps) % walk.Length]; }
            else { frameTime = 0; visual.sprite = idle; }
        }
        void OnDisable()
        { input = 0; if (body) body.velocity = new Vector2(0, body.velocity.y); if (visual && idle) visual.sprite = idle; }
    }
}
