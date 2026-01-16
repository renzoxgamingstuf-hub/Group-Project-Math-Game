using UnityEngine;

public class ButtonHook : MonoBehaviour
{
    public GameManager gameManager;

    public void Submit()
    {
        gameManager.SubmitAnswer();
    }
}
