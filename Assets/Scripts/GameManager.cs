using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager gameManager;

    public GameManager()
    {
        gameManager = this;
    }

    private struct PosAndRot
    {
        public Vector3 pos;
        public Quaternion rot;

        public PosAndRot(Vector3 pos, Quaternion rot)
        {
            this.pos = pos;
            this.rot = rot;
        }
    }

    public const float fieldSize = 12f * 0.3048f;
    public const float halfFieldSize = fieldSize / 2f;
    public const float maxTime = 120f - 15f;
    public const float maxRingXDist = 3.404f;
    public const float maxRingZDist = 3.404f;
    public const float maxPosDist = 1.64f;

    [SerializeField] private Text timer;
    [SerializeField] private Text scoreBlue;
    [SerializeField] private Text scoreRed;

    [SerializeField] private GameObject blueAllianceRobot15;
    [SerializeField] private GameObject blueAllianceRobot24;
    [SerializeField] private GameObject redAllianceRobot15;
    [SerializeField] private GameObject redAllianceRobot24;
    private PosAndRot[] robotPositions;

    private Agent blueAgent;
    private Agent redAgent;

    [SerializeField] private GameObject[] blueAllianceMogos;
    [SerializeField] private GameObject[] redAllianceMogos;
    [SerializeField] private GameObject[] neutralMogos;
    private PosAndRot[] mogoPositions;

    [Header("Reward Settings")]
    [SerializeField] private float pinningPenalty = 10f;
    [SerializeField] private float mogoReward = 10f;
    [SerializeField] private float positionPenatly = 10f;
    [SerializeField] private float ringReward = 3f;
    [SerializeField] private bool randMogos = false;
    [SerializeField] private int maxRingsPerGoal = 10;

    public GameObject[] rings { get; set; }
    private PosAndRot[] ringPositions;

    private const float initTime = 120f - 15f;
    private const float no_man_zone_width = 0.6f;

    public float time { get; set; } = initTime;
    private int blueAllianceScore;
    private int redAllianceScore;
    private float timeTogether = 0f;

    private float bluePinningPenalty = 0f;
    private float redPinningPenalty = 0f;
    private float redRingReward = 0f;
    private float blueRingReward = 0f;
    private float redMogoReward = 0f;
    private float blueMogoReward = 0f;
    private float bluePosPenalty = 0f;
    private float redPosPenalty = 0f;


    private PosAndRot getGameObjectPosAndRot(GameObject go)
    {
        return new PosAndRot(go.transform.position, go.transform.rotation);
    }

    private void setGameObjectPosAndRot(GameObject go, PosAndRot posAndRot)
    {
        go.transform.position = posAndRot.pos;
        go.transform.rotation = posAndRot.rot;
    }

    void Awake()
    {
        // get robot positions
        GetRobotPos();

        // get mogo positions
        GetMogoPos();

        // get rings
        rings = GameObject.FindGameObjectsWithTag("Ring");

        // get ring positions
        GetRingPos();

        ResetField();
    }

    void ResetField()
    {
        time = initTime;

        // reset robot positions
        ResetRobotPos();


        // reset mogo positions
        ResetMogoPos();


        // reset ring positions
        ResetRingPos();

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


        // enable all mogos
        EnableMogos();

    }

    void FixedUpdate()
    {
        var statsRecorder = Academy.Instance.StatsRecorder;
        //Timer Control
        time -= Time.deltaTime;
        float min = Mathf.FloorToInt(time / 60);
        float sec = Mathf.FloorToInt(time % 60);
        timer.text = "Time: " + string.Format("{0:00}:{1:00}", min, sec);

        scoreBlue.text = "Blue Rings: " + blueAllianceScore.ToString();
        scoreRed.text = "Red Rings: " + redAllianceScore.ToString();

        if (time <= 0)
        {

            Vector3[] allMogoPos = new Vector3[blueAllianceMogos.Length + redAllianceMogos.Length + neutralMogos.Length];
            for (int i = 0; i < blueAllianceMogos.Length; i++)
                allMogoPos[i] = blueAllianceMogos[i].transform.position;
            for (int i = 0; i < redAllianceMogos.Length; i++)
                allMogoPos[i + blueAllianceMogos.Length] = redAllianceMogos[i].transform.position;
            for (int i = 0; i < neutralMogos.Length; i++)
                allMogoPos[i + blueAllianceMogos.Length + redAllianceMogos.Length] = neutralMogos[i].transform.position;


            //give mogo reward
            foreach (Vector3 mogoPos in allMogoPos)
            {
                if (mogoPos.z < -no_man_zone_width)
                {
                    blueAgent.AddReward(mogoReward);
                    blueMogoReward += mogoReward;
                    Debug.Log("Blue mogo reward: 20f");
                }
                if (mogoPos.z > no_man_zone_width)
                {
                    redAgent.AddReward(mogoReward);
                    redMogoReward += 2f;
                    Debug.Log("Red mogo reward: 20f");
                }
            }

            //End of game rules - cannot be on other teams side to end game
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

            LogStats(statsRecorder);

            blueAgent.EndEpisode();
            redAgent.EndEpisode();

            ResetField();
        }

        //Pinning rule
        if (Vector3.Distance(blueAgent.transform.position, redAgent.transform.position) < 0.5f)
        {
            timeTogether += Time.deltaTime;
        }
        else
        {
            timeTogether = 0;
        }

        if (timeTogether >= 5f)
        {
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

    public void CollectRing(GameObject robot)
    {
        if (robot == blueAllianceRobot15 || robot == blueAllianceRobot24)
        {
            blueAllianceScore++;
            if (blueAllianceScore <= maxRingsPerGoal)
            {
                blueAgent.AddReward(ringReward);
                //redAgent.AddReward(-5f);

                Debug.Log("Blue Agent Ring Reward: .03f");

                blueRingReward += ringReward;
            } else {
                blueAgent.gameObject.GetComponent<RobotAgent>().ringFull = true;
            }
            return;
        }
        else if (robot == redAllianceRobot15 || robot == redAllianceRobot24)
        {
            redAllianceScore++;
            if (redAllianceScore <= maxRingsPerGoal)
            {
                redAgent.AddReward(ringReward);
                //blueAgent.AddReward(-3f);

                Debug.Log("Red Agent Ring Reward: .03f");

                redRingReward += ringReward;
            } else {
                redAgent.gameObject.GetComponent<RobotAgent>().ringFull = true;
            }
            return;
        }

        throw new System.ArgumentException("Invalid robot");
    }

    public (Vector3, Vector3, Vector3, Vector3) GetObservationsFromAlliancePerspective(GameObject robot)
    {
        GameObject allianceSecondary;
        GameObject opponentPrimary;
        GameObject opponentSecondary;

        if (robot == blueAllianceRobot15)
        {
            allianceSecondary = blueAllianceRobot24;
            opponentPrimary = redAllianceRobot15;
            opponentSecondary = redAllianceRobot24;
        }
        else if (robot == blueAllianceRobot24)
        {
            allianceSecondary = blueAllianceRobot15;
            opponentPrimary = redAllianceRobot15;
            opponentSecondary = redAllianceRobot24;
        }
        else if (robot == redAllianceRobot15)
        {
            allianceSecondary = redAllianceRobot24;
            opponentPrimary = blueAllianceRobot15;
            opponentSecondary = blueAllianceRobot24;
        }
        else if (robot == redAllianceRobot24)
        {
            allianceSecondary = redAllianceRobot15;
            opponentPrimary = blueAllianceRobot15;
            opponentSecondary = blueAllianceRobot24;
        }
        else
        {
            throw new System.ArgumentException("Invalid robot");
        }

        return (
            robot.transform.position,
            allianceSecondary.transform.position,
            opponentPrimary.transform.position,
            opponentSecondary.transform.position
        );
    }

    public System.Func<Vector3, Vector3> VectorAllianceTransform(GameObject robot)
    {
        if (robot != blueAllianceRobot15 && robot != blueAllianceRobot24 && robot != redAllianceRobot15 && robot != redAllianceRobot24)
        {
            throw new System.ArgumentException("Invalid robot");
        }

        return (Vector3 input) => input;
    }

    public System.Func<Vector3, Vector3> CombinedPositionTransform(GameObject robot)
    {
        var allianceTransform = VectorAllianceTransform(robot);
        System.Func<Vector3, Vector3> scalePositionVector = (Vector3 input) => input / halfFieldSize;

        return (Vector3 input) => allianceTransform(scalePositionVector(input));
    }

    private void GetRingPos()
    {
        ringPositions = new PosAndRot[rings.Length];
        for (int i = 0; i < rings.Length; i++)
            ringPositions[i] = getGameObjectPosAndRot(rings[i]);
    }

    private void GetMogoPos()
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

    private void GetRobotPos()
    {
        robotPositions = new PosAndRot[4];
        robotPositions[0] = getGameObjectPosAndRot(blueAllianceRobot15);
        robotPositions[1] = getGameObjectPosAndRot(blueAllianceRobot24);
        robotPositions[2] = getGameObjectPosAndRot(redAllianceRobot15);
        robotPositions[3] = getGameObjectPosAndRot(redAllianceRobot24);

        blueAgent = blueAllianceRobot15.GetComponent<Agent>();
        redAgent = redAllianceRobot15.GetComponent<Agent>();
    }

    private void EnableMogos()
    {
        for (int i = 0; i < blueAllianceMogos.Length; i++)
            blueAllianceMogos[i].SetActive(true);
        for (int i = 0; i < redAllianceMogos.Length; i++)
            redAllianceMogos[i].SetActive(true);
        for (int i = 0; i < neutralMogos.Length; i++)
            neutralMogos[i].SetActive(true);

        if (randMogos) {
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
        if (randMogos) {
            int numToDisable = Random.Range(0, 1);
            for (int i = 0; i < numToDisable; i++)
                neutralMogos[list[i]].SetActive(false);
        }
    }

    private void ResetRingPos()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            setGameObjectPosAndRot(rings[i], ringPositions[i]);
            rings[i].SetActive(true);
            rings[i].GetComponent<Rigidbody>().ResetInertiaTensor();
        }
    }

    private void ResetMogoPos()
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

    private void ResetRobotPos()
    {
        setGameObjectPosAndRot(blueAllianceRobot15, robotPositions[0]);
        setGameObjectPosAndRot(blueAllianceRobot24, robotPositions[1]);
        setGameObjectPosAndRot(redAllianceRobot15, robotPositions[2]);
        setGameObjectPosAndRot(redAllianceRobot24, robotPositions[3]);
    }
}