using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Remove : MonoBehaviour
{
public bool Poof = false;

    void Update()
    {
      if (Poof == true)
{
Destroy(this.gameObject);
}
    }
}
