using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class BoxRemover : MonoBehaviour
{
public TMP_InputField input;

    void Update()
    {
        if (input == null)
        {
            Debug.LogWarning("Input field not assigned in BoxRemover!");
            return;
        }

        if (input.text == "(-3, 1) (-2, 0) (-1, 1) (0, -1) (1, 0) (2, 2) (3, -2)")
        {
            GameObject boxObject = GameObject.Find("Box");
            if (boxObject == null)
            {
                Debug.LogWarning("No GameObject named 'Box' found!");
                return;
            }

            Remove removeScript = boxObject.GetComponent<Remove>();
            if (removeScript == null)
            {
                Debug.LogWarning("Remove script not found on Box object!");
                return;
            }

            System.GC.Collect();
            removeScript.Poof = true;
        }

    }
}
