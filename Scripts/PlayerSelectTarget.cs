using UnityEngine;
using Cinemachine;

public class PlayerSelectTarget : MonoBehaviour
{
    Player player;
    Camera mainCamera;
    KeyManager KEY;
    Ray ray;
    RaycastHit hitInfo;

    void Start()
    {
        player = FindObjectOfType<Player>().GetComponent<Player>();
        mainCamera = Camera.main;
        KEY = KeyManager.Instance;
    }

    void Update()
    {
        if (KEY.LMBDown)
        {
            ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (UICamera.Raycast(Input.mousePosition)) // NGUI 객체가 선택되었을 때
            {
                 return;
            }

            if (Physics.SphereCast(ray, 0.8f, out hitInfo, 34f, 1 << 11)) // 11번 레이어: Enemy
            {
                if (Physics.Raycast(ray, hitInfo.distance, 1 << 9)) // 9번 레이어: Ground
                {
                    return;
                }

                player.SelectTarget(hitInfo.collider.gameObject);
            }
            else
            {
                player.DeselectTarget();
            }
        }
    }
}
