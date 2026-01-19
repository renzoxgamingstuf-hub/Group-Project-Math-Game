using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private float elapsedTime = 0f;
    private TextMeshProUGUI timerText;
    private bool isRunning = true;

    void Start()
    {
        // Create UI Canvas and Timer Text if not already present
        CreateTimerUI();
    }

    void Update()
    {
        if (isRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }

    void CreateTimerUI()
    {
        // Find or create Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TimerCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create Timer Text GameObject
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(canvas.transform, false);
        
        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        timerText.text = "0:00";
        timerText.fontSize = 36;
        timerText.color = Color.white;
        timerText.alignment = TextAlignmentOptions.BottomLeft;

        // Position at bottom left
        RectTransform rectTransform = timerObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.offsetMin = new Vector2(20, 20);
        rectTransform.offsetMax = new Vector2(200, 70);
    }

    void UpdateTimerDisplay()
    {
        int minutes = (int)(elapsedTime / 60f);
        int seconds = (int)(elapsedTime % 60f);
        timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        UpdateTimerDisplay();
    }

    public float GetElapsedTime()
    {
        return elapsedTime;
    }
}
