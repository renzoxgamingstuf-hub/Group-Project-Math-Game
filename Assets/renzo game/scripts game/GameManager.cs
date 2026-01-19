using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
    public List<int> sequence = new List<int>();     // correct order
    public List<int> playerInput = new List<int>();  // what player clicks
    public List<char> operations = new List<char>();  // operators between sequence numbers
    public int sequenceLength = 3;                   // increases over time
    public bool isShowingSequence;
    public bool isWaitingForMathAnswer = false;      // waiting for math answer
    public NumberTile[] allTiles; // assign in inspector
    public TMP_Text feedbackText;
    public TMP_Text mathProblemText;
    public TMP_Text levelText;                       // NEW: display level
    public TMP_InputField answerInput;
    public int level = 1;
    private string firebaseWebApiKey = "";
    private string firebaseProjectId = "project5-arenanova";
    private string currentUserDocumentId = "";
    private int currentSessionNumber = -1;
    private int startingLevel = 1;

    void Start()
    {
        LoadFirebaseConfig();
        StartCoroutine(InitializeGame());
    }

    IEnumerator InitializeGame()
    {
        // Load level from Firebase first
        yield return StartCoroutine(LoadLevelFromFirebase());
        // Create a new session document in rprogress collection
        yield return StartCoroutine(CreateNewSessionDocument());
        // Then start the game
        StartNextRound();
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
            }
            catch
            {
                Debug.LogWarning("Failed to load Firebase config");
            }
        }
    }

    void GenerateSequence()
    {
        do
        {
            sequence.Clear();

            for (int i = 0; i < sequenceLength; i++)
            {
                int randomNumber = Random.Range(1, 10); // 1–9
                sequence.Add(randomNumber);
            }

            // Generate operators for the expression between numbers
            operations.Clear();
            if (sequence.Count > 1)
            {
                float temp = (float)sequence[0];
                for (int i = 1; i < sequence.Count; i++)
                {
                    float num = (float)sequence[i];
                    char op;

                    // Try to pick division only when divisible, otherwise pick +, *, or -
                    int choice = Random.Range(0, 4); // 0: +, 1: *, 2: /, 3: -
                    if (choice == 2 && num != 0 && temp % num == 0)
                    {
                        op = '/';
                    }
                    else if (choice == 1)
                    {
                        op = '*';
                    }
                    else if (choice == 3)
                    {
                        op = '-';
                    }
                    else
                    {
                        op = '+';
                    }

                    operations.Add(op);

                    // Apply op to temp so future division checks use updated value (left-to-right)
                    if (op == '+') temp += num;
                    else if (op == '*') temp *= num;
                    else if (op == '-') temp -= num;
                    else if (op == '/') temp /= num;
                }
            }
        } while (CalculateCorrectAnswer() < 0);
    }

    IEnumerator PlaySequence()
    {
        isShowingSequence = true;
        playerInput.Clear();
        isWaitingForMathAnswer = false;

        yield return new WaitForSeconds(1f);

        foreach (int num in sequence)
        {
            NumberTile tile = FindTile(num);
            if (tile != null)
            {
                tile.Highlight();
                yield return new WaitForSeconds(0.7f);
            }
        }

        isShowingSequence = false;
        ShowFeedback("Click the tiles in the order you remember!");
    }

    NumberTile FindTile(int number)
    {
        return System.Array.Find(allTiles, t => t.number == number);
    }

    public void OnTileClicked(int number)
    {
        if (isShowingSequence) return;
        if (isWaitingForMathAnswer) return;  // Don't click tiles while doing math

        // Highlight the clicked tile
        NumberTile clickedTile = FindTile(number);
        if (clickedTile != null)
        {
            clickedTile.Highlight();
        }

        playerInput.Add(number);

        // Check if player clicked wrong tile
        if (playerInput.Count > sequence.Count || playerInput[playerInput.Count - 1] != sequence[playerInput.Count - 1])
        {
            ShowFeedback("Wrong! Level down!");
            Debug.Log($"Wrong tile clicked! Current level before: {level}");
            // Decrease level by 1, but don't go below 1
            if (level > 1)
            {
                level--;
                sequenceLength = 2 + level;
                Debug.Log($"Level decreased to: {level}");
            }
            else
            {
                Debug.Log($"Level is already at minimum (1)");
            }
            UpdateLevelInFirebase();
            StartCoroutine(DelayedNextRound());
            return;
        }

        // Check if sequence is complete and correct
        if (playerInput.Count == sequence.Count)
        {
            ShowMathProblem();
        }
    }

    float CalculateCorrectAnswer()
    {
        if (sequence == null || sequence.Count == 0) return 0f;
        if (sequence.Count == 1) return (float)sequence[0];

        // Build list of values and operators for order of operations
        List<float> values = new List<float>();
        foreach (int num in sequence) values.Add((float)num);
        List<char> ops = new List<char>(operations);

        // First pass: handle * and / (left to right)
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i] == '*' || ops[i] == '/')
            {
                float result;
                if (ops[i] == '*')
                {
                    result = values[i] * values[i + 1];
                }
                else // division
                {
                    if (values[i + 1] != 0)
                        result = values[i] / values[i + 1];
                    else
                        result = 0;
                }
                // Replace values[i] with result and remove values[i+1] and ops[i]
                values[i] = result;
                values.RemoveAt(i + 1);
                ops.RemoveAt(i);
                i--; // Back up to check the next operator after replacement
            }
        }

        // Second pass: handle + and - (left to right)
        float finalResult = values[0];
        for (int i = 0; i < ops.Count; i++)
        {
            if (ops[i] == '+')
                finalResult += values[i + 1];
            else if (ops[i] == '-')
                finalResult -= values[i + 1];
        }

        return finalResult;
    }

    void ShowMathProblem()
    {
        isWaitingForMathAnswer = true;
        // Build problem string including operators
        string problem = "";
        if (sequence.Count > 0)
        {
            problem += sequence[0].ToString();
            for (int i = 1; i < sequence.Count; i++)
            {
                char op = (i - 1) < operations.Count ? operations[i - 1] : '+';
                problem += " " + op + " " + sequence[i].ToString();
            }
            problem += " = ?";
        }
        if (mathProblemText != null)
        {
            mathProblemText.text = problem;
        }
        ShowFeedback("Now solve the math problem!");
    }

    public void SubmitAnswer()
    {
        if (!isWaitingForMathAnswer)
        {
            ShowFeedback("First, click the tiles in order!");
            return;
        }

        float playerAnswer;
        if (!float.TryParse(answerInput.text, out playerAnswer))
        {
            ShowFeedback("Please enter a number.");
            return;
        }

        float correctAnswer = CalculateCorrectAnswer();

        if (Mathf.Approximately(playerAnswer, correctAnswer))
        {
            ShowFeedback("Correct! Level up!");
            sequenceLength++;
            level++;
            UpdateLevelInFirebase();
            StartNextRound();
        }
        else
        {
            ShowFeedback("Math answer is wrong. Level down!");
            Debug.Log($"Wrong answer! Current level before: {level}");
            // Decrease level by 1, but don't go below 1
            if (level > 1)
            {
                level--;
                sequenceLength = 2 + level;
                Debug.Log($"Level decreased to: {level}");
            }
            else
            {
                Debug.Log($"Level is already at minimum (1)");
            }
            UpdateLevelInFirebase();
            StartCoroutine(DelayedNextRound());
        }
    }

    void ResetGame()
    {
        // Deprecated - level no longer resets to 1, it decreases by 1 instead
        // This method is kept for compatibility but no longer called
    }

    IEnumerator DelayedNextRound()
    {
        yield return new WaitForSeconds(2f);
        StartNextRound();
    }

    void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
        else
        {
            Debug.LogWarning("Feedback Text is not assigned!");
        }
    }

    void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            levelText.text = "Level: " + level;
        }
    }

    void StartNextRound()
    {
        UpdateLevelDisplay();
        GenerateSequence();
        StartCoroutine(PlaySequence());
        if (answerInput != null) answerInput.text = "";
        if (mathProblemText != null) mathProblemText.text = "";
    }

    void UpdateLevelInFirebase()
    {
        // Only update if we have a valid session
        if (string.IsNullOrEmpty(currentUserDocumentId) || currentSessionNumber < 0 || string.IsNullOrEmpty(firebaseWebApiKey))
        {
            Debug.LogWarning("Cannot update Firebase: session info not available");
            return;
        }

        StartCoroutine(UpdateRprogressInSession());
        StartCoroutine(UpdateAccountRenzolvl());
    }

    IEnumerator UpdateRprogressInSession()
    {
        // Update rprogress in the current session document
        // rprogress = current level - starting level
        int rprogressValue = level - startingLevel;
        string updateUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/childaccounts/{currentUserDocumentId}/renzoprogress/{currentSessionNumber}?key={firebaseWebApiKey}&updateMask.fieldPaths=rprogress";
        
        string updateJson = $@"{{
  ""fields"": {{
    ""rprogress"": {{
      ""integerValue"": {rprogressValue}
    }}
  }}
}}";

        using (UnityWebRequest req = new UnityWebRequest(updateUrl, "PATCH"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(updateJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Successfully updated rprogress to {rprogressValue} in session {currentSessionNumber}");
            }
            else
            {
                Debug.LogError($"Failed to update Firebase: {req.error}");
                Debug.LogError($"Response: {req.downloadHandler.text}");
            }
        }
    }

    IEnumerator UpdateAccountRenzolvl()
    {
        // Update renzolvl in the account document (childaccounts)
        string updateUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/childaccounts/{currentUserDocumentId}?key={firebaseWebApiKey}&updateMask.fieldPaths=renzolvl";
        
        string updateJson = $@"{{
  ""fields"": {{
    ""renzolvl"": {{
      ""integerValue"": {level}
    }}
  }}
}}";

        using (UnityWebRequest req = new UnityWebRequest(updateUrl, "PATCH"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(updateJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Successfully updated account renzolvl to {level}");
            }
            else
            {
                Debug.LogError($"Failed to update account renzolvl: {req.error}");
                Debug.LogError($"Response: {req.downloadHandler.text}");
            }
        }
    }

    IEnumerator LoadLevelFromFirebase()
    {
        string username = PlayerPrefs.GetString("firebase_username", "");
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(firebaseWebApiKey))
        {
            Debug.LogWarning("Cannot load level from Firebase: username or API key not available");
            yield break;
        }

        // Query Firebase to find the user's document and get renzolvl
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:runQuery?key={firebaseWebApiKey}";
        
        string queryJson = $@"{{""structuredQuery"": {{""from"": [{{""collectionId"": ""childaccounts""}}], ""where"": {{""fieldFilter"": {{""field"": {{""fieldPath"": ""username""}}, ""op"": ""EQUAL"", ""value"": {{""stringValue"": ""{username}""}}}}}}}}}}";

        using (UnityWebRequest req = new UnityWebRequest(queryUrl, "POST"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(queryJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Firebase query error: {req.error}");
                yield break;
            }

            string response = req.downloadHandler.text;
            
            // Debug: log the full response
            Debug.Log($"=== Firebase Response Start ===");
            Debug.Log(response);
            Debug.Log($"=== Firebase Response End ===");
            
            // Extract renzolvl from response
            int loadedLevel = ExtractRenzolvlFromResponse(response);
            if (loadedLevel >= 0)
            {
                level = loadedLevel;
                sequenceLength = 2 + level; // Adjust sequence length based on level
                Debug.Log($"Loaded level {level} from Firebase");
            }
            else
            {
                Debug.LogWarning("Could not find renzolvl in Firebase response");
            }
        }
    }

    int ExtractRenzolvlFromResponse(string response)
    {
        try
        {
            // Try to find and parse the renzolvl value more robustly
            // Look for pattern: "renzolvl":{"integerValue":N}
            
            int renzolvlPos = response.IndexOf("\"renzolvl\"");
            if (renzolvlPos == -1)
            {
                Debug.LogWarning("renzolvl not found in response");
                return -1;
            }

            // Look for the next integerValue
            int searchPos = renzolvlPos + 10; // length of "renzolvl"
            int intValPos = response.IndexOf("\"integerValue\"", searchPos);
            
            if (intValPos == -1)
            {
                Debug.LogWarning("integerValue not found after renzolvl");
                return -1;
            }

            // Find the number after the colon
            int colonPos = response.IndexOf(":", intValPos);
            if (colonPos == -1) return -1;

            // Parse the number
            int startIdx = colonPos + 1;
            while (startIdx < response.Length && !char.IsDigit(response[startIdx]) && response[startIdx] != '-')
            {
                startIdx++;
            }

            int endIdx = startIdx;
            if (endIdx < response.Length && response[endIdx] == '-') endIdx++;
            
            while (endIdx < response.Length && char.IsDigit(response[endIdx]))
            {
                endIdx++;
            }

            if (endIdx > startIdx)
            {
                string numStr = response.Substring(startIdx, endIdx - startIdx);
                if (int.TryParse(numStr, out int result))
                {
                    Debug.Log($"Successfully extracted renzolvl: {result}");
                    return result;
                }
            }

            Debug.LogWarning("Could not extract number from renzolvl");
            return -1;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error extracting renzolvl: {e.Message}");
            return -1;
        }
    }

    IEnumerator UpdateLevelCoroutine(string username)
    {
        // This method is deprecated - rprogress is now updated directly in the session document
        yield break;
    }

    string ExtractDocumentIdFromResponse(string response)
    {
        // Parse the document path from the response
        // Expected format: "name": "projects/project5-arenanova/databases/(default)/documents/childaccounts/DOCUMENT_ID"
        int startIndex = response.IndexOf("childaccounts/");
        if (startIndex == -1) return null;

        startIndex += "childaccounts/".Length;
        int endIndex = response.IndexOf("\"", startIndex);
        if (endIndex == -1) return null;

        return response.Substring(startIndex, endIndex - startIndex);
    }

    IEnumerator UpdateRenzolvlField(string documentId)
    {
        // This method is deprecated - rprogress is now updated directly in the session document
        yield break;
    }

    IEnumerator CreateNewSessionDocument()
    {
        string username = PlayerPrefs.GetString("firebase_username", "");
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(firebaseWebApiKey))
        {
            Debug.LogWarning("Cannot create session document: username or API key not available");
            yield break;
        }

        // First, find the user's document ID in childaccounts collection
        yield return StartCoroutine(FindUserAndCreateSession(username));
    }

    IEnumerator FindUserAndCreateSession(string username)
    {
        // Query to find the user's document in childaccounts
        string queryUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents:runQuery?key={firebaseWebApiKey}";
        
        string queryJson = $@"{{""structuredQuery"": {{""from"": [{{""collectionId"": ""childaccounts""}}], ""where"": {{""fieldFilter"": {{""field"": {{""fieldPath"": ""username""}}, ""op"": ""EQUAL"", ""value"": {{""stringValue"": ""{username}""}}}}}}}}}}";

        using (UnityWebRequest req = new UnityWebRequest(queryUrl, "POST"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(queryJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Firebase query error: {req.error}");
                yield break;
            }

            string response = req.downloadHandler.text;
            
            // Extract user's document ID from response
            string userDocumentId = ExtractDocumentIdFromResponse(response);
            if (string.IsNullOrEmpty(userDocumentId))
            {
                Debug.LogWarning("Could not find user document ID in response");
                yield break;
            }

            // Now find the highest session number in the rprogress subcollection
            yield return StartCoroutine(FindHighestSessionNumberAndCreate(userDocumentId));
        }
    }

    IEnumerator FindHighestSessionNumberAndCreate(string userDocumentId)
    {
        // List all documents in the renzoprogress subcollection for this user
        string listUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/childaccounts/{userDocumentId}/renzoprogress?key={firebaseWebApiKey}";

        using (UnityWebRequest req = new UnityWebRequest(listUrl, "GET"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Firebase query error: {req.error}");
                Debug.LogError($"Response: {req.downloadHandler.text}");
                yield break;
            }

            string response = req.downloadHandler.text;
            
            // Find the highest document number
            int highestNum = FindHighestSessionNumber(response);
            int nextSessionNumber = highestNum >= 0 ? highestNum + 1 : 1; // Start from 1, not 0
            
            // Create new document with that number
            yield return StartCoroutine(CreateSessionDocumentWithNumber(userDocumentId, nextSessionNumber));
        }
    }

    int FindHighestSessionNumber(string response)
    {
        int highestNumber = -1;
        
        // Look for document names in the response
        // Format: "documents/childaccounts/{id}/renzoprogress/0", etc.
        int searchIndex = 0;
        string searchPattern = "/renzoprogress/";
        
        while ((searchIndex = response.IndexOf(searchPattern, searchIndex)) != -1)
        {
            searchIndex += searchPattern.Length;
            
            // Extract the document number
            int numberEndIndex = response.IndexOf("\"", searchIndex);
            if (numberEndIndex == -1) break;
            
            string numberStr = response.Substring(searchIndex, numberEndIndex - searchIndex);
            
            // Try to parse the number (it might contain additional path info, so be careful)
            int docNumber = -1;
            if (int.TryParse(numberStr.Split(new char[] { '/' })[0], out docNumber))
            {
                if (docNumber > highestNumber)
                {
                    highestNumber = docNumber;
                }
            }
        }
        
        return highestNumber;
    }

    IEnumerator CreateSessionDocumentWithNumber(string userDocumentId, int sessionNumber)
    {
        // Store the current session info
        currentUserDocumentId = userDocumentId;
        currentSessionNumber = sessionNumber;
        startingLevel = level; // Store the starting level

        // Create new document in renzoprogress subcollection with rprogress set to 0 (current level - starting level)
        string createUrl = $"https://firestore.googleapis.com/v1/projects/{firebaseProjectId}/databases/(default)/documents/childaccounts/{userDocumentId}/renzoprogress?key={firebaseWebApiKey}&documentId={sessionNumber}";
        
        int rprogressValue = level - startingLevel; // Calculate progress
        string createJson = $@"{{
  ""fields"": {{
    ""rprogress"": {{
      ""integerValue"": {rprogressValue}
    }},
    ""startingLevel"": {{
      ""integerValue"": {startingLevel}
    }},
    ""timestamp"": {{
      ""stringValue"": ""{System.DateTime.UtcNow:O}""
    }}
  }}
}}";

        using (UnityWebRequest req = new UnityWebRequest(createUrl, "POST"))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(createJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Successfully created session document {sessionNumber} in renzoprogress collection");
            }
            else
            {
                Debug.LogError($"Failed to create session document: {req.error}");
                Debug.LogError($"Response: {req.downloadHandler.text}");
            }
        }
    }
}