using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Ring : CullableFieldElement
{
    public MeshRenderer meshRenderer;
    public Material originalMaterial;

    public override bool culled
    {
        get
        {
            return base.culled;
        }
        set
        {
            base.culled = value;

            if (value)
            {
                if (meshRenderer)
                    meshRenderer.sharedMaterial = culledMaterial;
            }
            else
            {
                if (meshRenderer)
                    meshRenderer.sharedMaterial = originalMaterial;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Intake"))
        {
            GameManager.gameManager.CollectRing(other.gameObject.transform.parent.gameObject);
            if (other.GetComponentInParent<RobotAgent>().ringFull == false) {
                gameObject.SetActive(false);
            } else { 
                gameObject.transform.position = other.GetComponentInParent<RobotAgent>().outtake.transform.position;
            }   
        }
    }
}
