using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{

    public float shakeAmount = 0.25f;

    private float timer;

    private Vector3 originalPos;

    void Start()
    {
        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (timer > 0)
            transform.localPosition = originalPos + Random.insideUnitSphere * shakeAmount;
        else
            transform.localPosition = originalPos;

        if (timer > 0)
            timer -= Time.deltaTime;
    }

    public void Shake(float shakeTime)
    {
        timer = shakeTime;
    }
}
