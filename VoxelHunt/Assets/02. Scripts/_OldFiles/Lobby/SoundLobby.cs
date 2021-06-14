using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundLobby : MonoBehaviour
{

    private bool isChange;
    private AudioSource bgm_AudioSource, effect_AudioSource;

    public AudioClip[] bgm;

    void Awake()
    {
        isChange = false;

        AudioSource[] audioSources = GetComponents<AudioSource>();

        bgm_AudioSource = audioSources[0];
        effect_AudioSource = audioSources[1];

        bgm_AudioSource.clip = bgm[Random.Range(0, bgm.Length)];

        bgm_AudioSource.Play();
    }

    void Start()
    {
        LoadVolumeData();
    }

    void Update()
    {
        if (!bgm_AudioSource.isPlaying && !isChange)
        {
            isChange = true;

            Invoke("PlayMusic", Random.Range(3, 11));
        }
    }

    private void PlaySound(AudioSource audioSource, AudioClip audioClip)
    {
        audioSource.Stop(); // 문제 방지용
        audioSource.clip = audioClip;
        audioSource.Play();

        isChange = false;
    }

    public void PlayMusic()
    {
        PlaySound(bgm_AudioSource, bgm[Random.Range(0, bgm.Length)]);
    }

    public void PlayEffect(AudioClip audioClip)
    {
        if (effect_AudioSource.isPlaying) // 다중 이펙트 사운드 재생 (비효율)
        {
            GameObject emptyAudio = new GameObject("EmptyAudio");
            emptyAudio.AddComponent<AudioSource>();
            AudioSource audioSource = emptyAudio.GetComponent<AudioSource>();

            // AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            // audioSource_List.Add(audioSource);
            // 후처리가 문제겠지만 이런 방법도 있다! (관리하고 재사용하기 편할 것 같다)

            audioSource.volume = effect_AudioSource.volume;
            PlaySound(audioSource, audioClip);

            Destroy(emptyAudio, audioClip.length);
        }
        else // 단일 이펙트 사운드 재생
            PlaySound(effect_AudioSource, audioClip);
    }

    public void LoadVolumeData()
    {
        bgm_AudioSource.volume = DataManager.LoadDataToFloat("BGM_Volume_Data");
    }
}
