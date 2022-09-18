using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;

public class NicholasAgent : Agent
{

  ///////////////////
  //   Variables   //
  ///////////////////

  //Serialized Variables
  [Header("Robot Movement Settings")]
  [SerializeField] private float robotSpeed = 10f; //controls the multiplier of the movement speed
  [SerializeField] private float rotationSpeed = 10f; // controls the multiplier of the rotation speed

  [Header("MoGo Settings")]
  [SerializeField] private GameObject[] mogos; //array of all mogo game objects
  [SerializeField] private bool randomizeGoals = false; //whether or not to randomize enablization of goals

  [Header("AI Settings")] [Tooltip("the number of rings whose x and z position to observe")]
  [SerializeField] private int numRingsToObserve = 10; //the number of rings whose x and z position to observe

  [Header("Other Robots")]
  [SerializeField] private GameObject[] robots;//array of all 4 robots

  //Private variables
  private Rigidbody rb; //the rigibody of the robot
  private float time = 120f- 15f; //the time remaining in the game, total time is 120, with 15 autonomous period
  private GameObject[] rings; //array of gameobjects of rings 
  private GameObject[] sortedRings; //array of, sorted by distance, rings that the robot can see
  private float timeTogether = 0f; //the time of the robots together
  
  //Raycast variables
  [Header("Raycast Variables")]
  [SerializeField] private float maxRaycastDist = 20.0f; //the maximum distance of the raycast
  [SerializeField] private Collider visionCollider; //the collider of the robot
  private RaycastHit m_Hit; //the raycast hit of the robot
  private bool m_HitDetect; //whether or not the robot can see something
  

  //Tensorboard Tracking Variables
  private int totalReward = 0; //the total reward of the robot, 0.03f per ring, 0.2f per goal, -1f for penalty
  private int posReward = 0; //the reward for robot pos, -1f is wrong size, 0 otherwise
  private int ringReward = 0; //the total reward for rings that the robot got, 0.03f per ring
  private int mogoReward = 0; //the total reward for mobile goals that the robot collected
  private int pinningReward = 0; //the reward (penalty) for the robot pinnning the other

  //Initial Positions Of Objects
  private Transform[] mogoTransforms; //an array of all the mobile goals initial transforms
  private Transform[] ringTransforms; //an array of all rings sorted by closest to farthest distance to the robot
  private Transform[] robotTransforms; //an array of all the other robots transforms

  ///////////////////////////////
  //    A.I Model Functions    //
  ///////////////////////////////

  //Collection All Observations To Be Fed Into The Neural Network
  public override void CollectObservations(VectorSensor sensor)
  {
    //Add time left in the game to the model
    sensor.AddObservation(time); //the time left in the game
    
    //Add x & z pos of the robot to the model
    sensor.AddObservation(this.gameObject.transform.position.x); //x position of the robot
    sensor.AddObservation(this.gameObject.transform.position.z); //z position of the robot

    //Add x and z unit direction vector of the robot to the model
    Vector3 unitDirection = this.gameObject.transform.InverseTransformDirection(this.gameObject.transform.forward); //the unit direction vector of the robot
    sensor.AddObservation(unitDirection.x); //x unit direction of the robot
    sensor.AddObservation(unitDirection.z); //z unit direction of the robot

    //Collect Ring Observations
    ObserveRings(); //fill the ring gameobject array with rings sorted by distance
    
    //Add ring observations to the model
    foreach (GameObject ring in sortedRings) { //traverse sorted ring array
      sensor.AddObservation(ring.transform.position.x);//x pos of the ring
      sensor.AddObservation(ring.transform.position.z);//z pos of the ring
    }
  }

  //When a step is taken, the agent will execute the actions specified by the actions array
  public override void OnActionReceived(ActionBuffers actions)
  {
    //use action[0] to determine the forward speed of the robot
    float forwardSpeed = actions.ContinuousActions[0];
    //action[1] to determine the rotation speed of the robot
    float rotationSpeed = actions.ContinuousActions[1];

    //Move The Robot
    transform.Translate(Vector3.forward * forwardSpeed * robotSpeed * Time.deltaTime);//pos of robot
    transform.Rotate(Vector3.up * rotationSpeed * rotationSpeed * Time.deltaTime);//rot of robot
  }

  //Resets the playing field, including robots, rings, and goals
  //Also resets the time and score
  public override void OnEpisodeBegin()
  {
    //Reset time
    time = 120f - 15f; //120 total 15 for autonomous period
    
    //Reset Tensorboard tracking variables
    totalReward = 0; //reset the reward
    posReward = 0; //reset the position penalty reward
    ringReward = 0; //reset the ring reward 
    mogoReward = 0; //reset the mogo reward

    //Reset Positions of all mobile goals
    for(int i = 0; i < mogos.Length; i++) { //traverse all the mogos
      mogos[i].GetComponent<Transform>().position = mogoTransforms[i].position;//set the position of the mogo to stored init position
      mogos[i].GetComponent<Transform>().rotation = mogoTransforms[i].rotation;//set the rotation of the mogo to stored init rotation
    }

    //Reset positions of all ring
    for(int i = 0; i < rings.Length; i++) { //traverse all the rings
      rings[i].GetComponent<Transform>().position = ringTransforms[i].position;//set the position of the rings to stored init position
      rings[i].GetComponent<Transform>().rotation = ringTransforms[i].rotation;//set the rotation of the rings to stored init rotation
    }

    //Reset the position of the robots
    for (int i = 0; i < robots.Length; i++) { //traverse all the robots
      robots[i].GetComponent<Transform>().position = robotTransforms[i].position; //set the position of the robots to the stored initial position
      robots[i].GetComponent<Transform>().rotation = robotTransforms[i].rotation; //set the rotation of the robots to the stored initial rotation
    }

    //If randomization is true, randomize the enablization of the mogos
    foreach (GameObject mogo in mogos) {
      if (UnityEngine.Random.Range(0,2) == 0) { //if the random number is 0, enable the mogo
        mogo.SetActive(false);
      } else {
        mogo.SetActive(true);
      }
    }
  }

  //Allow for user control of the robot
  public override void Heuristic(in ActionBuffers actionsOut)
  {
    ActionSegment<float> control = actionsOut.ContinuousActions;
    control[0] = Input.GetAxis("Vertical");
    control[1] = Input.GetAxis("Horizontal");
  }


  ///////////////////////////
  //Game managing functions//
  ///////////////////////////

  void Start() {
    //Set the collider of the robot
    visionCollider = this.gameObject.GetComponent<Collider>();

    //Traverse the mogo game object array
    for (int i = 0; i < mogos.Length; i++) {
      mogoTransforms[i] = mogos[i].GetComponent<Transform>();//set the corresponding mogo transform to the mogos gameobject transform
    }

    //Traverse the ring game object array
    for (int i = 0; i < rings.Length; i++) {
      ringTransforms[i] = rings[i].GetComponent<Transform>();//set the corresponding ring transform to the rings transform 
    }

    //Traverse the robot game object array
    for (int i = 0; i < robots.Length; i++) {
      robotTransforms[i] = robots[i].GetComponent<Transform>(); //set the corresponding robot transforms to the robot transform
    }
  }


  void FixedUpdate() {
    //Subtract the time between the last time update
    time -= Time.fixedDeltaTime;

    //If the time is less than or equal to zero, end the episode
    if (time <= 0f) {
      LogVariables(); //log the variables to tensorboard for visualization
      EndEpisode(); //End the episode which will call OnEpisodeBegin() therefore resetting the field
    }

    //check if the pinning penalty should be applied
    //TODO use pinning penalty

  }

  private void LogVariables()
  {
    Academy.Instance.StatsRecorder.Add("Position Penatly Total", posReward);//logs the position penatly total
    Academy.Instance.StatsRecorder.Add("Ring Reward Total", ringReward);//logs the ring reward total 
    Academy.Instance.StatsRecorder.Add("Mogo Reward Total", mogoReward);//logs the mogo reward total
    Academy.Instance.StatsRecorder.Add("Pinning Penalty Total", pinningReward);//logs the pinning penalty 
  }

  private void ObserveRings()
  {
    GameObject[] temp_rings;//the temp array of rings that can be seen by the robot to be sorted later

    //TODO collect rings via raycasting from the robots perspective
    m_HitDetect = Physics.BoxCast(visionCollider.bounds.center, transform.localScale, transform.forward, out m_Hit, transform.rotation, maxRaycastDist);//check if the raycast hits anything
    if (m_HitDetect) {//if the raycast hits something
      Debug.Log("Hit : " + m_Hit.collider.name); //Output the name of the Collider your Box hit
    }
    //TODO sort ring array by nearest to farthest distance to the robot
    //use basic sorting function
  }
  //Draw the BoxCast as a gizmo to show where it currently is testing. Click the Gizmos button to see this
  void OnDrawGizmos()
  {
      Gizmos.color = Color.red;

      //Check if there has been a hit yet
      if (m_HitDetect)
      {
          //Draw a Ray forward from GameObject toward the hit
          Gizmos.DrawRay(transform.position, transform.forward * m_Hit.distance);
          //Draw a cube that extends to where the hit exists
          Gizmos.DrawWireCube(transform.position + transform.forward * m_Hit.distance, transform.localScale);
      }
      //If there hasn't been a hit yet, draw the ray at the maximum distance
      else
      {
          //Draw a Ray forward from GameObject toward the maximum distance
          Gizmos.DrawRay(transform.position, transform.forward * maxRaycastDist);
          //Draw a cube at the maximum distance
          Gizmos.DrawWireCube(transform.position + transform.forward * maxRaycastDist, transform.localScale);
      }
  }
}