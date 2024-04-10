using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointTriggers : MonoBehaviour
{
    public int numberOfSegments;
    private bool[] segmentVisited;

    void Start()
    {
        InitializeSegments();
    }

    void InitializeSegments()
    {
        segmentVisited = new bool[numberOfSegments];
    }

    public void VisitSegment(int segmentIndex)
    {
        if (!segmentVisited[segmentIndex])
        {
            segmentVisited[segmentIndex] = true;
            // Reward the agent
        }
    }

    // Reset method to be called at the start of each episode
    public void ResetSegments()
    {
        InitializeSegments();
    }
}