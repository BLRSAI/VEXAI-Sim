using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.Mathematics;

public class RobotAgent : Agent
{
    [Header("Observation Settings")]
    [SerializeField] private Transform fieldPerspectiveTransform;

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
        float time = GameManager.gameManager.time;
        time = time / GameManager.maxTime;
        sensor.AddObservation(time);

        Vector3 pointingVector = fieldPerspectiveTransform.InverseTransformVector(transform.forward);

        // var observations = GameManager.gameManager.GetObservationsFromAlliancePerspective(this.gameObject);
        // var observationsArray = new Vector3[] { observations.Item1, observations.Item2, observations.Item3, observations.Item4 };

        // for (int i = 0; i < observationsArray.Length; i++)
        // {
        //     Vector3 observationLocal = localTransform.InverseTransformPoint(observationsArray[i]);
        //     sensor.AddObservation(observationLocal.x);
        //     sensor.AddObservation(observationLocal.z);
        // }

        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.z);
        sensor.AddObservation(pointingVector.x);
        sensor.AddObservation(pointingVector.z);

        CullRings();
        SortRings();

        for (int i = 0; i < numRings; i++)
        {
            Vector3 ringRelative = transform.InverseTransformPoint(nearestRings[i]); // relative to robot's local space
            // Vector3 ringLocal = fieldPerspectiveTransform.InverseTransformPoint(nearestRings[i]); // relative to robot's field perspective
            sensor.AddObservation(ringRelative.x);
            sensor.AddObservation(ringRelative.z);
        }

        var obs = this.GetObservations();
        Debug.Log(obs.Count);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        /* no need for math to be done, actions are alwasy between -1 and 1
        speed = Mathf.Min(1f, Mathf.Max(-1f, actions.ContinuousActions[0]));
        rotation = Mathf.Min(1f, Mathf.Max(-1f, actions.ContinuousActions[1]));
        */
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
                    if (Physics.Linecast(cameraLocation.position, ring.transform.position, out RaycastHit hit))
                    {
                        if (!hit.collider.gameObject.CompareTag("Ring Collider"))
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
