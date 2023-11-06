using UnityEngine;

public class SFX : MonoBehaviour
{
    public GameObject prefab;
    private GameObject sFX;
    private AudioSource audioSource;
    private float audioSourceVolumeFactor;
    private GameManager gameManagerInstance;

    private void Awake()
    {
        gameManagerInstance = GameManager.Instance;

        sFX = Instantiate(prefab, transform.position, Quaternion.identity);
        audioSource = sFX.GetComponent<AudioSource>();
        if (audioSource) audioSourceVolumeFactor = audioSource.volume;
        sFX.transform.SetParent(gameObject.transform);
    }

    private void OnEnable()
    {
        if (!audioSource)
            return;

        audioSource.volume = gameManagerInstance.MasterVolume * gameManagerInstance.SFXVolume * audioSourceVolumeFactor;
        audioSource.Play();
    }

    // Update is called once per frame
    private void OnDisable()
    {
        if (audioSource) audioSource.Stop();
    }
}