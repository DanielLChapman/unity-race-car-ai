using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarPhysics : MonoBehaviour
{
    [SerializeField]
    public Transform wheelFrontLeft; // Drag the corresponding empty GameObject here in the Inspector
    public Transform wheelFrontRight;
    public Transform wheelRearLeft;
    public Transform wheelRearRight;
    public float springStrength = 10f;
    public float dampingStrength = 10f;
    [SerializeField]
    Rigidbody carRigidBody;

    public float suspensionRestDist = 0.83f;

    //direction
    public float frontTireGrip = 1f;
    public float rearTireGrip = 1f;
    public bool frontWheelDrive = true;

    public float currentSteering = 0f; // Current steering input
    public float timeToFullSteer = 1f; // Time in seconds to reach full steering input

    public float topSpeed = 100f; // Define the top speed of your car
    public float currentSpeed;

    public float maxAcceleration = 6f;
    public float accelerationIncrement = 0.005f;
    public float minAcceleration = -6f;
    public float currentAcceleration = 0;
    
    public float brakeStrength = 2f; 


    // Start is called before the first frame update
    void Start()
    {
    }

    void Update()
    {
        // Check for steering input
        if (Input.GetKey(KeyCode.RightArrow))
        {
            // Increase currentSteering towards 1 over timeToFullSteer
            currentSteering += Time.deltaTime / timeToFullSteer;
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            // Decrease currentSteering towards -1 over timeToFullSteer
            currentSteering -= Time.deltaTime / timeToFullSteer;
        }
        else
        {
            // Optionally, automatically return to neutral steering when no keys are pressed
            currentSteering = Mathf.MoveTowards(currentSteering, 0, Time.deltaTime / timeToFullSteer);
        }

        // Clamp currentSteering to be within -1 and 1
        currentSteering = Mathf.Clamp(currentSteering, -1f, 1f);

        //acceleration
        if (Input.GetKey(KeyCode.UpArrow)) // Accelerate
        {
            currentAcceleration += accelerationIncrement;
        }
        else if (Input.GetKey(KeyCode.DownArrow)) // Decelerate/Brake
        {
            currentAcceleration -= accelerationIncrement;
        }
        else
        {
            // Optionally, smoothly return to 0 when no keys are pressed
            if (currentAcceleration > 0)
            {
                currentAcceleration -= Mathf.Min(accelerationIncrement, currentAcceleration / 10);
            }
            else if (currentAcceleration < 0)
            {
                currentAcceleration += Mathf.Min(accelerationIncrement, -currentAcceleration / 10);
            }
        }


        currentAcceleration = Mathf.Clamp(currentAcceleration, minAcceleration, maxAcceleration);

    }


    void FixedUpdate()
    {
        
        ApplySuspensionForce(wheelFrontLeft);
        ApplySuspensionForce(wheelFrontRight);
        ApplySuspensionForce(wheelRearLeft);
        ApplySuspensionForce(wheelRearRight);

         

        ApplyThrottle(currentAcceleration, wheelRearLeft);
        ApplyThrottle(currentAcceleration, wheelRearRight);

        // Assuming Steer is called in FixedUpdate to stay in sync with physics updates
        Steer(wheelFrontLeft, currentSteering, frontTireGrip);
        Steer(wheelFrontRight, currentSteering, frontTireGrip);

        float downforceValue = currentSpeed * 50;
        carRigidBody.AddForce(-transform.up * downforceValue, ForceMode.Force);
    }


    
    public void ApplyThrottle(float accelInput, Transform wheel)
    {
        // Calculate current speed and speed percentage
        currentSpeed = carRigidBody.velocity.magnitude;
        float speedPercentage = currentSpeed / topSpeed;

        // Calculate torqueValue based on speedPercentage, similar to your existing logic
        float torqueValue;
         if (speedPercentage < 0.5f)
        {
            // Linearly interpolate between 0.5 and 1.0 torque from 0% to 50% of top speed
            torqueValue = Mathf.Lerp(5f, 10f, speedPercentage / 0.5f);
        }
        else if (speedPercentage < 0.75f)
        {
            // Torque is at maximum (1.0) between 50% and 75% of top speed
            torqueValue = 10.0f;
        }
        else
        {
            // Linearly interpolate torque to decrease from 1.0 to 0.4 from 75% to 100% of top speed
            // Calculate how far along this segment we are (from 0 at 75% to 1 at 100%)
            float factor = (speedPercentage - 7.5f) / (10f - 7.5f);
            torqueValue = Mathf.Lerp(10f, 4f, factor);
        }

        // Determine the direction of force based on accelInput sign (forward for accel, backward for decel)
        Vector3 forceDirection = (accelInput >= 0) ? wheel.forward : -wheel.forward;

        // Optional: Scale torqueValue by accelInput for variable throttle/brake intensity
        torqueValue *= Mathf.Abs(accelInput);

        // Apply force in the determined direction
        carRigidBody.AddForceAtPosition((forceDirection * torqueValue * carRigidBody.mass)/2, wheel.position);
    }


    public void Brake(float brakeStrength)
    {
        // Alternatively, apply a backward force
        Vector3 brakeForce = -transform.forward * brakeStrength;
        //currentAcceleration -= brakeForce;
        carRigidBody.AddForce(brakeForce, ForceMode.Force);
    }


    public void Steer(Transform wheel, float steeringInput, float gripFactor)
    {
         float turnAngle = steeringInput * 55 * Time.deltaTime * gripFactor; // Define maxTurnAngle based on desired turning rate
        carRigidBody.MoveRotation(carRigidBody.rotation * Quaternion.Euler(0f, turnAngle, 0f));
        /* Vector3 steeringDir = transform.right * steeringInput; // Assuming steeringInput ranges from -1 (left) to 1 (right)
        Vector3 tireWorldVel = carRigidBody.GetPointVelocity(wheel.position);

        // Calculate the tire's lateral velocity component (i.e., velocity in the right/left direction of the car)
        float lateralVel = Vector3.Dot(steeringDir, tireWorldVel);

        // Desired change in lateral velocity to reduce slipping and simulate grip
        float desiredVelChange = -lateralVel * gripFactor; // Adjust gripFactor for tire grip simulation

        // Calculate the desired acceleration to achieve the velocity change within the next physics update
        float desiredAccel = desiredVelChange / Time.fixedDeltaTime;

        // Apply a force in the steering direction proportional to the desired acceleration
        carRigidBody.AddForceAtPosition(steeringDir * carRigidBody.mass * desiredAccel, wheel.position); */
    }

    public void ApplySuspensionForce(Transform wheel) 
    {
        RaycastHit hit;
        Vector3 rayOrigin = wheel.position;
        // Use the suspensionRestDistance directly from the class level variable, assuming it's defined
        if (Physics.Raycast(rayOrigin, -transform.up, out hit, suspensionRestDist)) {
            Debug.DrawRay(rayOrigin, -transform.up * hit.distance, Color.yellow);

            Vector3 springDir = wheel.up;
            Vector3 tireWorldVel = carRigidBody.GetPointVelocity(wheel.position);
            float offset =  suspensionRestDist - hit.distance;
            float vel = Vector3.Dot(springDir, tireWorldVel);
            float force = (offset * springStrength) - (vel * dampingStrength);

            carRigidBody.AddForceAtPosition(springDir * force, wheel.position);
        }
    }

}
