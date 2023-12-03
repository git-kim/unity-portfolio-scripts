using System.Collections;
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

    private Coroutine doorsOpeningOrClosingRoutine;

    private Vector3 leftDoorInitialAngles;
    private Vector3 rightDoorInitialAngles;
    private float leftDoorEulerY;

    private void Start()
    {
        doorOpeningTrigger.ActionOnTriggerEnter = TryOpeningDoors;
        doorClosingTrigger.ActionOnTriggerEnter = TryClosingDoors;

        leftDoorInitialAngles = leftDoorTransform.rotation.eulerAngles;
        rightDoorInitialAngles = rightDoorTransform.rotation.eulerAngles;
        leftDoorEulerY = leftDoorInitialAngles.y;
    }

    private void OnDisable()
    {
        if (doorsOpeningOrClosingRoutine != null)
            StopCoroutine(doorsOpeningOrClosingRoutine);
        doorsOpeningOrClosingRoutine = null;
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