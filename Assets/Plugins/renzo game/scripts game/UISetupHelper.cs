using UnityEngine;
using TMPro;

public class UISetupHelper : MonoBehaviour
{
    public TMP_InputField inputField;

    void Start()
    {
        inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
    }
}
