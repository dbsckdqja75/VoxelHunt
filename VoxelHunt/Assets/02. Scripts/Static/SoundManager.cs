using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{

    public static SoundManager Instance { get { return instance != null ? instance : null; } }
    private static SoundManager instance = null;

    private bool isCoroutine = false, isLobby = false;

    private AudioSource bgm_AudioSource, effect_AudioSource;
    private AudioClip bgm_Clip;

    private float original_BgmVolume, original_EffectVolume;

    void Awake()
    {
        if (instance != null)
        {
            if (instance != this)
                Destroy(this.gameObject);
        }
        else
        {
            instance = this;

            DontDestroyOnLoad(this.gameObject);
        }

        Init();
    }

    void Init()
    {
        AudioSource[] audioSources = GetComponents<AudioSource>();

        bgm_AudioSource = audioSources[0];
        effect_AudioSource = audioSources[1];

        original_BgmVolume = bgm_AudioSource.volume;
        original_EffectVolume = effect_AudioSource.volume;
    }

    public void PlayMusic(AudioClip clip, bool isFade = false)
    {
        bgm_Clip = clip;

        if (isFade)
            StartCoroutine(FadeInMusic());
        else
        {
            bgm_AudioSource.Stop();
            bgm_AudioSource.clip = bgm_Clip;
            bgm_AudioSource.Play();
        }
    }

    public void StopMusic(bool isFade = false)
    {
        if (bgm_AudioSource.isPlaying)
        {
            if (isFade)
                StartCoroutine(FadeOutMusic());
            else
                bgm_AudioSource.Stop();
        }
    }

    public void PlayEffect(AudioClip clip, float volume = 0.4f) // 2D
    {
        if(effect_AudioSource.isPlaying)
        {
            GameObject effectAudio = new GameObject("Effect2D_Audio");
            AudioSource audioSource = effectAudio.AddComponent<AudioSource>();

            audioSource.volume = original_EffectVolume;
            audioSource.clip = clip;
            audioSource.Play();

            Destroy(effectAudio, clip.length);
        }
        else
        {
            effect_AudioSource.volume = original_EffectVolume;
            effect_AudioSource.clip = clip;
            effect_AudioSource.Play();
        }
    }

    public void PlayEffectPoint(Vector3 point, AudioClip clip, float volume = 1f, float minDistance = 5f, float maxDistance = 10f) // 3D
    {
        GameObject effectAudio = new GameObject("Effect3D_Audio");

        effectAudio.transform.position = point;

        AudioSource audioSource = effectAudio.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;

        audioSource.volume = volume;
        audioSource.clip = clip;
        audioSource.Play();

        Destroy(effectAudio, clip.length);
    }

    public void SetBgmVolume(float volume, bool isLobby = false)
    {
        if (this.isLobby != isLobby)
            return;

        original_BgmVolume = volume;

        if (!isCoroutine)
            bgm_AudioSource.volume = volume;
    }

    public void SetLobby(bool isOn)
    {
        isLobby = isOn;
    }

    IEnumerator FadeInMusic()
    {
        if(isCoroutine)
            yield break;

        yield return StartCoroutine(FadeOutMusic());

        isCoroutine = true;

        bgm_AudioSource.clip = bgm_Clip;

        bgm_AudioSource.Play();

        while (bgm_AudioSource.volume < original_BgmVolume)
        {
            bgm_AudioSource.volume += 0.1f * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        bgm_AudioSource.volume = original_BgmVolume;

        isCoroutine = false;

        yield break;
    }

    IEnumerator FadeOutMusic()
    {
        if (isCoroutine)
            yield break;

        isCoroutine = true;

        while (bgm_AudioSource.volume > 0f)
        {
            bgm_AudioSource.volume -= 0.1f * Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        bgm_AudioSource.volume = 0f;

        bgm_AudioSource.Stop();

        isCoroutine = false;

        yield break;
    }
}
