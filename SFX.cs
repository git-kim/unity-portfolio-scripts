using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFX : MonoBehaviour
{
    public GameObject prefab;
    GameObject sFX;
    AudioSource audioSource;
    float audioSourceVolumeFactor;
    GameManager GAME;

    void Awake()
    {
        GAME = GameManager.Instance;

        sFX = Instantiate(prefab, transform.position, Quaternion.identity);
        audioSource = sFX.GetComponent<AudioSource>();
        if (audioSource) audioSourceVolumeFactor = audioSource.volume;
        sFX.transform.SetParent(gameObject.transform);
    }

    void OnEnable()
    {
        if (audioSource)
        {
            audioSource.volume = GAME.MasterVolume * GAME.SFXVolume * audioSourceVolumeFactor;
            audioSource.Play();
        }
    }

    // Update is called once per frame
    void OnDisable()
    {
        if (audioSource) audioSource.Stop();
    }
}
