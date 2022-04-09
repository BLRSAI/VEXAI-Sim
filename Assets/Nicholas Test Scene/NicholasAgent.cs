using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class NicholasAgent : Agent
{

  //Serialized Variables
  [Header("Robot Movement Settings")]
  [SerializeField] private float robotSpeed = 10f; //controls the multiplier of the movement speed
  [SerializeField] private float rotationSpeed = 10f; // controls the multiplier of the rotation speed

  [Header("MoGo Settings")]
  [SerializeField] private GameObject[] goals; //array of all mogo game objects
  [SerializeField] private bool randomizeGoals = false; //whether or not to randomize enablization of goals

  [Header("AI Settings")]
  [SerializeField] private int numRingsToObserve = 10; //the number of rings whose x and z position to obersve

 
  //Private variables
  private Rigidbody rb; //the rigibody of the robot
  private float time = 120f- 15f; //the time remaining in the game, total time is 120, with 15 autonomous period
  private int totalReward = 0; //the total reward of the robot, 0.03f per ring, 0.2f per goal, -1f for penalty

  //Collection All Observations To Be Fed Into The Neural Network
  public override void CollectObservations(VectorSensor sensor)
  {
    
  }

  //When a step is taken, the agent will execute the actions specified by the actions array
  public override void OnActionReceived(ActionBuffers actions)
  {
    //use action[0] to determine the forward speed of the robot and action[1] to determine the rotation speed of the robot
    float forwardSpeed = actions.ContinuousActions[0];
    float rotationSpeed = actions.ContinuousActions[1];

    //move the robot
    transform.Translate(Vector3.forward * forwardSpeed * robotSpeed * Time.deltaTime);
    transform.Rotate(Vector3.up * rotationSpeed * rotationSpeed * Time.deltaTime);
  }

  //Resets the playing field, including robots, rings, and goals
  //Also resets the time and score
  public override void OnEpisodeBegin()
  {
    //Reset time
    time = 0f;
    
    //Reset reward

  }

  public override void Heuristic(in ActionBuffers actionsOut)
  {
    
  }
}
