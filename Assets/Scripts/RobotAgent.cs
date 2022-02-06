using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RobotAgent : Agent
{

    [Header("Robot Movement Settings")]
    [SerializeField] private float robotSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Ring Culling Settings")]
    [SerializeField] private bool visualizeCulling;
    [SerializeField] private Transform cameraLocation;
    [SerializeField] private float cullingFov = 60f;

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

    void Update()
    {
        CullRings();
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
                // get vector from camera to ring
                Vector3 direction = (ring.transform.position - cameraLocation.position).normalized;
                // get horizontal angle from camera to ring
                float angle = Vector3.Angle(new Vector3(direction.x, cameraLocation.position.y, direction.z), cameraLocation.forward);

                ring.GetComponent<CullableFieldElement>().culled = false;
                ringsCulled[i] = false;

                if (angle > cullingFov)
                {
                    ringsCulled[i] = true;
                    if (visualizeCulling)
                        ring.GetComponent<CullableFieldElement>().culled = true;
                }
                else
                {
                    if (Physics.Linecast(cameraLocation.position, ring.transform.position, out RaycastHit hit))
                    {
                        if (hit.collider.gameObject.transform.parent.gameObject != ring)
                        {
                            ringsCulled[i] = true;
                            if (visualizeCulling)
                                ring.GetComponent<CullableFieldElement>().culled = true;
                        }
                    }
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
        nearestRingsTemp.CopyTo(nearestRings);
    }
}
