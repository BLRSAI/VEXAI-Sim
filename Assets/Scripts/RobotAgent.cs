using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RobotAgent : Agent
{

    [Header("Robot Movement Settings")]
    [SerializeField] private float robotSpeed = 10f; // The speed of the robot
    [SerializeField] private float rotationSpeed = 10f; // The speed of the robot's rotation

    [Header("Ring Culling Settings")]
    [SerializeField] private bool visualizeCulling; // Whether or not to visualize the culling
    [SerializeField] private Transform cameraLocation; // The location of the camera
    [SerializeField] private float fx = 608.38952271f; // The focal x length of the camera
    [SerializeField] private float fy = 610.59807426f; // The focal y length of the camera
    [SerializeField] private float cx = 332.60130193f; // The center x length of the camera
    [SerializeField] private float cy = 236.58507294f; // The center y length of the camera
    [SerializeField] private float h = 480f; // The height of the camera
    [SerializeField] private float w = 640f; // The width of the camera
 
    [Header("AI Settings")]
    [SerializeField] private int numRings = 10; // The number of rings the robot can pickup and gain points for
    [SerializeField] public GameObject outtake; // where the rings would end up if the robots mobile goal is full
    //[SerializeField] private float penatly24 = 1f; // The penalty for hitting ones own 24in robot //TODO: use this to penalize hitting the 24in robot

    public bool ringFull = false; // Whether or not the robots mobile goal is full

    private Rigidbody rb; // The rigidbody of the robot    
    private bool[] ringsCulled; // Whether or not the robot has culled the individual ring
    private Vector3[] nearestRings; // The culled rings sorted by distance
    private bool[] ringExists; // Whether or not the ring exists/picked up

    // running movement variables
    private float speed = 0f;
    private float rotation = 0f;

    //On Script Awake Get Ribigbody
    void Awake()
    {
        rb = this.gameObject.GetComponent<Rigidbody>(); // Get the rigidbody of the robot
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        string observations = "";

        //Get the time and normalize it
        float time = GameManager.gameManager.time;
        time = time / GameManager.maxTime;
        sensor.AddObservation(time);

        //add noise to transform.position of the robot
        Vector3 noisyPosition = transform.position;
        noisyPosition.x += UnityEngine.Random.Range(-0.1f, 0.1f);
        noisyPosition.z += UnityEngine.Random.Range(-0.1f, 0.1f);

        /* TODO implement gaussian noise
        float u1 = 1.0f - UnityEngine.Random.Range(0,1);
        float u2 = 1.0f - UnityEngine.Random.Range(0,1);
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)
        float randNormal = transform.position.x + .05f * randStdNormal;
        */

        sensor.AddObservation(noisyPosition.x);
        observations += " Robot Position X: " + transform.position.x + ", ";
        sensor.AddObservation(noisyPosition.z);
        observations += " Robot Position Z: " + transform.position.z + ", ";

        //Pointer vector
        //Vector3 pointingVector = fieldPerspectiveTransform.InverseTransformVector(transform.forward);
        //sensor.AddObservation(pointingVector.x);
        //observations += " Robot Direction X: " + pointingVector.x + ", ";
        //sensor.AddObservation(pointingVector.z);
        //observations += " Robot Direction Z: " + pointingVector.z + ", ";
        sensor.AddObservation(0.0f); //stopped using pointing vectors
        sensor.AddObservation(0.0f); //stopped using pointing vectors

        //Cull and Sort the Rings For Obervation
        CullRings();
        SortRings();

        //Add the Ring Observations
        for (int i = 0; i < numRings; i++)
        {
            //If the ring exists i.e not picked up, then give the observation of the ring relative to the robots position
            Vector3 ringRelative;
            if (ringExists[i])
            {
                ringRelative = transform.InverseTransformPoint(nearestRings[i]); // relative to robot's local space i.e robot's position is (0,0,0)
            }
            else
            {
                ringRelative = new Vector3(0, 0, 0); // if the ring doesn't exist, the robot doesn't see it
            }

            //add noise to the ringRelative vector
            float noise = UnityEngine.Random.Range(-0.25f, 0.25f);
            ringRelative.x += noise;
            ringRelative.y += noise;

            sensor.AddObservation(ringRelative.x);
            observations += " Ring " + i + " Position X: " + ringRelative.x + ", ";
            sensor.AddObservation(ringRelative.z);
            observations += " Ring " + i + " Position Z: " + ringRelative.z + ", ";
        }
        //Debug.Log(observations);
    }

    // When the model makes a decision, update the speed and rotation of the robot
    public override void OnActionReceived(ActionBuffers actions)
    {
        speed = actions.ContinuousActions[0];
        rotation = actions.ContinuousActions[1];
    }

    // Move the robot according to the speed and rotation decided on by the model 
    void FixedUpdate()
    {
        Vector3 movement = transform.forward * robotSpeed * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        float turnValue = rotation * rotationSpeed * Time.fixedDeltaTime;
        Quaternion turn = Quaternion.Euler(0f, turnValue, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    //Clear all variables and arrays used in calculations
    public override void OnEpisodeBegin()
    {
        speed = 0;
        rotation = 0;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        System.Array.Clear(ringExists, 0, ringExists.Length);
        System.Array.Clear(ringsCulled, 0, ringsCulled.Length);
        System.Array.Clear(nearestRings, 0, nearestRings.Length);
    }

    //If the user wants to control the robot, the following code will be used
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> control = actionsOut.ContinuousActions;
        control[0] = Input.GetAxis("Vertical");
        control[1] = Input.GetAxis("Horizontal");
    }

    // Cull the rings within the robot's view
    public void CullRings()
    {
        if (ringsCulled == null) ringsCulled = new bool[GameManager.gameManager.rings.Length]; // Initialize the array if it hasn't been initialized yet

        for (int i = 0; i < ringsCulled.Length; i++) //Traverse the ringsCulled array
        {
            GameObject ring = GameManager.gameManager.rings[i]; // Get the ring from the game managers list of all rings in the game

            if (ring.activeSelf) //if the ring is active
            {
                if (visualizeCulling) //if the user wants to visualize the culling
                    ring.GetComponent<CullableFieldElement>().culled = false; //set the ring to not culled
                ringsCulled[i] = false; //set the ring to not culled

                bool ringInFrame = true; //set the ring to be in the frame to true

                Vector3 ringPosTransformed = cameraLocation.InverseTransformPoint(ring.transform.position); //transform the ring's position from the camera's world space to local space
                ringInFrame &= ringPosTransformed.z > 0; //ring has to be in from of the camera in order to be in the frame

                //if the ring is in the frame, then check if it is within the field of view
                Vector3 projectedPos = new Vector3(
                    (fx * ringPosTransformed.x) + (cx * ringPosTransformed.z),
                    (fy * ringPosTransformed.y) + (cy * ringPosTransformed.z),
                    (ringPosTransformed.z)
                ) / ringPosTransformed.z;
                ringInFrame &= projectedPos.x > 0 && projectedPos.x < w && projectedPos.y > 0 && projectedPos.y < h; //ring has to be within the field of view

                if (!ringInFrame || Mathf.Abs(rotation) > 0.5f) //TODO why the rotation being above 0.5f??
                {
                    ringsCulled[i] = true;
                    if (visualizeCulling)
                        ring.GetComponent<CullableFieldElement>().culled = true;
                }
                else //if the ring is in the frame
                {
                    if (Physics.Linecast(cameraLocation.position, ring.transform.position, out RaycastHit hit)) //send out a linecast to the ring and detect if the ring was hit
                    {
                        if (!hit.collider.gameObject.CompareTag("Ring Collider")) //TODO why if NOT hit collider is Ring Collider??
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

    // Sort the rings according to their distance from the robot
    void SortRings()
    {
        var nearestRingsTemp = new List<Vector3>();
        for (int i = 0; i < GameManager.gameManager.rings.Length; i++) //Traverse the rings in the game
        {
            if (!ringsCulled[i]) //if the ring is not culled
            {
                nearestRingsTemp.Add(GameManager.gameManager.rings[i].transform.position); //add the ring to the list of nearest rings
            }
        }

        nearestRingsTemp.Sort((x, y) => Vector3.Distance(x, this.transform.position).CompareTo(Vector3.Distance(y, this.transform.position))); //sort the list of nearest rings according to their distance from the robot

        if (nearestRings == null) nearestRings = new Vector3[numRings]; // Initialize the array if it hasn't been initialized yet
        if (ringExists == null) ringExists = new bool[numRings]; // Initialize the array if it hasn't been initialized yet
        for (int i = 0; i < numRings; i++) //fill nearestRings with the sorted rings
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
}
