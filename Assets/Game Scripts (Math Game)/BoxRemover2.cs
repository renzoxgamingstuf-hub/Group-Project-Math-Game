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
     if (input.text == "(-3, -3) (-2, -2) (-1, -2) (0, 0) (1, 1) (2, -1) (3, 0)")
{
GameObject.FindWithTag("Box1").GetComponent<Remove>();
System.GC.Collect();
GameObject.FindWithTag("Box1").GetComponent<Remove>().Poof = true;
}

    }
}
