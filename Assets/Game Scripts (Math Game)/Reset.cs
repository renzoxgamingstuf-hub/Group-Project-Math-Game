using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Reset : MonoBehaviour
{
private Rigidbody2D rb;
    private Animator anim;

    private void Start()
    {
rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
       if (collision.gameObject.CompareTag("Bonk"))
{
Die(); 
StartCoroutine(NextLevelAfterWait());
    }
}

private void Die()
{
rb.bodyType = RigidbodyType2D.Static;
}

IEnumerator NextLevelAfterWait()
{
yield return new WaitForSeconds(0.5f);

SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
}