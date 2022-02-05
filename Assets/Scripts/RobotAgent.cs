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
    [SerializeField] private GameManager gm;
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
        //Reset game manager
        gm.Reset();
        //Reset score
        score = 0;
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
    }

    void Update() {
        if ((int) gm.time <= 0) {
            EndEpisode();
        }
    }
    
    void OnCollisionEnter(Collision collision) {
        if(collision.gameObject.tag == "Ring") {
            collision.gameObject.SetActive(false);
            score++;
        }
    }

    public int getScore() {
        return score;
    } 
}
