using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BoxWireframe : MonoBehaviour
{
    public Color color = Color.white;

    void Update()
    {
        // draw a box wireframe

        // first all the vertices
        Vector3 p1 = new Vector3(0, 0, 0);
        Vector3 p2 = new Vector3(0, 0, 1);
        Vector3 p3 = new Vector3(1, 0, 1);
        Vector3 p4 = new Vector3(1, 0, 0);
        Vector3 p5 = new Vector3(0, 1, 0);
        Vector3 p6 = new Vector3(0, 1, 1);
        Vector3 p7 = new Vector3(1, 1, 1);
        Vector3 p8 = new Vector3(1, 1, 0);

        // subtract 0.5 to center the box
        p1 -= 0.5f * Vector3.one;
        p2 -= 0.5f * Vector3.one;
        p3 -= 0.5f * Vector3.one;
        p4 -= 0.5f * Vector3.one;
        p5 -= 0.5f * Vector3.one;
        p6 -= 0.5f * Vector3.one;
        p7 -= 0.5f * Vector3.one;
        p8 -= 0.5f * Vector3.one;

        // transform by the object's transform
        p1 = transform.TransformPoint(p1);
        p2 = transform.TransformPoint(p2);
        p3 = transform.TransformPoint(p3);
        p4 = transform.TransformPoint(p4);
        p5 = transform.TransformPoint(p5);
        p6 = transform.TransformPoint(p6);
        p7 = transform.TransformPoint(p7);
        p8 = transform.TransformPoint(p8);

        Debug.DrawLine(p1, p2, color, 0f, false);
        Debug.DrawLine(p2, p3, color, 0f, false);
        Debug.DrawLine(p3, p4, color, 0f, false);
        Debug.DrawLine(p4, p1, color, 0f, false);

        Debug.DrawLine(p5, p6, color, 0f, false);
        Debug.DrawLine(p6, p7, color, 0f, false);
        Debug.DrawLine(p7, p8, color, 0f, false);
        Debug.DrawLine(p8, p5, color, 0f, false);

        Debug.DrawLine(p1, p5, color, 0f, false);
        Debug.DrawLine(p2, p6, color, 0f, false);
        Debug.DrawLine(p3, p7, color, 0f, false);
        Debug.DrawLine(p4, p8, color, 0f, false);
    }
}
