using Enums;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Managers
{
    public class KeyManager : Singleton<KeyManager>
    {
        private KeyManager() { }

        // The old Input Manager axis settings (Horizontal/Vertical) so movement feels the same.
        private const float AxisSensitivity = 3f;
        private const float AxisGravity = 20f;

        // Raw smoothed axes, before the diagonal-release handling below.
        private float axisV, axisH;

        // These are used to rotate the player character properly.
        private float tempV, tempH;
        private float delayToResetVH;

        private float delayToResetJump; // Used to reserve a jump.

        public float V { get; private set; }
        public float H { get; private set; }
        public float MouseWheel { get; private set; }
        public bool LMBDown { get; private set; }
        public bool RMBDown { get; private set; }
        public bool RMBUp { get; private set; }
        public bool Jump { get; set; }
        public int Action { get; private set; }
        public bool Ult { get; private set; }
        public bool BattlePose { get; private set; }
        public LocomotionMode LocomotionMode { get; set; }

        private void ResetValues()
        {
            V = H = MouseWheel = 0f;
            axisV = axisH = 0f;
            tempV = tempH = 0f;
            delayToResetVH = 0.06f;
            delayToResetJump = 0.2f;
            LMBDown = RMBDown = RMBUp = false;
            Jump = BattlePose = false;
            Action = 0;
            LocomotionMode = LocomotionMode.Run;
        }

        private void Awake()
        {
            ResetValues();
        }

        private void Update()
        {
            //if (GameManagerInstance.State == GameState.Over)
            //{
            //    ResetValues();
            //    return;
            //}

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            axisV = SmoothAxis(axisV, Direction(keyboard?.wKey, keyboard?.upArrowKey, keyboard?.sKey, keyboard?.downArrowKey));
            axisH = SmoothAxis(axisH, Direction(keyboard?.dKey, keyboard?.rightArrowKey, keyboard?.aKey, keyboard?.leftArrowKey));
            V = axisV;
            H = axisH;
            if (Mathf.Abs(V) > 0.001f && Mathf.Abs(H) > 0.001f)
            {
                tempV = V;
                tempH = H;
                delayToResetVH = 0.06f;
            }
            else if (Mathf.Abs(V) > 0.001f && Mathf.Abs(tempH) > 0.001f)
            {
                H = tempH;
                delayToResetVH -= Time.deltaTime;
                if (delayToResetVH < 0f)
                {
                    tempH = tempV = 0f;
                    delayToResetVH = 0.06f;
                }
            }
            else if (Mathf.Abs(H) > 0.001f && Mathf.Abs(tempV) > 0.001f)
            {
                V = tempV;
                delayToResetVH -= Time.deltaTime;
                if (delayToResetVH < 0f)
                {
                    tempH = tempV = 0f;
                    delayToResetVH = 0.06f;
                }
            }
            else
            {
                tempH = tempV = 0f;
                delayToResetVH = 0.06f;
            }

            MouseWheel = mouse == null ? 0f : mouse.scroll.ReadValue().y * 0.1f; // 0.1 per notch, as the old axis gave
            RMBDown = mouse != null && mouse.rightButton.wasPressedThisFrame;
            RMBUp = mouse != null && mouse.rightButton.wasReleasedThisFrame;

            LMBDown = mouse != null && mouse.leftButton.wasPressedThisFrame;

            var jump = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
            if (!jump && Jump && delayToResetJump > 0f)
            {
                delayToResetJump -= Time.deltaTime;
            }
            else
            {
                Jump = jump;
                delayToResetJump = 0.2f;
            }

            BattlePose = keyboard != null && keyboard.zKey.wasPressedThisFrame; // Battle Pose Toggle

            if (keyboard != null && keyboard.slashKey.wasPressedThisFrame)
                LocomotionMode ^= LocomotionMode.WalkAndRun;
        }

        private static float Direction(KeyControl positive, KeyControl altPositive, KeyControl negative, KeyControl altNegative)
        {
            var value = 0f;
            if (positive != null && (positive.isPressed || altPositive.isPressed)) value += 1f;
            if (negative != null && (negative.isPressed || altNegative.isPressed)) value -= 1f;
            return value;
        }

        // Mirrors Input.GetAxis with snap on: reversing jumps to zero, pressing ramps up, releasing falls back fast.
        private static float SmoothAxis(float current, float target)
        {
            if (target != 0f && current * target < 0f)
                current = 0f;
            var speed = target == 0f ? AxisGravity : AxisSensitivity;
            return Mathf.MoveTowards(current, target, speed * Time.deltaTime);
        }
    }
}