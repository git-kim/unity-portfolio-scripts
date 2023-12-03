using Characters;
using Managers;
using UnityEngine;

public class PlayerSelectTarget : MonoBehaviour
{
    private Player player;
    private Camera mainCamera;
    private KeyManager keyManagerInstance;
    private Ray ray;
    private RaycastHit hit;

    private void Start()
    {
        player = FindObjectOfType<Player>();
        mainCamera = Camera.main;
        keyManagerInstance = KeyManager.Instance;
    }

    private void Update()
    {
        if (!keyManagerInstance.LMBDown)
            return;

        if (player.IsDead)
            return;

        ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (UICamera.Raycast(Input.mousePosition)) // NGUI
        {
            return;
        }

        if (Physics.SphereCast(ray, 0.8f, out hit, 34f, 1 << 11)) // 11: Enemy
        {
            if (Physics.Raycast(ray, hit.distance, 1 << 9)) // 9: Ground
            {
                return;
            }

            player.SelectTarget(hit.collider.gameObject);
        }
        else
        {
            player.DeselectTarget();
        }
    }
}