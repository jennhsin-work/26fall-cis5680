using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreUI : MonoBehaviour
{
    Global globalObj;
    Text scoreText;

    // Use this for initialization
    void Start()
    {
        GameObject g = GameObject.Find("GlobalObject");
        if (g != null)
        {
            globalObj = g.GetComponent<Global>();
        }
        scoreText = gameObject.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (scoreText != null && globalObj != null)
        {
            scoreText.text = globalObj.score.ToString();
        }
    }
}
