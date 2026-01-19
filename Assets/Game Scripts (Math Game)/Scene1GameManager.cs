using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using TMPro;
using System.Text.RegularExpressions;

public class Scene1GameManager : MonoBehaviour
{
    private string firebaseWebApiKey = "";
    private string firebaseProjectId = "project5-arenanova";
    private string currentUserDocumentId = "";
    private int currentSessionNumber = -1;
    private Timer timerScript;

    void Start()
    {
        LoadFirebaseConfig();
        StartCoroutine(InitializeGame());
    }

    IEnumerator InitializeGame()
    {
        Debug.Log("=== Scene1GameManager Initializing ===");
        
        // Get Timer script reference
        timerScript = FindObjectOfType<Timer>();
        if (timerScript == null)
        {
            Debug.LogWarning("Timer script not found in scene");
        }

        // Get childid from PlayerPrefs (saved during login)
        currentUserDocumentId = PlayerPrefs.GetString("firebase_childid", "");
        Debug.Log($"ChildID from PlayerPrefs: {currentUserDocumentId}");
        
        if (string.IsNullOrEmpty(currentUserDocumentId))
        {
            Debug.LogError("No childid found! User must be logged in first.");
            yield break;
        }

        // Find highest session number and create new one
        yield return StartCoroutine(FindHighestSessionNumberAndCreate());
        
        Debug.Log($"=== Scene1GameManager Initialization Complete - Session: {currentSessionNumber} ===");
    }

    void LoadFirebaseConfig()
    {
        TextAsset cfgText = Resources.Load<TextAsset>("firebase_config");
        if (cfgText != null)
        {
            try
            {
                FirebaseConfig config = JsonUtility.FromJson<FirebaseConfig>(cfgText.text);
                firebaseWebApiKey = config.webApiKey;
                Debug.Log("Firebase config loaded successfully");
            }
            catch
            {
                Debug.LogError("Failed to parse firebase config");
            }
        }
        else
        {
            Debug.LogError("firebase_config.json not found in Resources");
        }
    }

    IEnumerator FindHighestSessionNumberAndCreate()
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/childaccounts/{currentUserDocumentId}/lizzyprogress?pageSize=1000&key={firebaseWebApiKey}";
        Debug.Log($"Querying lizzyprogress documents at: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            int highestNumber = -1;

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log($"Response from lizzyprogress: {response}");
                
                // Find all document IDs (they are numbers)
                MatchCollection matches = Regex.Matches(response, @"lizzyprogress/(\d+)");
                
                foreach (Match match in matches)
                {
                    if (int.TryParse(match.Groups[1].Value, out int docNumber))
                    {
                        if (docNumber > highestNumber)
                        {
                            highestNumber = docNumber;
                        }
                    }
                }
                
                Debug.Log($"Highest existing session number: {highestNumber}");
            }
            else
            {
                Debug.Log($"No existing lizzyprogress documents (query returned: {request.error})");
            }

            // Next session number is highest + 1
            currentSessionNumber = highestNumber + 1;
            Debug.Log($"Creating new session document: {currentSessionNumber}");
            
            yield return StartCoroutine(CreateSessionDocument());
        }
    }

    IEnumerator CreateSessionDocument()
    {
        if (currentSessionNumber < 0 || string.IsNullOrEmpty(currentUserDocumentId))
        {
            Debug.LogError($"Cannot create session: sessionNum={currentSessionNumber}, userId={currentUserDocumentId}");
            yield break;
        }

        string docPath = $"projects/{firebaseProjectId}/databases/(default)/documents/childaccounts/{currentUserDocumentId}/lizzyprogress/{currentSessionNumber}";
        string url = $"https://firestore.googleapis.com/v1/{docPath}?key={firebaseWebApiKey}";

        // Create document data with initial lprogress = 0
        string jsonData = $@"
        {{
            ""fields"": {{
                ""lprogress"": {{ ""integerValue"": 0 }},
                ""startTime"": {{ ""stringValue"": ""{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}"" }},
                ""timestamp"": {{ ""stringValue"": ""{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}"" }}
            }}
        }}";

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ Session document {currentSessionNumber} created successfully");
            }
            else
            {
                Debug.LogError($"✗ Failed to create session document {currentSessionNumber}: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
    }

    public void OnGameFinished()
    {
        // Stop timer and get elapsed time
        if (timerScript != null)
        {
            timerScript.StopTimer();
            float elapsedTime = timerScript.GetElapsedTime();
            int elapsedSeconds = (int)elapsedTime;
            
            Debug.Log($"Game finished! Elapsed time: {elapsedSeconds} seconds");
            
            // Save lprogress to Firebase for THIS session document
            StartCoroutine(UpdateLprogressInSession(elapsedSeconds));
        }
        else
        {
            Debug.LogWarning("Timer script not found!");
        }
    }

    IEnumerator UpdateLprogressInSession(int timeInSeconds)
    {
        if (currentSessionNumber < 0 || string.IsNullOrEmpty(currentUserDocumentId))
        {
            Debug.LogError($"Cannot update lprogress: sessionNum={currentSessionNumber}, userId={currentUserDocumentId}");
            yield break;
        }

        string docPath = $"projects/{firebaseProjectId}/databases/(default)/documents/childaccounts/{currentUserDocumentId}/lizzyprogress/{currentSessionNumber}";
        string url = $"https://firestore.googleapis.com/v1/{docPath}?key={firebaseWebApiKey}&updateMask.fieldPaths=lprogress";

        string jsonData = $@"
        {{
            ""fields"": {{
                ""lprogress"": {{ ""integerValue"": {timeInSeconds} }}
            }}
        }}";

        Debug.Log($"Updating lprogress for session {currentSessionNumber} to {timeInSeconds} seconds");

        using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ lprogress updated: session {currentSessionNumber} = {timeInSeconds}s");
            }
            else
            {
                Debug.LogError($"✗ Failed to update lprogress: {request.error}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
    }
}
