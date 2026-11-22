using UnityEngine;

namespace UserInterface
{
    public class EnemyHitPointsDisplay : HitAndManaPointsDisplay
    {
        // The screen UI's CanvasScaler matches a 700-pixel-wide reference; the bars follow the same scale.
        private const float ReferenceScreenWidth = 700f;

        private GameObject enemyGameObject;
        private Camera mainCamera;
        private Transform mainCameraTransform;
        private Transform enemyTransform;
        private Transform thisTransform;
        private float enemyHeight;
        private float heightOnScreen;
        private Vector3 initialLocalScale;

        public void Initialize(Transform enemyTransform, CharacterController characterController)
        {
            enemyGameObject = enemyTransform.gameObject;
            this.enemyTransform = enemyTransform;
            enemyHeight = characterController.height;

            thisTransform = transform;

            mainCamera = Camera.main;
            mainCameraTransform = mainCamera.SelfOrNull()?.transform;

            initialLocalScale = thisTransform.localScale;

            heightOnScreen = 30f;
        }

        private void LateUpdate()
        {
            if (!enemyGameObject.activeSelf)
                return;

            UpdatePosition();
            UpdateLocalScale();
            UpdateRotationToLookAtCamera();
        }

        private void UpdatePosition()
        {
            thisTransform.position =
                enemyTransform.position + (enemyHeight * enemyTransform.lossyScale.y + 0.5f) * Vector3.up;
        }

        private void UpdateLocalScale()
        {
            var screenHeight = heightOnScreen * Screen.width / ReferenceScreenWidth;
            var tempPos = mainCamera.ScreenToWorldPoint(mainCamera.WorldToScreenPoint(thisTransform.position) + Vector3.up * screenHeight);
            thisTransform.localScale = initialLocalScale * (tempPos - thisTransform.position).magnitude;
        }

        private void UpdateRotationToLookAtCamera()
        {
            thisTransform.rotation = mainCameraTransform.rotation;
        }
    }
}