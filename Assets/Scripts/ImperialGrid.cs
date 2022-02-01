using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ImperialGrid : MonoBehaviour
{
    public Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    void Update()
    {
        float m2f = 3.280839895f;
        float f2m = 1f / m2f;

        Vector3 offset = new Vector3(-6f * f2m, 0, -6f * f2m) + transform.position;

        // draw a 6x6 grid where each square is 2x2 feet
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                // draw a square
                Vector3 p1 = new Vector3(i * 2 * f2m, 0, j * 2 * f2m) + offset;
                Vector3 p2 = new Vector3(i * 2 * f2m, 0, (j + 1) * 2 * f2m) + offset;
                Vector3 p3 = new Vector3((i + 1) * 2 * f2m, 0, (j + 1) * 2 * f2m) + offset;
                Vector3 p4 = new Vector3((i + 1) * 2 * f2m, 0, j * 2 * f2m) + offset;

                Debug.DrawLine(p1, p2, color, 0f, false);
                Debug.DrawLine(p2, p3, color, 0f, false);
                Debug.DrawLine(p3, p4, color, 0f, false);
                Debug.DrawLine(p4, p1, color, 0f, false);
            }
        }
    }
}
