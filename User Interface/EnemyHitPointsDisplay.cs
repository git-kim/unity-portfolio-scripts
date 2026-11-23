using Unity.Cinemachine;
using UnityEngine;

namespace UserInterface
{
    public class EnemyHitPointsDisplay : HitAndManaPointsDisplay
    {
        // The screen UI's CanvasScaler matches a 700-pixel-wide reference; the bars follow the same scale.
        private const float ReferenceScreenWidth = 700f;
        // Up to this camera distance a bar keeps its size on screen; farther away it shrinks like its enemy does.
        private const float ConstantSizeDistance = 10f;
        // Screen pixels (at the reference width) between the top of the head and the bar's center.
        private const float GapAboveHead = 30f;

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

        // Cinemachine moves the camera in LateUpdate too, in no fixed order with this script; following the
        // camera from a plain LateUpdate lags a frame behind while it turns. This event fires after the move.
        private void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);

        private void OnDisable() => CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);

        private void OnCameraUpdated(CinemachineBrain brain)
        {
            if (!enemyGameObject || !enemyGameObject.activeSelf)
                return;

            UpdatePosition();
            UpdateLocalScale();
            UpdateRotationToLookAtCamera();
        }

        private void UpdatePosition()
        {
            // Offset the bar on screen, not in the world: a world "up" offset slides sideways once the camera
            // looks steeply up or down, which pulled the bar away from its enemy.
            var head = enemyTransform.position + enemyHeight * enemyTransform.lossyScale.y * Vector3.up;
            var screenPoint = mainCamera.WorldToScreenPoint(head);
            if (screenPoint.z <= 0f) // behind the camera: nothing to show
            {
                thisTransform.position = head;
                return;
            }
            screenPoint.y += GapAboveHead * Screen.width / ReferenceScreenWidth * Mathf.Min(1f, ConstantSizeDistance / screenPoint.z);
            thisTransform.position = mainCamera.ScreenToWorldPoint(screenPoint);
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