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
        player = FindObjectOfType<Player>().GetComponent<Player>();
        mainCamera = Camera.main;
        keyManagerInstance = KeyManager.Instance;
    }

    private void Update()
    {
        if (!keyManagerInstance.LMBDown)
            return;

        ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (UICamera.Raycast(Input.mousePosition)) // NGUI 객체가 선택되었을 때
        {
            return;
        }

        if (Physics.SphereCast(ray, 0.8f, out hit, 34f, 1 << 11)) // 11번 레이어: Enemy
        {
            if (Physics.Raycast(ray, hit.distance, 1 << 9)) // 9번 레이어: Ground
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