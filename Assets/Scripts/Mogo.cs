using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mogo : CullableFieldElement
{
    public Material blueMaterial;
    public Material redMaterial;
    public Material neutralMaterial;

    [SerializeField]
    private MeshRenderer colorMeshRenderer;

    public enum Alliance
    {
        Blue,
        Red,
        Neutral
    }

    public Alliance alliance;

    public override bool culled
    {
        set {
            base.culled = value;
            SetMogoMaterial();
        }
    }

    void OnValidate()
    {
        SetMogoMaterial();
    }

    void SetMogoMaterial()
    {
        if (culled)
        {
            colorMeshRenderer.material = culledMaterial;
        }

        switch (alliance)
        {
        case Alliance.Blue:
            colorMeshRenderer.material = blueMaterial;
            break;
        case Alliance.Red:
            colorMeshRenderer.material = redMaterial;
            break;
        case Alliance.Neutral:
            colorMeshRenderer.material = neutralMaterial;
            break;
        }
    }
}
