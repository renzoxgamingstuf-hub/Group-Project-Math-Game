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
     if (input.text == "(-3, 1) (-2, 0) (-1, 1) (0, -1) (1, 0) (2, 2) (3, -2)")
{
GameObject.FindWithTag("Box").GetComponent<Remove>();
System.GC.Collect();
GameObject.FindWithTag("Box").GetComponent<Remove>().Poof = true;
}

    }
}
