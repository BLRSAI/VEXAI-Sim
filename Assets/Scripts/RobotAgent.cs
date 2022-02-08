using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.Mathematics;

public class RobotAgent : Agent
{

    [Header("Robot Movement Settings")]
    [SerializeField] private float robotSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Ring Culling Settings")]
    [SerializeField] private bool visualizeCulling;
    [SerializeField] private Transform cameraLocation;
    [SerializeField] private float fx = 608.38952271f;
    [SerializeField] private float fy = 610.59807426f;
    [SerializeField] private float cx = 332.60130193f;
    [SerializeField] private float cy = 236.58507294f;
    [SerializeField] private float h = 480f;
    [SerializeField] private float w = 640f;

    [Header("AI Settings")]
    [SerializeField] private int numRings = 10;

    private Rigidbody rb;

    private bool[] ringsCulled;
    private Vector3[] nearestRings;

    // running movement variables
    private float speed = 0f;
    private float rotation = 0f;

    void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        (var allianceRobot24Pos, var opponentRobot15Pos, var opponentRobot24Pos) = GameManager.gameManager.GetObservationsFromAlliancePerspective(this.gameObject);

        Vector3[] observations = {
            this.transform.position,
            this.transform.forward,
            allianceRobot24Pos,
            opponentRobot15Pos,
            opponentRobot24Pos
        };

        for (int i = 0; i < observations.Length; i++)
        {
            sensor.AddObservation(observations[i]);
        }

        CullRings();
        SortRings();
        for (int i = 0; i < numRings; i++)
        {
            sensor.AddObservation(nearestRings[i]);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        speed = actions.ContinuousActions[0];
        rotation = actions.ContinuousActions[1];
    }

    void FixedUpdate()
    {
        Vector3 movement = transform.forward * robotSpeed * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        float turnValue = rotation * rotationSpeed * Time.fixedDeltaTime;
        Quaternion turn = Quaternion.Euler(0f, turnValue, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    public override void OnEpisodeBegin()
    {
        speed = 0;
        rotation = 0;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> control = actionsOut.ContinuousActions;
        control[0] = Input.GetAxis("Vertical");
        control[1] = Input.GetAxis("Horizontal");
    }


    public void CullRings()
    {
        if (ringsCulled == null) ringsCulled = new bool[GameManager.gameManager.rings.Length];

        for (int i = 0; i < ringsCulled.Length; i++)
        {
            GameObject ring = GameManager.gameManager.rings[i];

            if (ring.activeSelf)
            {
                if (visualizeCulling)
                    ring.GetComponent<CullableFieldElement>().culled = false;
                ringsCulled[i] = false;

                bool ringInFrame = true;

                Vector3 ringPosTransformed = cameraLocation.InverseTransformPoint(ring.transform.position);
                ringInFrame &= ringPosTransformed.z > 0;

                Vector3 projectedPos = new Vector3(
                    (fx * ringPosTransformed.x) + (cx * ringPosTransformed.z),
                    (fy * ringPosTransformed.y) + (cy * ringPosTransformed.z),
                    (ringPosTransformed.z)
                ) / ringPosTransformed.z;
                ringInFrame &= projectedPos.x > 0 && projectedPos.x < w && projectedPos.y > 0 && projectedPos.y < h;

                if (!ringInFrame)
                {
                    ringsCulled[i] = true;
                    if (visualizeCulling)
                        ring.GetComponent<CullableFieldElement>().culled = true;
                }
                else
                {
                    ringsCulled[i] = false;
                    if (visualizeCulling)
                        ring.GetComponent<CullableFieldElement>().culled = false;
                    // if (Physics.Linecast(cameraLocation.position, ring.transform.position, out RaycastHit hit))
                    // {
                    //     if (hit.collider.transform.parent != null && hit.collider.transform.parent.gameObject != ring)
                    //     {
                    //         ringsCulled[i] = true;
                    //         if (visualizeCulling)
                    //             ring.GetComponent<CullableFieldElement>().culled = true;
                    //     }
                    // }
                }
            }
        }
    }

    void SortRings()
    {
        var nearestRingsTemp = new List<Vector3>();
        for (int i = 0; i < GameManager.gameManager.rings.Length; i++)
        {
            if (!ringsCulled[i])
            {
                nearestRingsTemp.Add(GameManager.gameManager.rings[i].transform.position);
            }
        }

        nearestRingsTemp.Sort((x, y) => Vector3.Distance(x, this.transform.position).CompareTo(Vector3.Distance(y, this.transform.position)));

        if (nearestRings == null) nearestRings = new Vector3[numRings];
        for (int i = 0; i < numRings; i++)
        {
            if (i < nearestRingsTemp.Count)
            {
                nearestRings[i] = nearestRingsTemp[i];
            }
            else
            {
                nearestRings[i] = Vector3.zero;
            }
        }
    }
}
