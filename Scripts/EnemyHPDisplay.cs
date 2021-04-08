using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHPDisplay : MonoBehaviour
{
    GameObject enemy;
    Camera mainCamera;
    Transform mainCameraTransform;
    Transform enemyTransform;
    Transform enemyHPDisplayTransform;
    UIProgressBar enemyHPBar;
    float enemyHeight;
    float heightOnScreen;
    Vector3 enemyHPDisplayInitialScale;

    void Start()
    {
        enemy = FindObjectOfType<Enemy>().gameObject;
        enemyHeight = enemy.GetComponent<CharacterController>().height;
        enemyTransform = enemy.transform;
        enemyHPDisplayTransform = gameObject.transform;
        enemyHPBar = gameObject.GetComponent<UIProgressBar>();

        mainCamera = Camera.main;
        mainCameraTransform = mainCamera.transform;

        enemyHPDisplayInitialScale = enemyHPDisplayTransform.localScale; // 인스펙터로 지정된 로컬 스케일 값을 저장하여 둔다.

        heightOnScreen = 30f;
    }

    void LateUpdate()
    {
        if (!enemy.activeSelf) return;

        enemyHPDisplayTransform.position = (enemyTransform.position + (enemyHeight * enemyTransform.lossyScale.y + 0.5f) * Vector3.up);
        // 또는 enemyHPDisplayTransform.OverlayPosition((enemyTransform.position + (enemyHeight * enemyTransform.lossyScale.y + 0.4f) * Vector3.up), mainCamera, mainCamera);

        ScaleHPBar();

        LookAtCamera();
    }

    /// <summary>
    /// 거리와 무관하게 화면에 일정한 크기로 HP 바가 출력되도록 로컬 스케일을 조절한다.
    /// </summary>
    void ScaleHPBar()
    {
        Vector3 tempPos = mainCamera.ScreenToWorldPoint(mainCamera.WorldToScreenPoint(enemyHPDisplayTransform.position) + Vector3.up * heightOnScreen);
        enemyHPDisplayTransform.localScale = enemyHPDisplayInitialScale * (tempPos - enemyHPDisplayTransform.position).magnitude;
    }

    public void UpdateHPBar(int currentHP, int maxHP)
    {
        enemyHPBar.Set((float)currentHP / maxHP, false);
    }

    void LookAtCamera()
    {
        enemyHPDisplayTransform.rotation = mainCameraTransform.rotation;
        // 또는 enemyHPDisplayTransform.LookAt(enemyHPDisplayTransform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        // 또는 thisTransform.LookAt(mainCameraTransform); 후 thisTransform.forward = -thisTransform.forward;
    }
}
