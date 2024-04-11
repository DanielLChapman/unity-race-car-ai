using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackPiece : MonoBehaviour
{
    // This could be the object's forward direction or a manually set direction
    public Vector3 forwardDirection = Vector3.forward; // Default to world forward; adjust as needed
    public int id;

    void OnDrawGizmos()
    {
        // Optionally draw the forward direction in the editor for visualization
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, forwardDirection * 5); // Adjust length as needed
    }
}
