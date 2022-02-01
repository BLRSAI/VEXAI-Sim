using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Ring : CullableFieldElement
{
    MeshRenderer meshRenderer;
    Material originalMaterial;

    public override bool culled
    {
        get {
            return base.culled;
        }
        set {
            base.culled = value;

            if (value)
            {
                if (meshRenderer)
                    meshRenderer.material = culledMaterial;
            }
            else
            {
                meshRenderer.material = originalMaterial;
            }
        }
    }

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        originalMaterial = meshRenderer.material;
    }
}
