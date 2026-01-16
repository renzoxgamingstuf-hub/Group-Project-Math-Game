using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class FirebaseConfig
{
    public string webApiKey = "";
    public string customTokenEndpoint = "http://localhost:5000/createCustomToken";
}

[System.Serializable]
public class SignInResponse
{
    public string idToken;
    public string localId;
    public string email;
}

public class FirebaseAuthManager : MonoBehaviour
{
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public Button loginButton;
    public TextMeshProUGUI messageText;

    private FirebaseConfig config = new FirebaseConfig();

    void Awake()
    {
        // Load config from Resources/firebase_config.json (do not commit real keys)
        TextAsset cfgText = Resources.Load<TextAsset>("firebase_config");
        if (cfgText != null)
        {
            try { config = JsonUtility.FromJson<FirebaseConfig>(cfgText.text); }
            catch { config = new FirebaseConfig(); }
        }

        // Ensure EventSystem exists (required for UI interactions)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // If any UI fields are missing in the scene, create a minimal UI at runtime
        if (usernameField == null || passwordField == null || loginButton == null || messageText == null)
        {
            CreateDefaultUI();
        }

        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);
    }

    void OnLoginClicked()
    {
        // SWAP: Fields are assigned backwards in the scene
        string username = passwordField.text.Trim();  // This is actually the username field
        string password = usernameField.text.Trim();  // This is actually the password field

        Debug.Log("[FirebaseAuth] Login button clicked");
        Debug.Log($"[FirebaseAuth] Username: '{username}', Password: '{password}'");

        // Validate username
        if (string.IsNullOrEmpty(username))
        {
            messageText.text = "Username cannot be empty.";
            return;
        }

        // Validate password
        if (string.IsNullOrEmpty(password))
        {
            messageText.text = "Password cannot be empty.";
            return;
        }

        StartCoroutine(SignInCoroutine(username, password));
    }

    // Create a minimal UI at runtime if the scene doesn't have UI wired up
    void CreateDefaultUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("LoginCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Title
        CreateLabel(canvasGO.transform, "Title", new Vector2(0, 250), "Login", 40, Color.white);

        // Username label
        CreateLabel(canvasGO.transform, "UsernameLabel", new Vector2(-150, 140), "User", 20, Color.black);

        // Username input field
        usernameField = CreateInputField(canvasGO.transform, "UsernameInput", new Vector2(0, 110), "Enter username...", false);

        // Password label
        CreateLabel(canvasGO.transform, "PasswordLabel", new Vector2(-150, 50), "Password", 20, Color.black);

        // Password input field
        passwordField = CreateInputField(canvasGO.transform, "PasswordInput", new Vector2(0, 20), "Enter password...", true);

        // Login button
        GameObject btnGO = new GameObject("LoginButton");
        btnGO.transform.SetParent(canvasGO.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(200, 50);
        btnRT.anchoredPosition = new Vector2(0, -80);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 1f);
        Button btn = btnGO.AddComponent<Button>();
        
        GameObject btnTextGO = new GameObject("Text");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "Log In";
        btnText.fontSize = 24;
        btnText.color = Color.white;
        RectTransform btRT = btnTextGO.GetComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one; btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;
        loginButton = btn;

        // Message text
        GameObject msgGO = new GameObject("MessageText");
        msgGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI msgText = msgGO.AddComponent<TextMeshProUGUI>();
        msgText.text = "";
        msgText.color = Color.red;
        msgText.fontSize = 20;
        RectTransform msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.sizeDelta = new Vector2(500, 40);
        msgRT.anchoredPosition = new Vector2(0, -180);
        messageText = msgText;



        // Wire button click
        loginButton.onClick.AddListener(OnLoginClicked);
    }

    void CreateLabel(Transform parent, string name, Vector2 anchoredPos, string text, int fontSize, Color color)
    {
        GameObject labelGO = new GameObject(name);
        labelGO.transform.SetParent(parent, false);
        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.sizeDelta = new Vector2(300, 40);
        labelRT.anchoredPosition = anchoredPos;
        
        TextMeshProUGUI labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.text = text;
        labelText.fontSize = fontSize;
        labelText.color = color;
    }

    TMP_InputField CreateInputField(Transform parent, string name, Vector2 anchoredPos, string placeholderText, bool isPassword)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 40);
        rt.anchoredPosition = anchoredPos;
        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);
        TMP_InputField input = go.AddComponent<TMP_InputField>();

        // Text
        GameObject textGO = new GameObject("Text Area/Text");
        textGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.color = Color.black;
        text.fontSize = 18;
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0); textRT.anchorMax = new Vector2(1, 1); textRT.offsetMin = new Vector2(10, 6); textRT.offsetMax = new Vector2(-10, -7);
        input.textComponent = text;

        // Placeholder
        GameObject placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.75f);
        placeholder.text = placeholderText;
        placeholder.fontSize = 18;
        RectTransform phRT = placeholderGO.GetComponent<RectTransform>();
        phRT.anchorMin = textRT.anchorMin; phRT.anchorMax = textRT.anchorMax; phRT.offsetMin = textRT.offsetMin; phRT.offsetMax = textRT.offsetMax;
        input.placeholder = placeholder;

        if (isPassword)
        {
            input.inputType = TMP_InputField.InputType.Password;
            input.contentType = TMP_InputField.ContentType.Password;
        }

        return input;
    }

    IEnumerator SignInCoroutine(string username, string password)
    {
        Debug.Log("[FirebaseAuth] SignInCoroutine started");
        Debug.Log($"[FirebaseAuth] Searching for user: username='{username}', password='{password}'");
        messageText.text = "Signing in...";

        if (!string.IsNullOrEmpty(config.webApiKey))
        {
            string projectId = "project5-arenanova";
            
            // Use runQuery to search for documents where username field matches
            string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents:runQuery?key={config.webApiKey}";
            
            // Query to find documents where username field matches
            string queryJson = $@"{{
  ""structuredQuery"": {{
    ""from"": [{{
      ""collectionId"": ""childaccounts""
    }}],
    ""where"": {{
      ""fieldFilter"": {{
        ""field"": {{
          ""fieldPath"": ""username""
        }},
        ""op"": ""EQUAL"",
        ""value"": {{
          ""stringValue"": ""{username}""
        }}
      }}
    }}
  }}
}}";

            Debug.Log($"[FirebaseAuth] Querying for username: {url}");
            Debug.Log($"[FirebaseAuth] Query JSON:\n{queryJson}");

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(queryJson);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.SetRequestHeader("Content-Type", "application/json");

                Debug.Log("[FirebaseAuth] Sending POST request to query by username");
                yield return req.SendWebRequest();

                Debug.Log($"[FirebaseAuth] HTTP Response Code: {req.responseCode}");
                
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"[FirebaseAuth] Network Error: {req.error}");
                    Debug.LogError($"[FirebaseAuth] Response: {req.downloadHandler.text}");
                    messageText.text = "Error: " + req.error;
                    yield break;
                }

                string resp = req.downloadHandler.text;
                Debug.Log($"[FirebaseAuth] Response length: {resp.Length}");
                Debug.Log($"[FirebaseAuth] ===== FULL RESPONSE START =====");
                Debug.Log(resp);
                Debug.Log($"[FirebaseAuth] ===== FULL RESPONSE END =====");
                
                // Search for matching document in the response and check password
                bool foundMatch = false;
                
                // Check if document was found and password matches
                if (resp.Contains("\"document\"") && resp.Contains($"\"{password}\"") && !resp.Contains("\"error\""))
                {
                    Debug.Log("[FirebaseAuth] Found document with matching username and password");
                    foundMatch = true;
                }
                
                Debug.Log($"[FirebaseAuth] Found matching user: {foundMatch}");
                
                if (foundMatch)
                {
                    Debug.Log("[FirebaseAuth] Login successful!");
                    messageText.text = "it works";
                    PlayerPrefs.SetString("firebase_username", username);
                    yield return new WaitForSeconds(2f);
                    Debug.Log("[FirebaseAuth] Loading scene: Explain scene");
                    SceneManager.LoadScene("Explain scene");
                }
                else
                {
                    Debug.LogWarning("[FirebaseAuth] Login failed: No user found with matching credentials");
                    messageText.text = "Login failed: Username or password incorrect.";
                }
            }
        }
        else
        {
            Debug.LogError("[FirebaseAuth] No Web API key configured!");
            messageText.text = "No Web API key configured.";
        }
    }

    string GetIdToken()
    {
        // Get stored ID token or return empty if not available
        return PlayerPrefs.GetString("firebase_idToken", "");
    }

    string EscapeJsonString(string input)
    {
        // Escape special JSON characters
        return input.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
    }
}
