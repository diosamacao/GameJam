using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(RobotMotor2D))]
    public sealed class RobotKeyboardInput : MonoBehaviour
    {
        public KeyCode leftKey=KeyCode.A, rightKey=KeyCode.D, jumpKey=KeyCode.Space;
        RobotMotor2D motor;
        void Awake() { motor = GetComponent<RobotMotor2D>(); }
        public static float AxisFromKeys(bool a, bool d) { return (d ? 1 : 0) - (a ? 1 : 0); }
        void Update()
        {
            motor.SetMoveInput(AxisFromKeys(Input.GetKey(leftKey), Input.GetKey(rightKey)));
            if (Input.GetKeyDown(jumpKey)) motor.RequestJump();
            motor.SetJumpHeld(Input.GetKey(jumpKey));
        }
        void ClearInput() { if (motor) { motor.SetMoveInput(0); motor.CancelJumpRequest(); } }
        void OnDisable() { ClearInput(); }
        void OnApplicationFocus(bool focus) { if (!focus) ClearInput(); }
    }
}
