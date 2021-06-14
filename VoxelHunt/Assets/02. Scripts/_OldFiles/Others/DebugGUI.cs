using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugGUI : MonoBehaviour
{

    public bool isAlwaysOn;

    public float radius = 1f;

    public Color color;

    void OnDrawGizmos()
    {
        if (!isAlwaysOn)
            return;

        Gizmos.color = color;

        Gizmos.DrawSphere(transform.position, radius);
    }

    void OnDrawGizmosSelected()
    {
        if (isAlwaysOn)
            return;

        Gizmos.color = color;

        Gizmos.DrawSphere(transform.position, radius);
    }
}
