using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate360 : MonoBehaviour
{

    public bool isRandom;

    public Vector3 direction = Vector3.up;

    public float speed;

    void Start()
    {
        if (isRandom)
            speed = Random.Range(0, 2) > 0? speed : -speed;
    }

    void Update()
    {
        transform.Rotate(direction * speed * Time.deltaTime);
    }
}
