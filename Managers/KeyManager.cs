using Enums;
using UnityEngine;

namespace Managers
{
    public class KeyManager : Singleton<KeyManager>
    {
        private KeyManager() { }

        private const string VAxisName = "Vertical";
        private const string HAxisName = "Horizontal";
        private const string MouseWheelName = "Mouse ScrollWheel";

        private const string JumpName = "Jump";

        private const string BattlePoseName = "Battle Pose";
        private const string WalkOrRunName = "WalkOrRun";

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

            V = Input.GetAxis(VAxisName);
            H = Input.GetAxis(HAxisName);
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

            MouseWheel = Input.GetAxis(MouseWheelName);
            RMBDown = Input.GetMouseButtonDown(1);
            RMBUp = Input.GetMouseButtonUp(1);

            LMBDown = Input.GetMouseButtonDown(0);

            var jump = Input.GetButtonDown(JumpName);
            if (!jump && Jump && delayToResetJump > 0f)
            {
                delayToResetJump -= Time.deltaTime;
            }
            else
            {
                Jump = jump;
                delayToResetJump = 0.2f;
            }

            BattlePose = Input.GetButtonDown(BattlePoseName); // Battle Pose Toggle

            if (Input.GetButtonDown(WalkOrRunName))
                LocomotionMode ^= LocomotionMode.WalkAndRun;
        }
    }
}