using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(RobotMotor2D))]
    public sealed class RobotKeyboardInput : MonoBehaviour
    {
        RobotMotor2D motor;
        void Awake() { motor = GetComponent<RobotMotor2D>(); }
        public static float AxisFromKeys(bool a, bool d) { return (d ? 1 : 0) - (a ? 1 : 0); }
        void Update()
        {
            motor.SetMoveInput(AxisFromKeys(Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.D)));
            if (Input.GetKeyDown(KeyCode.Space)) motor.RequestJump();
        }
        void ClearInput() { if (motor) { motor.SetMoveInput(0); motor.CancelJumpRequest(); } }
        void OnDisable() { ClearInput(); }
        void OnApplicationFocus(bool focus) { if (!focus) ClearInput(); }
    }
}
