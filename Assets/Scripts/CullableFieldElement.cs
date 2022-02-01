using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CullableFieldElement : MonoBehaviour
{
    public Material culledMaterial;

    private bool culled_ = false;
    public virtual bool culled
    {
        get {
            return culled_;
        }
        set {
            culled_ = value;
        }
    }
}
