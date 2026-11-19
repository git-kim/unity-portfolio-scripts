using System.Collections;
using System.Collections.Generic;
using Characters;
using Managers;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    private enum State
    {
        Open,
        Closed
    }

    private State doorsState = State.Closed;

    [SerializeField] private Transform leftDoorTransform;
    [SerializeField] private Transform rightDoorTransform;

    [SerializeField] private PlayerTrigger doorOpeningTrigger;
    [SerializeField] private PlayerTrigger doorClosingTrigger;

    [SerializeField] private float rotationSpeedInDegrees = 120f;

    [Tooltip("The doors stay locked until every enemy in this list is dead.")]
    [SerializeField] private List<Enemy> guardians = new List<Enemy>();
    [SerializeField] private string lockedMessage = "문을 지키는 해골 전사를 모두 처치해야 합니다.";
    [SerializeField] private string unlockedMessage = "보스 방의 문이 열렸습니다.";

    private Coroutine doorsOpeningOrClosingRoutine;

    private Vector3 leftDoorInitialAngles;
    private Vector3 rightDoorInitialAngles;
    private float leftDoorEulerY;

    public bool IsUnlocked => guardians.TrueForAll(guardian => !guardian || guardian.IsDead);

    private void Start()
    {
        doorOpeningTrigger.ActionOnTriggerEnter = OnPlayerReachedDoors;
        doorClosingTrigger.ActionOnTriggerEnter = TryClosingDoors;

        leftDoorInitialAngles = leftDoorTransform.rotation.eulerAngles;
        rightDoorInitialAngles = rightDoorTransform.rotation.eulerAngles;
        leftDoorEulerY = leftDoorInitialAngles.y;

        GameManager.Instance.EnemyDied += OnEnemyDied;
    }

    private void OnDestroy()
    {
        var gameManager = GameManager.ExistingInstance;
        if (gameManager)
            gameManager.EnemyDied -= OnEnemyDied;
    }

    private void OnDisable()
    {
        if (doorsOpeningOrClosingRoutine != null)
            StopCoroutine(doorsOpeningOrClosingRoutine);
        doorsOpeningOrClosingRoutine = null;
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (!guardians.Contains(enemy) || !IsUnlocked)
            return;

        GameManager.Instance.ShowMessage(unlockedMessage);
        TryOpeningDoors();
    }

    private void OnPlayerReachedDoors()
    {
        if (!IsUnlocked)
        {
            GameManager.Instance.ShowMessage(lockedMessage);
            return;
        }

        TryOpeningDoors();
    }

    private void TryOpeningDoors()
    {
        if (doorsState == State.Open)
            return;

        doorsState = State.Open;
        if (doorsOpeningOrClosingRoutine != null)
            StopCoroutine(doorsOpeningOrClosingRoutine);
        doorsOpeningOrClosingRoutine = StartCoroutine(OpenDoors());
    }

    private void TryClosingDoors()
    {
        if (doorsState == State.Closed)
            return;

        doorsState = State.Closed;
        if (doorsOpeningOrClosingRoutine != null)
            StopCoroutine(doorsOpeningOrClosingRoutine);
        doorsOpeningOrClosingRoutine = StartCoroutine(CloseDoors());
    }

    private IEnumerator OpenDoors()
    {
        var targetAngle = leftDoorInitialAngles.y + 75f;

        while (leftDoorEulerY < targetAngle)
        {
            leftDoorEulerY = Mathf.Min(targetAngle, leftDoorEulerY + rotationSpeedInDegrees * Time.deltaTime);
            leftDoorTransform.rotation = Quaternion.Euler(
                leftDoorInitialAngles.x, leftDoorEulerY, leftDoorInitialAngles.z);
            rightDoorTransform.rotation = Quaternion.Euler(
                rightDoorInitialAngles.x, -leftDoorEulerY, rightDoorInitialAngles.z);
            yield return null;
        }
    }

    private IEnumerator CloseDoors()
    {
        var targetAngle = leftDoorInitialAngles.y;

        while (leftDoorEulerY > targetAngle)
        {
            leftDoorEulerY = Mathf.Max(targetAngle, leftDoorEulerY - rotationSpeedInDegrees * Time.deltaTime);
            leftDoorTransform.rotation = Quaternion.Euler(
                leftDoorInitialAngles.x, leftDoorEulerY, leftDoorInitialAngles.z);
            rightDoorTransform.rotation = Quaternion.Euler(
                rightDoorInitialAngles.x, -leftDoorEulerY, rightDoorInitialAngles.z);
            yield return null;
        }
    }
}
