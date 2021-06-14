using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour
{
    void Update()
    {
        if (transform.position.x <= -600f)
            transform.position += (Vector3.right * 1000);

        transform.Translate(Vector3.left * Time.deltaTime);
    }
}
