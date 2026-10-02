using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(RobotMotor2D))]
    public sealed class RobotKeyboardInput : MonoBehaviour
    {
        RobotMotor2D motor;
        void Awake() { motor = GetComponent<RobotMotor2D>(); }
        public static float AxisFromKeys(bool a, bool d) { return (d ? 1 : 0) - (a ? 1 : 0); }
        void Update() { motor.SetMoveInput(AxisFromKeys(Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.D))); }
        void OnDisable() { if (motor) motor.SetMoveInput(0); }
        void OnApplicationFocus(bool focus) { if (!focus && motor) motor.SetMoveInput(0); }
    }
}
