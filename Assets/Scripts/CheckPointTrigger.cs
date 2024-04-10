using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointTrigger : MonoBehaviour
{
    public bool hasBeenVisited = false;
    public float rewardValue = 10;
    public int checkpointID = 0;

    void Start()
    {
    }


    void OnTriggerEnter(Collider other)
    {
        if (!hasBeenVisited )
        {
            hasBeenVisited = true;
        }
    }

    // Reset method to be called at the start of each episode
    public void ResetSegment()
    {
        hasBeenVisited = false;
    }
}