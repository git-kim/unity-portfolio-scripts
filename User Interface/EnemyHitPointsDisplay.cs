using UnityEngine;

namespace UserInterface
{
    public class EnemyHitPointsDisplay : HitAndManaPointsDisplay
    {
        // The screen UI's CanvasScaler matches a 700-pixel-wide reference; the bars follow the same scale.
        private const float ReferenceScreenWidth = 700f;
        // Up to this camera distance a bar keeps its size on screen; farther away it shrinks like its enemy does.
        private const float ConstantSizeDistance = 10f;

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
            var depth = Vector3.Dot(thisTransform.position - mainCameraTransform.position, mainCameraTransform.forward);
            var worldPerPixel = 2f * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Screen.height;
            thisTransform.localScale = initialLocalScale * (screenHeight * worldPerPixel * Mathf.Clamp(depth, 0.1f, ConstantSizeDistance));
        }

        private void UpdateRotationToLookAtCamera()
        {
            thisTransform.rotation = mainCameraTransform.rotation;
        }
    }
}