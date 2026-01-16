using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;

public class LoginManager : MonoBehaviour
{
    public InputField emailInput;
    public InputField passwordInput;
    public Text statusText;

    FirebaseAuth auth;

    void Start()
    {
        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                statusText.text = "Firebase initialized";
            }
            else
            {
                statusText.text = "Firebase dependency error: " + task.Result;
            }
        });
    }

    public void Register()
    {
        auth.CreateUserWithEmailAndPasswordAsync(
            emailInput.text,
            passwordInput.text
        ).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                statusText.text = "Registration failed";
                return;
            }

            statusText.text = "User registered!";
        });
    }

    public void Login()
    {
        auth.SignInWithEmailAndPasswordAsync(
            emailInput.text,
            passwordInput.text
        ).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                statusText.text = "Login failed";
                return;
            }

            statusText.text = "Login successful!";
            // Load next scene here
        });
    }
}
