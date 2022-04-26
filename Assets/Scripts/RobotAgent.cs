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
    [SerializeField] public GameObject outtake;
    [SerializeField] private float penatly24 = 1f;
    public bool ringFull = false;

    private Rigidbody rb;

    private bool[] ringsCulled;
    private Vector3[] nearestRings;
    private bool[] ringExists;

    // running movement variables
    private float speed = 0f;
    private float rotation = 0f;

    void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        string observations = "";
        float time = GameManager.gameManager.time;
        time = time / GameManager.maxTime;
        sensor.AddObservation(time);
        //observations += "Time: " + time.ToString() + ",";

        Vector3 pointingVector = fieldPerspectiveTransform.InverseTransformVector(transform.forward);

        //add noise to transform.position of the robot
        Vector3 noisyPosition = transform.position;
        noisyPosition.x += UnityEngine.Random.Range(-0.1f, 0.1f);
        noisyPosition.z += UnityEngine.Random.Range(-0.1f, 0.1f);

        /* gaussian noise
        float u1 = 1.0f - UnityEngine.Random.Range(0,1);
        float u2 = 1.0f - UnityEngine.Random.Range(0,1);
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
        float randNormal = transform.position.x + .05f * randStdNormal;
        */

        // sensor.AddObservation(noisyPosition.x);
        //observations += " Robot Position X: " + transform.position.x + ", ";
        // sensor.AddObservation(noisyPosition.z);
        //observations += " Robot Position Z: " + transform.position.z + ", ";
        // sensor.AddObservation(pointingVector.x);
        //observations += " Robot Direction X: " + pointingVector.x + ", ";
        // sensor.AddObservation(pointingVector.z);
        //observations += " Robot Direction Z: " + pointingVector.z + ", ";
        sensor.AddObservation(0.0f);
        sensor.AddObservation(0.0f);
        sensor.AddObservation(0.0f);
        sensor.AddObservation(0.0f);

        CullRings();
        SortRings();

        for (int i = 0; i < numRings; i++)
        {
            Vector3 ringRelative;
            if (ringExists[i])
            {
                ringRelative = transform.InverseTransformPoint(nearestRings[i]); // relative to robot's local space
            }
            else
            {
                ringRelative = new Vector3(0, 0, 0);
            }
            // Vector3 ringLocal = fieldPerspectiveTransform.InverseTransformPoint(nearestRings[i]); // relative to robot's field perspective

            //add noise to the ringRelative vector
            float noise = UnityEngine.Random.Range(-0.5f, 0.5f);
            ringRelative.x += noise;
            ringRelative.y += noise;

            sensor.AddObservation(ringRelative.x);
            observations += " Ring " + i + " Position X: " + ringRelative.x + ", ";
            sensor.AddObservation(ringRelative.z);
            observations += " Ring " + i + " Position Z: " + ringRelative.z + ", ";
        }
        //Debug.Log(observations);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // no need for math to be done, actions are alwasy between -1 and 1
        // speed = Mathf.Min(1f, Mathf.Max(-1f, actions.ContinuousActions[0]));
        // rotation = Mathf.Min(1f, Mathf.Max(-1f, actions.ContinuousActions[1]));
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
        if (ringExists == null) ringExists = new bool[numRings];
        for (int i = 0; i < numRings; i++)
        {
            if (i < nearestRingsTemp.Count)
            {
                nearestRings[i] = nearestRingsTemp[i];
                ringExists[i] = true;
            }
            else
            {
                nearestRings[i] = Vector3.zero;
                ringExists[i] = false;
            }
        }
    }

    // private void OnCollisionEnter(Collision other) {
    //     if (other.gameObject.CompareTag("24")) {
    //         AddReward(-penatly24);
    //     }
    // }
}
