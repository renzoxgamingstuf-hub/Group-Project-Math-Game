using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class BoxRemover2 : MonoBehaviour
{
public TMP_InputField input;

    void Update()
    {
        if (input == null)
        {
            Debug.LogWarning("Input field not assigned in BoxRemover2!");
            return;
        }

        if (input.text == "(-3, -3) (-2, -2) (-1, -2) (0, 0) (1, 1) (2, -1) (3, 0)")
        {
            GameObject boxObject = GameObject.Find("Box (1)");
            if (boxObject == null)
            {
                Debug.LogWarning("No GameObject named 'Box (1)' found!");
                return;
            }

            Remove removeScript = boxObject.GetComponent<Remove>();
            if (removeScript == null)
            {
                Debug.LogWarning("Remove script not found on Box1 object!");
                return;
            }

            System.GC.Collect();
            removeScript.Poof = true;
        }

    }
}
