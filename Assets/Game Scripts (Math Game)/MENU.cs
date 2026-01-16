using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MENU : MonoBehaviour
{

public void Play ()
{
SceneManager.LoadScene("Scene 1");
}

public void Quit ()
{
Application.Quit();
}

public void Back ()
{
SceneManager.LoadScene("Menu");
}

public void PlayOther ()
{
SceneManager.LoadScene("Explain scene");
}


}
