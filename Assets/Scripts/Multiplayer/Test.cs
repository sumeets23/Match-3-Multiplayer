using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public ShapesManager shapesManager;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            shapesManager.IncreaseScore(10);
        }
    }
}
