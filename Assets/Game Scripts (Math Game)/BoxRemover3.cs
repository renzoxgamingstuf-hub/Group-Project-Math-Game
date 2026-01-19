using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class BoxRemover3 : MonoBehaviour
{
public TMP_InputField input;

    void Update()
    {
        if (input == null)
        {
            Debug.LogWarning("Input field not assigned in BoxRemover3!");
            return;
        }

        if (input.text == "(-3, 3) (-2, -2) (0, -1) (1, -3) (2, 2) (3, 0)")
        {
            GameObject boxObject = GameObject.Find("Box (2)");
            if (boxObject == null)
            {
                Debug.LogWarning("No GameObject named 'Box (2)' found!");
                return;
            }

            Remove removeScript = boxObject.GetComponent<Remove>();
            if (removeScript == null)
            {
                Debug.LogWarning("Remove script not found on Box2 object!");
                return;
            }

            System.GC.Collect();
            removeScript.Poof = true;
        }

    }
}
