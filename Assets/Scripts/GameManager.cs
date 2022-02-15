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

    public GameObject[] rings { get; set; }
    private PosAndRot[] ringPositions;

    private const float initTime = 120f - 15f;

    [SerializeField] public float time = initTime;
    private int blueAllianceScore;
    private int redAllianceScore;
    private float timeTogether = 0f;

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
        robotPositions = new PosAndRot[4];
        robotPositions[0] = getGameObjectPosAndRot(blueAllianceRobot15);
        robotPositions[1] = getGameObjectPosAndRot(blueAllianceRobot24);
        robotPositions[2] = getGameObjectPosAndRot(redAllianceRobot15);
        robotPositions[3] = getGameObjectPosAndRot(redAllianceRobot24);

        blueAgent = blueAllianceRobot15.GetComponent<Agent>();
        redAgent = redAllianceRobot15.GetComponent<Agent>();

        // get mogo positions
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

        // get rings
        rings = GameObject.FindGameObjectsWithTag("Ring");

        // get ring positions
        ringPositions = new PosAndRot[rings.Length];
        for (int i = 0; i < rings.Length; i++)
            ringPositions[i] = getGameObjectPosAndRot(rings[i]);

        ResetField();
    }

    void ResetField()
    {
        time = initTime;

        // reset robot positions
        setGameObjectPosAndRot(blueAllianceRobot15, robotPositions[0]);
        setGameObjectPosAndRot(blueAllianceRobot24, robotPositions[1]);
        setGameObjectPosAndRot(redAllianceRobot15, robotPositions[2]);
        setGameObjectPosAndRot(redAllianceRobot24, robotPositions[3]);

        // reset mogo positions
        int offsetLength = 0;
        for (int i = 0; i < blueAllianceMogos.Length; i++)
            setGameObjectPosAndRot(blueAllianceMogos[i], mogoPositions[i]);
        offsetLength += blueAllianceMogos.Length;
        for (int i = 0; i < redAllianceMogos.Length; i++)
            setGameObjectPosAndRot(redAllianceMogos[i], mogoPositions[i + offsetLength]);
        offsetLength += redAllianceMogos.Length;
        for (int i = 0; i < neutralMogos.Length; i++)
            setGameObjectPosAndRot(neutralMogos[i], mogoPositions[i + offsetLength]);

        // reset ring positions
        for (int i = 0; i < rings.Length; i++)
        {
            setGameObjectPosAndRot(rings[i], ringPositions[i]);
            rings[i].SetActive(true);
            rings[i].GetComponent<Rigidbody>().ResetInertiaTensor();
        }

        // reset score
        blueAllianceScore = 0;
        redAllianceScore = 0;
    }

    void Update()
    {
        //Timer Control
        time -= Time.deltaTime;
        float min = Mathf.FloorToInt(time / 60);
        float sec = Mathf.FloorToInt(time % 60);
        timer.text = "Time: " + string.Format("{0:00}:{1:00}", min, sec);

        scoreBlue.text = "Blue Score: " + blueAllianceScore.ToString();
        scoreRed.text = "Red Score: " + redAllianceScore.ToString();

        if (time <= 0)
        {
            blueAgent.EndEpisode();
            redAgent.EndEpisode();

            ResetField();
        }

        //Pinning rule
        if(Vector3.Distance(ringPositions[0].pos, ringPositions[1].pos) < 0.1f) {
            timeTogether += Time.deltaTime;
        } else {
            timeTogether = 0;
        }
        if (timeTogether >= 10f) {
            blueAgent.AddReward(-100f);
            redAgent.AddReward(-100f);
        }
        //End of game rules - cannot be on other teams side to end game
        if(redAgent.transform.localPosition.z > 0.3825585) {
            redAgent.AddReward(-100f);
        }
        if(blueAgent.transform.localPosition.z < 0.3825585) {
            blueAgent.AddReward(-100f);
        }
    }

    public void CollectRing(GameObject robot)
    {
        if (robot == blueAllianceRobot15 || robot == blueAllianceRobot24)
        {
            blueAllianceScore++;
            blueAgent.AddReward(1f);
            redAgent.AddReward(-1f);
            return;
        }
        else if (robot == redAllianceRobot15 || robot == redAllianceRobot24)
        {
            redAllianceScore++;
            redAgent.AddReward(1f);
            blueAgent.AddReward(-1f);
            return;
        }

        throw new System.ArgumentException("Invalid robot");
    }

    public (Vector3, Vector3, Vector3) GetObservationsFromAlliancePerspective(GameObject robot)
    {
        if (robot == blueAllianceRobot15)
        {
            return (
                blueAllianceRobot24.transform.localPosition,
                redAllianceRobot15.transform.localPosition,
                redAllianceRobot24.transform.localPosition
            );
        }
        else if (robot == redAllianceRobot15)
        {
            return (
                redAllianceRobot24.transform.localPosition,
                blueAllianceRobot15.transform.localPosition,
                blueAllianceRobot24.transform.localPosition
            );
        }

        throw new System.ArgumentException("Invalid robot");
    }
}