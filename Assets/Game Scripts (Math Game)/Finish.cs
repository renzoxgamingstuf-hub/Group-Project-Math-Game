using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Finish : MonoBehaviour
{

private void OnTriggerEnter2D(Collider2D collision)
{
    if (collision.gameObject.name == "Player")
    {
        // Save game progress to Firebase before loading next scene
        Scene1GameManager gameManager = FindObjectOfType<Scene1GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameFinished();
        }
        
        SceneManager.LoadScene("FINISH scene");
    }
}
}
