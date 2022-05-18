using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour, IGameScore {
    public GameObject textScore;
    private int score = 0;

    public void IncreaseScore() {
        score += 100;
        textScore.GetComponent<Text>().text = $"SCORE: {score}";
    }
}
