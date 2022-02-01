using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
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
        get {
            return base.culled;
        }
        set {
            base.culled = value;
            SetMogoMaterial();
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SetMogoMaterial();
    }

    void SetMogoMaterial()
    {
        if (culled)
        {
            colorMeshRenderer.material = culledMaterial;
            return;
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
