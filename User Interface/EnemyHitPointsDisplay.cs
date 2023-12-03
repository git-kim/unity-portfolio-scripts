using UnityEngine;

public class EnemyHitPointsDisplay : HitAndManaPointsDisplay
{
    private GameObject enemy;
    private Camera mainCamera;
    private Transform mainCameraTransform;
    private Transform enemyTransform;
    private Transform enemyHitPointsDisplayTransform;
    private UIProgressBar enemyHitPointsBar;
    private float enemyHeight;
    private float heightOnScreen;
    private Vector3 enemyHitPointsDisplayInitialScale;

    private void Start()
    {
        enemy = FindObjectOfType<Enemy>().gameObject;
        enemyHeight = enemy.GetComponent<CharacterController>().height;
        enemyTransform = enemy.transform;
        enemyHitPointsDisplayTransform = gameObject.transform;
        enemyHitPointsBar = gameObject.GetComponent<UIProgressBar>();

        mainCamera = Camera.main;
        mainCameraTransform = mainCamera.SelfOrNull()?.transform;

        enemyHitPointsDisplayInitialScale = enemyHitPointsDisplayTransform.localScale;

        heightOnScreen = 30f;
    }

    private void LateUpdate()
    {
        if (!enemy.activeSelf) return;

        enemyHitPointsDisplayTransform.position =
            (enemyTransform.position + (enemyHeight * enemyTransform.lossyScale.y + 0.5f) * Vector3.up);

        UpdateBarLocalScale();

        LookAtCamera();
    }

    private void UpdateBarLocalScale()
    {
        var tempPos = mainCamera.ScreenToWorldPoint(mainCamera.WorldToScreenPoint(enemyHitPointsDisplayTransform.position) + Vector3.up * heightOnScreen);
        enemyHitPointsDisplayTransform.localScale = enemyHitPointsDisplayInitialScale * (tempPos - enemyHitPointsDisplayTransform.position).magnitude;
    }

    private void LookAtCamera()
    {
        enemyHitPointsDisplayTransform.rotation = mainCameraTransform.rotation;
    }
}