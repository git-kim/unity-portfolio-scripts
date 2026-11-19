using UnityEngine;

namespace UserInterface
{
    // Floating stat-change text: fades out once per activation, then returns itself to the pool by deactivating.
    [RequireComponent(typeof(UiFade))]
    public class StatChangePrefabController : MonoBehaviour
    {
        private UiFade fade;

        private void Awake()
        {
            fade = GetComponent<UiFade>();
            fade.Finished += () => gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            fade.Play();
        }
    }
}
