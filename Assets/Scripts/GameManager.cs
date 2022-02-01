using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Text timer;
    [SerializeField] private Text scorer;
    [SerializeField] private GameObject robot;
    private float time;
    private int score;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Timer Control
        time = robot.GetComponent<RobotAgent>().getTime();
        float min = Mathf.FloorToInt(time / 60);
        float sec = Mathf.FloorToInt(time % 60);
        timer.text = "Time: " + string.Format("{0:00}:{1:00}", min, sec);

        //Score Control
        score = robot.GetComponent<RobotAgent>().getScore();
        scorer.text = "Score: " + score.ToString();
    }
}
