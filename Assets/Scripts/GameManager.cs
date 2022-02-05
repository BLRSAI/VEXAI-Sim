using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Text timer;
    [SerializeField] private Text scorer;
    [SerializeField] private Text scorer2;
    [SerializeField] private GameObject robot1;
    [SerializeField] private GameObject robot2;
    [SerializeField] public float time = 120 - 15;
    [SerializeField] private GameObject[] goals;
    
    [SerializeField] public GameObject[] robots;
    private Vector3[] robotPositions;
    public GameObject[] rings;
    private Vector3[] ringPositions;
    private Vector3[] goalPositions;
    private int score;
    void Awake()
    {
        //Collect goal transforms in order to reset them each episode
        goalPositions = new Vector3[goals.Length];
        int i = 0;
        foreach (GameObject g in goals) {
            goalPositions[i] = g.transform.localPosition;
            i++;
        }
        //Collect ring transforms in order to reset them each episode
        int y = 0;
        foreach (GameObject ring in rings) {
            ringPositions[y] = ring.transform.localPosition;
            y++;
        }
        //Collect robot transforms in order to reset them each episode
        robotPositions = new Vector3[robots.Length];
        int x = 0;
        foreach (GameObject r in robots) {
            robotPositions[x] = r.transform.localPosition;
            x++;
        }
    }
    void Update()
    {
        //Timer Control
        time -= Time.deltaTime;
        float min = Mathf.FloorToInt(time / 60);
        float sec = Mathf.FloorToInt(time % 60);
        timer.text = "Time: " + string.Format("{0:00}:{1:00}", min, sec);

        //EndEpisode
        if ((int) time <= 0) {
            robot1.GetComponent<RobotAgent>().SetReward(Math.abs(robot1.GetComponent<RingAgent>().getScore() - robot2.GetComponent<RingAgent>().getScore()));
            robot2.GetComponent<RobotAgent>().SetReward(Math.abs(robot1.GetComponent<RingAgent>().getScore() - robot2.GetComponent<RingAgent>().getScore()));
            robot1.GetComponent<RobotAgent>().EndEpisode();
            robot2.GetComponent<RobotAgent>().EndEpisode();
        }
        //Score Control
        score = robot1.GetComponent<RobotAgent>().getScore();
        scorer.text = "Robot 1 Score: " + score.ToString();
        score = robot2.GetComponent<RobotAgent>().getScore();
        scorer2.text = "Robot 1 Score: " + score.ToString();
    }

    public void Reset() {
        //Set time to 0
        time = 0;
        //Randomize Goals
        int amountOfGoals = Random.Range(0, goals.Length + 1);
        for (int y = 0; y < amountOfGoals; y++)
        {
            goals[y].SetActive(false);
        }
        //Set Goals Back To Starting Position
        int i = 0;
        foreach (GameObject g in goals)
        {
            g.transform.localPosition = goalPositions[i];
            g.gameObject.SetActive(true);
            i++;
        }
        //Reset rings
        int z = 0;
        foreach (GameObject ring in rings) {
            ring.transform.localPosition = ringPositions[z];
            z++;
        }
        //Set Robots back to starting positions
        int x = 0;
        foreach (GameObject r in robots)
        {
            r.transform.localPosition = robotPositions[x];
            x++;
        }
    }
}