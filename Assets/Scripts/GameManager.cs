using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager gameManager; //current instance of the gamemanger script

    public GameManager()
    {
        gameManager = this; //set the current instance of the gamemanager script
    }

    private struct PosAndRot 
    {
        public Vector3 pos; //position of the agent
        public Quaternion rot; //rotation of the agent

        public PosAndRot(Vector3 pos, Quaternion rot) 
        {
            this.pos = pos; //set the position of the agent
            this.rot = rot; //set the rotation of the agent
        }
    }

    public const float fieldSize = 12f * 0.3048f; //the size of the field in meters
    public const float halfFieldSize = fieldSize / 2f; //the half size of the field in meters
    public const float maxTime = 120f - 15f; //the maximum time in seconds
    //public const float maxRingXDist = 3.404f; //max 
    //public const float maxRingZDist = 3.404f;
    //public const float maxPosDist = 1.64f;

    [SerializeField] private Text timer; //the timer text
    [SerializeField] private Text scoreBlue; //the score text for blue
    [SerializeField] private Text scoreRed; //the score text for red

    [SerializeField] private GameObject blueAllianceRobot15; //the blue alliance robot 15
    [SerializeField] private GameObject blueAllianceRobot24; //the blue alliance robot 24
    [SerializeField] private GameObject redAllianceRobot15; //the red alliance robot 15
    [SerializeField] private GameObject redAllianceRobot24; //the red alliance robot 24
    private PosAndRot[] robotPositions; //the positions and rotations of the robots

    private Agent blueAgent; //the blue agent
    private Agent redAgent; //the red agent

    [SerializeField] private GameObject[] blueAllianceMogos; //the blue alliance mogos
    [SerializeField] private GameObject[] redAllianceMogos; //the red alliance mogos
    [SerializeField] private GameObject[] neutralMogos; //the neutral mogos
    private PosAndRot[] mogoPositions; //the positions and rotations of the mogos

    [Header("Reward Settings")]
    [SerializeField] private float pinningPenalty = 10f; //the penalty for pinning
    [SerializeField] private float mogoReward = 10f; //the reward for mogo
    [SerializeField] private float positionPenatly = 10f; //the penalty for position
    [SerializeField] private float ringReward = 3f; //the reward for ring
    [SerializeField] private bool randMogos = false; //whether or not to randomize the mogos
    [SerializeField] private int maxRingsPerGoal = 10; //the maximum number of rings per goal

    public GameObject[] rings { get; set; } //the rings
    private PosAndRot[] ringPositions; //the positions and rotations of the rings

    private const float initTime = 120f - 15f; //the initial time
    private const float no_man_zone_width = 0.6f; //the width of the no man zone on the field

    public float time { get; set; } = 0f;
    private int blueAllianceScore; //the blues score
    private int redAllianceScore;
    private float timeTogether = 0f;

    //The totals for all penalties/rewards calculated in the current episode to be logged
    private float bluePinningPenalty = 0f; 
    private float redPinningPenalty = 0f;
    private float redRingReward = 0f;
    private float blueRingReward = 0f;
    private float redMogoReward = 0f;
    private float blueMogoReward = 0f;
    private float bluePosPenalty = 0f;
    private float redPosPenalty = 0f;

    [SerializeField] private bool randomizeRingCount = false; //whether or not to randomize the number of rings per goal


    private PosAndRot getGameObjectPosAndRot(GameObject go) //get the position and rotation of the gameobject
    {
        return new PosAndRot(go.transform.position, go.transform.rotation);
    }

    private void setGameObjectPosAndRot(GameObject go, PosAndRot posAndRot) //set the position and rotation of the gameobject
    {
        go.transform.position = posAndRot.pos;
        go.transform.rotation = posAndRot.rot;
    }

    void Awake()
    {        
        GetRobotPos();// get robot positions
        
        GetMogoPos();// get mogo positions
        
        rings = GameObject.FindGameObjectsWithTag("Ring");// get rings
        
        GetRingPos();// get ring positions

        ResetField();// Reset the field
    }

    void ResetField()
    {
        time = 0f; //reset the time
        
        Random.InitState(System.DateTime.Now.Millisecond); //initialize random seed

        ResetRobotPos();// reset robot positions
        
        ResetMogoPos();// reset mogo positions
        
        ResetRingPos();// reset ring positions

        // reset score
        blueAllianceScore = 0;
        redAllianceScore = 0;

        //reset vars
        bluePinningPenalty = 0f;
        redPinningPenalty = 0f;
        redRingReward = 0f;
        blueRingReward = 0f;
        redMogoReward = 0f;
        blueMogoReward = 0f;
        bluePosPenalty = 0f;
        redPosPenalty = 0f;

        EnableMogos();// enable all mogos

    }

    void FixedUpdate()
    {
        var statsRecorder = Academy.Instance.StatsRecorder; //get the stats recorder for tensorboard logging

        //Timer Control
        time += Time.deltaTime;
        float min = Mathf.FloorToInt(time / 60);
        float sec = Mathf.FloorToInt(time % 60);
        timer.text = "Time: " + string.Format("{0:00}:{1:00}", min, sec);

        //Score Logging
        scoreBlue.text = "Blue Rings: " + blueAllianceScore.ToString();
        scoreRed.text = "Red Rings: " + redAllianceScore.ToString();

        if (time >= initTime) //once the end of the game is reached
        {
            EndGame(statsRecorder); //end the game
        }

    //Pinning rule
    if (Vector3.Distance(blueAgent.transform.position, redAgent.transform.position) < 0.5f)
        {
            timeTogether += Time.deltaTime; //increment the time together
        }
        else
        {
            timeTogether = 0; //once the robots are separated reset the time together
        }

        if (timeTogether >= 5f) //end the episode without giving out the endgame rewards 
        {
            //Give the pinning penalty
            blueAgent.AddReward(-pinningPenalty);
            redAgent.AddReward(-pinningPenalty);

            bluePinningPenalty -= pinningPenalty;
            redPinningPenalty -= pinningPenalty;

            Debug.Log("Pinning Penalty: -" + pinningPenalty);

            LogStats(statsRecorder);

            blueAgent.EndEpisode();
            redAgent.EndEpisode();

            ResetField();
        }
    }

  private void EndGame(StatsRecorder statsRecorder)
  {
    Vector3[] allMogoPos = new Vector3[blueAllianceMogos.Length + redAllianceMogos.Length + neutralMogos.Length]; //all mogos positions

    //get all mogos positions
    for (int i = 0; i < blueAllianceMogos.Length; i++)
      allMogoPos[i] = blueAllianceMogos[i].transform.position;
    for (int i = 0; i < redAllianceMogos.Length; i++)
      allMogoPos[i + blueAllianceMogos.Length] = redAllianceMogos[i].transform.position;
    for (int i = 0; i < neutralMogos.Length; i++)
      allMogoPos[i + blueAllianceMogos.Length + redAllianceMogos.Length] = neutralMogos[i].transform.position;


    //give mogo reward
    foreach (Vector3 mogoPos in allMogoPos)
    {
      if (mogoPos.z < -no_man_zone_width) //if the mogo is on the robots side
      {
        blueAgent.AddReward(mogoReward);
        blueMogoReward += mogoReward;
        Debug.Log("Blue mogo reward: 20f");
      }

      if (mogoPos.z > no_man_zone_width) //if the mogo is on the robots side
      {
        redAgent.AddReward(mogoReward);
        redMogoReward += 2f;
        Debug.Log("Red mogo reward: 20f");
      }
    }

    //Give the position penalty to the robots that end up on the other teams side
    if (redAgent.transform.position.z < no_man_zone_width)
    {
      redAgent.AddReward(-positionPenatly);
      redPosPenalty -= positionPenatly;
      Debug.Log("Red position penalty: -10f");
    }
    if (blueAgent.transform.position.z > -no_man_zone_width)
    {
      blueAgent.AddReward(-positionPenatly);
      bluePosPenalty -= positionPenatly;
      Debug.Log("Blue position penalty: -10f");
    }

    LogStats(statsRecorder); //log the stats for tensorboard

    blueAgent.EndEpisode(); //end the episode
    redAgent.EndEpisode(); //end the episode

    ResetField(); //reset the field
  }

  private void LogStats(StatsRecorder statsRecorder)
    {
        statsRecorder.Add("Blue Agent Mogo Reward", blueMogoReward);
        statsRecorder.Add("Red Agent Mogo Reward", redMogoReward);
        statsRecorder.Add("Red Agent Position Penalty", redPosPenalty);
        statsRecorder.Add("Blue Agent Position Penalty", bluePosPenalty);
        statsRecorder.Add("Blue Agent Pinning Penalty", bluePinningPenalty);
        statsRecorder.Add("Red Agent Pinning Penalty", redPinningPenalty);
        statsRecorder.Add("Blue Agent Ring Reward", blueRingReward);
        statsRecorder.Add("Red Agent Ring Reward", redRingReward);
    }

    public void CollectRing(GameObject robot) //collect the rings and give the reward
    {
        //if (robot == blueAllianceRobot15 || robot == blueAllianceRobot24)
        if (robot == blueAllianceRobot15)
        {
            blueAllianceScore++; //increase the number of rings collected by the robot
            if (blueAllianceScore <= maxRingsPerGoal) //as long as the goal isnt full, give the reward
            {
                blueAgent.AddReward(ringReward);
                //redAgent.AddReward(-5f); //give the other robot a penalty for letting the robot collect the ring

                //Debug.Log(string.Format("Blue Agent Ring Reward: {}f", ringReward));

                blueRingReward += ringReward;
            }
            else
            {
                blueAgent.gameObject.GetComponent<RobotAgent>().ringFull = true; //if goal is full, mark it as so
            }
            return;
        }
        //else if (robot == redAllianceRobot15 || robot == redAllianceRobot24)
        else if (robot == redAllianceRobot15)
        {
            redAllianceScore++;
            if (redAllianceScore <= maxRingsPerGoal)
            {
                redAgent.AddReward(ringReward);
                //blueAgent.AddReward(-3f);

                //Debug.Log(string.Format("Red Agent Ring Reward: {}f", ringReward));

                redRingReward += ringReward;
            }
            else
            {
                redAgent.gameObject.GetComponent<RobotAgent>().ringFull = true;
            }
            return;
        }

        throw new System.ArgumentException("Invalid robot");
    }

    
    private void GetRingPos() //Get the ring positions
    {
        ringPositions = new PosAndRot[rings.Length];
        for (int i = 0; i < rings.Length; i++)
            ringPositions[i] = getGameObjectPosAndRot(rings[i]);
    }

    
    private void GetMogoPos() //Get the mogo positions
    {
        mogoPositions = new PosAndRot[blueAllianceMogos.Length + redAllianceMogos.Length + neutralMogos.Length];

        int offsetLength = 0;
        for (int i = 0; i < blueAllianceMogos.Length; i++)
            mogoPositions[i] = getGameObjectPosAndRot(blueAllianceMogos[i]);
        offsetLength += blueAllianceMogos.Length;
        for (int i = 0; i < redAllianceMogos.Length; i++)
            mogoPositions[i + offsetLength] = getGameObjectPosAndRot(redAllianceMogos[i]);
        offsetLength += redAllianceMogos.Length;
        for (int i = 0; i < neutralMogos.Length; i++)
            mogoPositions[i + offsetLength] = getGameObjectPosAndRot(neutralMogos[i]);
    }

    private void GetRobotPos() //Get the robot positions
    {
        robotPositions = new PosAndRot[4];
        robotPositions[0] = getGameObjectPosAndRot(blueAllianceRobot15);
        robotPositions[1] = getGameObjectPosAndRot(blueAllianceRobot24);
        robotPositions[2] = getGameObjectPosAndRot(redAllianceRobot15);
        robotPositions[3] = getGameObjectPosAndRot(redAllianceRobot24);

        blueAgent = blueAllianceRobot15.GetComponent<Agent>();
        redAgent = redAllianceRobot15.GetComponent<Agent>();
    }

    private void EnableMogos() //Enabled all the mogos
    {
        for (int i = 0; i < blueAllianceMogos.Length; i++)
            blueAllianceMogos[i].SetActive(true);
        for (int i = 0; i < redAllianceMogos.Length; i++)
            redAllianceMogos[i].SetActive(true);
        for (int i = 0; i < neutralMogos.Length; i++)
            neutralMogos[i].SetActive(true);

        if (randMogos) //if the amount of active mogos is random, then set a random amount active
        {
            int blueAllianceMogoIndex = Random.Range(0, blueAllianceMogos.Length);
            int redAllianceMogoIndex = Random.Range(0, redAllianceMogos.Length);
            blueAllianceMogos[blueAllianceMogoIndex].SetActive(false);
            redAllianceMogos[redAllianceMogoIndex].SetActive(false);
        }


        List<int> list = new List<int>();
        for (int i = 0; i < neutralMogos.Length; i++)
            list.Add(i);
        list.Sort((x, y) => 1 - 2 * Random.Range(0, 1));

        //disable random amount of neutral mogos if the user chooses to
        if (randMogos)
        {
            int numToDisable = Random.Range(0, 1);
            for (int i = 0; i < numToDisable; i++)
                neutralMogos[list[i]].SetActive(false);
        }
    }

    
    private void ResetRingPos() //Reset all the ring positions
    {
        //Create a list of the rings
        var idx = new List<int>();
        for (int i = 0; i < rings.Length; i++)
        {
            idx.Add(i);
        }

        int numRings;
        if (randomizeRingCount)
        {
            idx.Sort((x, y) => 1 - 2 * Random.Range(0, 1));
            numRings = Random.Range(rings.Length / 4, rings.Length);
        }
        else
        {
            numRings = rings.Length;
        }

        for (int i = 0; i < rings.Length; i++)
        {
            setGameObjectPosAndRot(rings[idx[i]], ringPositions[idx[i]]);
            rings[idx[i]].GetComponent<Rigidbody>().ResetInertiaTensor();

            if (i < numRings)
                rings[idx[i]].SetActive(true);
            else
                rings[idx[i]].SetActive(false);
        }
    }

    
    private void ResetMogoPos() //Reset mogo positions
    {
        int offsetLength = 0;
        for (int i = 0; i < blueAllianceMogos.Length; i++)
            setGameObjectPosAndRot(blueAllianceMogos[i], mogoPositions[i]);
        offsetLength += blueAllianceMogos.Length;
        for (int i = 0; i < redAllianceMogos.Length; i++)
            setGameObjectPosAndRot(redAllianceMogos[i], mogoPositions[i + offsetLength]);
        offsetLength += redAllianceMogos.Length;
        for (int i = 0; i < neutralMogos.Length; i++)
            setGameObjectPosAndRot(neutralMogos[i], mogoPositions[i + offsetLength]);
    }

    
    private void ResetRobotPos() //Reset robot positions
    {
        setGameObjectPosAndRot(blueAllianceRobot15, robotPositions[0]);
        setGameObjectPosAndRot(blueAllianceRobot24, robotPositions[1]);
        setGameObjectPosAndRot(redAllianceRobot15, robotPositions[2]);
        setGameObjectPosAndRot(redAllianceRobot24, robotPositions[3]);
    }
}