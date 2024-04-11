using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CarAgent : Agent
{
    [SerializeField]
    Rigidbody rb;
    CarPhysics carPhysics;

    private Vector3 startingPos;

    LayerMask trackLayer;
    LayerMask groundLayer;

    public int id = 0;

    private float startTime;
    private float elapsedTime;
    private bool finishLineCrossed = false;

    public float rewardOnTrack = 0.5f; // Reward for each tire on the track
    public float penaltyOffTrack = -20f; // Penalty for each tire off the track

    private float lastX;
    private float lastZ;

    private float currentTrackID = -1;

    [SerializeField]
    public GameObject triggerPoints;

    Dictionary<int, HashSet<int>> agentCheckpointsReached = new Dictionary<int, HashSet<int>>();

    Vector3 trackForward = Vector3.forward;

    void RegisterCheckpoint(int agentId, int checkpointId)
    {
        if (!agentCheckpointsReached.ContainsKey(agentId))
        {
            agentCheckpointsReached[agentId] = new HashSet<int>();
        }

        // Check if the checkpoint was already reached
        if (!agentCheckpointsReached[agentId].Contains(checkpointId))
        {
            agentCheckpointsReached[agentId].Add(checkpointId);
            AddReward(100f * checkpointId);
            if (checkpointId == 0) {
                AddReward(100f);
            }
        } 
        else 
        {
            AddReward(-50f);
        }
    }

    public void ResetAgentCheckpoints(int agentId)
    {
        if (agentCheckpointsReached.ContainsKey(agentId))
        {
            // Clears the list of reached checkpoints for this specific agent
            agentCheckpointsReached[agentId].Clear();
        }
    }



    bool IsTireOnTrack(Transform tire)
    {
        RaycastHit hit;
        float checkDistance = 100.0f; // How far below the tire we check for the track
        Vector3 startOffset = Vector3.up * 40f; // Small offset to start the ray above the collider

        if (Physics.Raycast(tire.position + startOffset, -Vector3.up, out hit, checkDistance))
        {
            if (hit.collider.CompareTag("TrackPiece"))
            {
                return true;
            }
        }
        return false;
    }


    private void Start()
    {
        //triggersParent = GameObject.Find("Triggers");
         carPhysics = GetComponent<CarPhysics>();
        startingPos = transform.position;
        trackLayer = LayerMask.GetMask("Track");
        groundLayer = LayerMask.GetMask("Ground");
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.z);
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);
        sensor.AddObservation(carPhysics.currentSpeed);
        sensor.AddObservation(carPhysics.currentAcceleration);
        sensor.AddObservation(carPhysics.currentSteering);
        sensor.AddObservation(IsTireOnTrack(carPhysics.wheelFrontLeft) ? 1f : 0f);
        sensor.AddObservation(IsTireOnTrack(carPhysics.wheelFrontRight) ? 1f : 0f);
        sensor.AddObservation(IsTireOnTrack(carPhysics.wheelRearLeft) ? 1f : 0f);
        sensor.AddObservation(IsTireOnTrack(carPhysics.wheelRearRight) ? 1f : 0f);
        sensor.AddObservation(rb.angularVelocity);
        sensor.AddObservation(rb.angularVelocity.magnitude);
        sensor.AddObservation(elapsedTime);
        sensor.AddObservation(currentTrackID);
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        lastX = transform.localPosition.x;
        lastZ = transform.localPosition.z;
        var action = actions.ContinuousActions;
        float steeringAction = action[0]; // Continuous action for steering
        float accelerationAction = action[1]; // Continuous action for acceleration
        float timeToFullSteer = carPhysics.timeToFullSteer;
        float accelerationIncrement = carPhysics.accelerationIncrement;

        if (steeringAction > 0) {
            carPhysics.currentSteering += Time.deltaTime / timeToFullSteer; // Directly setting, assuming it fits the expected range (-1 to 1)
        } else if (steeringAction < 0 ) {
            carPhysics.currentSteering -= Time.deltaTime / timeToFullSteer;
        } else {
            carPhysics.currentSteering = Mathf.MoveTowards(carPhysics.currentSteering, 0, Time.deltaTime / timeToFullSteer);
        }
        // Clamp currentSteering to be within -1 and 1
         carPhysics.currentSteering = Mathf.Clamp( carPhysics.currentSteering, -1f, 1f);


        if (accelerationAction > 0) {
            carPhysics.currentAcceleration += accelerationIncrement;
        } else if (accelerationAction < 0) {
            carPhysics.currentAcceleration -= accelerationIncrement;
        } else {
            if (carPhysics.currentAcceleration > 0)
            {
                carPhysics.currentAcceleration -= Mathf.Min(accelerationIncrement,carPhysics.currentAcceleration / 10);
            }
            else if (carPhysics.currentAcceleration < 0)
            {
                carPhysics.currentAcceleration += Mathf.Min(accelerationIncrement, -1 * carPhysics.currentAcceleration / 10);
            }
        }

        carPhysics.currentAcceleration = Mathf.Clamp(carPhysics.currentAcceleration, carPhysics.minAcceleration, carPhysics.maxAcceleration);

        
        
        

        float totalReward = 0f;
        bool bool1 = IsTireOnTrack(carPhysics.wheelFrontLeft);
        bool bool2 = IsTireOnTrack(carPhysics.wheelFrontRight);
        bool bool3 = IsTireOnTrack(carPhysics.wheelRearLeft);
        bool bool4 = IsTireOnTrack(carPhysics.wheelRearRight);
        totalReward += bool1 ? 0.1f : -1f;
        totalReward += bool2 ? 0.1f : -1f;
        totalReward += bool3 ? 0.1f : -1f;
        totalReward += bool4 ? 0.1f : -1f;
        
        
        AddReward(totalReward);
    
    }
    
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = 0; // Steering neutral
        continuousActionsOut[1] = 0; // Acceleration neutral

        // Steering
        if (Input.GetKey(KeyCode.RightArrow))
        {
            continuousActionsOut[0] = 1; // Steer right
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            continuousActionsOut[0] = -1; // Steer left
        } else {
            continuousActionsOut[0] = 0;
        }

        // Acceleration and braking
        if (Input.GetKey(KeyCode.UpArrow))
        {
            continuousActionsOut[1] = 1; // Accelerate
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            continuousActionsOut[1] = -1; // Brake/Decelerate
        }
    }

    [SerializeField]
    private float maxRotationAngle = 15f; // Adjust based on when you consider the car flipped

    private void CheckForFlipping()
    {
        Vector3 eulerRotation = transform.eulerAngles;
        eulerRotation.x = (eulerRotation.x > 180) ? eulerRotation.x - 360 : eulerRotation.x;
        eulerRotation.z = (eulerRotation.z > 180) ? eulerRotation.z - 360 : eulerRotation.z;

        if (Mathf.Abs(eulerRotation.x) > maxRotationAngle || Mathf.Abs(eulerRotation.z) > maxRotationAngle)
        {
            AddReward(-1f); // Punish for flipping
            EndEpisode();
        }

    
    }

    
    private void Update()
    {
        if (!finishLineCrossed)
        {
            elapsedTime = Time.time - startTime;
        }
        if (transform.position.y < -10f)
        {
            AddReward(-100f); // Punish for falling off the map
            EndEpisode();
        }

        Vector3 currentForwardDirection = trackForward; // Implement this based on your track
        float forwardVelocity = Vector3.Dot(rb.velocity, currentForwardDirection.normalized);

        Debug.Log(forwardVelocity);

        // Ensure that the reward is only given for forward movement
        if (forwardVelocity > 0) {
            AddReward(forwardVelocity * 1f); // Adjust 'forwardVelocityRewardFactor' as needed
        } else if (forwardVelocity < -5f) {
            AddReward(-5f);
        }
        
        RaycastHit hit;
        if (Physics.Raycast(rb.position + Vector3.up * 50f, -Vector3.up, out hit, 100f))
        {
            TrackPiece hitTrackPiece = hit.collider.GetComponent<TrackPiece>();
            if (hitTrackPiece != null)
            {
               currentTrackID = hitTrackPiece.id;
            }
        }
        AddReward(currentTrackID / 10f);
        CheckForFlipping();
    }
    public override void OnEpisodeBegin()
    {
        // Use Rigidbody.position instead of transform.localPosition for setting position
        rb.position = startingPos; // Ensure it's above the terrain
        rb.transform.rotation = Quaternion.Euler(0, 0, 0); // Resets the rotation to face 'north' with no tilt
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero; // Reset angular velocity too
        // Ensure carPhysics variables are reset appropriately
        carPhysics.currentSpeed = 0f;
        carPhysics.currentAcceleration = 0f;
        // Reset time and flags
        startTime = Time.time;
        elapsedTime = 0f;
        finishLineCrossed = false;
        trackForward = Vector3.forward;

        //ResetAgentCheckpoints(id);
        foreach (Transform checkpoint in triggerPoints.transform)
        {
            checkpoint.gameObject.SetActive(true); // Reactivates the checkpoint
            // If there are any additional components or settings you need to reset,
            // you can do that here. For example:
            // checkpoint.GetComponent<Checkpoint>().ResetCheckpoint();
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Wall")
        {
            Debug.Log("Wall collided, reducing score");
            AddReward(-175f);
            EndEpisode();
        } 
        if (other.CompareTag("Finish"))
        {
            finishLineCrossed = true;
            float reward = 100f;//1f / elapsedTime; // Assuming elapsedTime is the time taken to reach the finish line
            AddReward(reward);
            EndEpisode();
        }
        if (other.CompareTag("Checkpoint")) 
        {
           /*CheckPointTrigger checkpointScript = other.gameObject.GetComponent<CheckPointTrigger>();
           
           if (checkpointScript != null)
            {
                int checkpointID = checkpointScript.checkpointID;


                // Here you can handle the checkpoint logic, such as checking if it's the next expected checkpoint
                // And rewarding the agent accordingly
                RegisterCheckpoint(id, checkpointID);
            }*/
            
            CheckPointTrigger checkpointScript = other.gameObject.GetComponent<CheckPointTrigger>();
             if (checkpointScript != null)
            {
                int checkpointID = checkpointScript.checkpointID;


                // Here you can handle the checkpoint logic, such as checking if it's the next expected checkpoint
                // And rewarding the agent accordingly
                AddReward(10f * checkpointID + 100f);
            }

            other.gameObject.SetActive(false); // Disable the checkpoint
    
        }
        if (other.gameObject.CompareTag("TrackPiece")) // Ensure your track pieces have a specific tag
        {
            TrackPiece piece = other.GetComponent<TrackPiece>();
            if (piece != null)
            {
                // Now you have access to the piece's forward direction
                trackForward = piece.forwardDirection;

                // Use trackForward for navigation, AI steering, etc.
            }
        }
    }
}
