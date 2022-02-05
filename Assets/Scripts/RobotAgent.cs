using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RobotAgent : Agent
{
    private Rigidbody rb;
    [SerializeField] private float robotSpeed = 10f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private GameObject[] robots;
    [SerializeField] private GameObject[] goals;
    private GameObject[] rings;
    private Transform[] goalTransorms;
    private Transform[] robotTransforms;
    private Transform[] ringTransforms;
    [SerializeField] private float time = 120f;
    private int score = 0; 

    public override void CollectObservations(VectorSensor sensor)
    {
        //Total Observation Size: 43 - 3 robot - 9 for other robots - 1 for time - 30 for rings
        //Collect this robots x and z position and y rotation
        sensor.AddObservation(gameObject.transform.position.x);
        sensor.AddObservation(gameObject.transform.position.z);
        sensor.AddObservation(gameObject.transform.rotation.y);
        //Collect the other 3 robots x and z position and y rotation
        foreach (GameObject g in robots) {
            sensor.AddObservation(g.transform.position.x);
            sensor.AddObservation(g.transform.position.z);
            sensor.AddObservation(g.transform.rotation.y);
        }
        //Collect the time
        sensor.AddObservation(time);
        //TODO - Collect the rings
        
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        float move = actions.ContinuousActions[0];
        float turnAngle = actions.ContinuousActions[1];

        Vector3 movement = transform.forward * robotSpeed * move * Time.deltaTime;
        rb.MovePosition(rb.position + movement);

        float turnValue = turnAngle * rotationSpeed * Time.deltaTime;
        Quaternion turn = Quaternion.Euler(0f, turnValue, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("starting episode");
        //Reset time
        time = 120 - 15;
        //Reset score
        score = 0;
        //Set Goals Back To Starting Position
        int i = 0;
        foreach (GameObject g in goals)
        {
            g.transform.localPosition = goalTransorms[i].localPosition;
            g.gameObject.SetActive(true);
            i++;
        }
        //Randomize Goals
        int amountOfGoals = Random.Range(0, goals.Length + 1);
        for (int y = 0; y < amountOfGoals; y++)
        {
            goals[y].SetActive(false);
        }
        //Set Robots back to starting positions
        int x = 0;
        foreach (GameObject r in robots)
        {
            r.transform.localPosition = robotTransforms[x].localPosition;
            x++;
        }

        //Reset rings
        int z = 0;
        foreach (GameObject ring in rings) {
            ring.transform.localPosition = ringTransforms[z].localPosition;
            z++;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Vector3 movement = transform.forward * robotSpeed * Input.GetAxis("Vertical") * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);

        float turnValue = Input.GetAxis("Horizontal") * rotationSpeed * Time.fixedDeltaTime;
        Quaternion turn = Quaternion.Euler(0f, turnValue, 0f);
        rb.MoveRotation(rb.rotation * turn);
    }

    void Start() {
        rb = this.gameObject.GetComponent<Rigidbody>();
        
        //Collect goal transforms in order to reset them each episode
        goalTransorms = new Transform[goals.Length];
        int i = 0;
        foreach (GameObject g in goals) {
            goalTransorms[i] = g.transform;
            i++;
        }

        //Collect robot transforms in order to reset them each episode
        robotTransforms = new Transform[robots.Length];
        int x = 0;
        foreach (GameObject r in robots) {
            robotTransforms[x] = r.transform;
            x++;
        }

        //Collect ring transforms in order to reset them each episode
        int y = 0;
        foreach (GameObject ring in rings) {
            ringTransforms[y] = ring.transform;
            y++;
        }
    }
    void FixedUpdate() {
        time -= Time.deltaTime;

        if((int) time == 0) {
            EndEpisode();
        }
    }
    
    void OnCollisionEnter(Collision collision) {
        if(collision.gameObject.tag == "Ring") {
            collision.gameObject.SetActive(false);
            score++;
        }
    }

    public float getTime() {
        return time;
    }

    public int getScore() {
        return score;
    } 
}
