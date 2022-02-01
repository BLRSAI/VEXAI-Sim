using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CullableFieldElement : MonoBehaviour
{
    public Material culledMaterial;

    public bool culledPublic = false;

    public virtual bool culled { get; set; }

    protected virtual void OnValidate()
    {
        culled = culledPublic;
    }
}
