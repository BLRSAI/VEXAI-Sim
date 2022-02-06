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

    private Rigidbody rb;
    private bool[] ringsCulled;

    // running movement variables
    private float speed = 0f;
    private float rotation = 0f;

    void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // // Total Observation Size: 43 - 3 robot - 9 for other robots - 1 for time - 30 for rings
        // // Collect this robots x and z position and y rotation
        // sensor.AddObservation(gameObject.transform.position.x);
        // sensor.AddObservation(gameObject.transform.position.z);
        // sensor.AddObservation(gameObject.transform.rotation.y);
        // foreach (GameObject g in gm.robots)
        // {
        //     sensor.AddObservation(g.transform.position.x);
        //     sensor.AddObservation(g.transform.position.z);
        //     sensor.AddObservation(g.transform.rotation.y);
        // }
        // //Collect the time
        // sensor.AddObservation(gm.time);
        // //TODO - Collect the rings

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

        foreach (GameObject ring in GameManager.gameManager.rings)
        {
            if (ring.activeSelf)
            {
                Vector3 direction = ring.transform.position - cameraLocation.position;
                float angle = Vector3.Angle(direction, cameraLocation.forward);
                ring.GetComponent<CullableFieldElement>().culled = false;

                if (angle > cullingFov)
                {
                    if (visualizeCulling)
                    {
                        ring.GetComponent<CullableFieldElement>().culled = true;
                    }
                    else
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(cameraLocation.position, direction, out hit, Mathf.Infinity))
                        {
                            if (hit.collider.gameObject != ring)
                            {
                                ring.GetComponent<CullableFieldElement>().culled = true;
                            }
                            else
                            {
                                ring.GetComponent<CullableFieldElement>().culled = false;
                            }
                        }
                        else
                        {
                            ring.GetComponent<CullableFieldElement>().culled = false;
                        }
                    }
                }
            }
        }
    }
}
