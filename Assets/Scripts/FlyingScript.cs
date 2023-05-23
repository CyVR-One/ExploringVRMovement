using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class FlyingScript : MonoBehaviour
{
    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;

    // Flight parameters
    [Tooltip("Distance between hands required to initiate a flap")]
    public float flapThreshold = 0.3f;

    [Tooltip("Distance between hands required to speed up during flap")]
    public float closeThreshold = 0.2f;

    [Tooltip("Strength of the upward force applied during a flap")]
    public float flapPower = 10f;

    [Tooltip("Additional boost given when initiating a flap")]
    public float flapBoost = 5f;

    [Tooltip("Factor by which speed increases when hands are close together")]
    public float speedUpFactor = 2f;

    [Tooltip("Factor that determines the efficiency of gliding, affects the player's ability to maintain height and speed during a glide")]
    public float glideFactor = 1f;

    [Tooltip("Drag applied to the player during flight, slowing them down")]
    public float flightDrag = 0.05f;

    [Tooltip("Rate at which the player's speed changes towards the target speed")]
    public float speedChangeRate = 1f; 

    [Tooltip("Base speed of the player in flight")]
    public float baseSpeed = 10f;

    [Tooltip("Threshold at which player's speed is low enough to start applying glide drag")]
    public float glideSpeedThreshold = 0.1f; 

    [Tooltip("Drag applied to the player during a glide, slowing them down")]
    public float glideDrag = 0.02f;

    [Tooltip("Minimum force that will be applied during a glide, even when player's hands are close to their head")]
    public float minGlideForce = 1f;

    [Tooltip("Factor that determines how much player's forward tilt affects their forward movement")]
    public float forwardTiltFactor = 1.0f;

    [Tooltip("Factor that determines the strength of player's turning or banking ability")]
    public float turnFactor;

    [Tooltip("Coefficient used in the lift equation, affects the amount of lift generated during flight")]
    public float liftCoefficient = 1.0f;

    [Tooltip("Representative area of player's wings used in the lift equation, affects the amount of lift generated during flight")]
    public float wingArea = 1.0f;

    [Tooltip("Density of the air used in the lift equation, affects the amount of lift generated during flight")]
    public float airDensity = 1.225f;

    [Tooltip("Distance to cast the ground check ray")]
    public float groundCheckDistance = 0.2f;
    [Tooltip("The force applied to the player during a glide")]
    public float glideForce = 10f;

    [Tooltip("The smoothness with which the player transitions between different glide forces")]
    public float glideSmoothness = 5f;

    [Tooltip("The smoothness with which the player turns")]

    public float maxForwardSpeed = 10.0f; // Set this value to the maximum forward speed of the player

    private float currentTurnSpeed = 0f;
    private const float turnSmoothness = 5f;
    private const float directionChangeSmoothness = 5f;

    private float? turningDirection = null;
    private const float turnThreshold = 0.1f; 

    //Control position variables
    private Vector3 leftHandPos;
    private Vector3 rightHandPos;
    private Vector3 leftHandMovement;
    private Vector3 rightHandMovement;
    private Vector3 headsetPosition;
    private Quaternion headsetRotation;



    

    // State variables
    private Rigidbody rb;
    private Vector3 leftHandPosOld, rightHandPosOld;
    private float flapCooldown = 0f;
    private float targetSpeed;
    private bool isFlapping = false;
    private InputDevice headDevice;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        InitializeControllers();
        InitializeHeadset();
    }

    void Update()
    {
        InitializeControllers();
        InitializeHeadset();
        // Get the hand and headset positions.
        bool areHandsActive = GetHandPositions(out leftHandPos, out rightHandPos);
        if (!areHandsActive) return;

        bool isHeadsetActive = GetHeadsetPosition(out headsetPosition);
        if (!isHeadsetActive) return;

        // Calculate hand movement and flap speed.
        leftHandMovement = leftHandPos - leftHandPosOld;
        rightHandMovement = rightHandPos - rightHandPosOld;

        // Calculate the average flap speed.
        float avgFlapSpeed = GetAverageFlapSpeed();

        // Determine whether the user is in flap or glide mode.
        bool isFlapMode = IsFlapMode();
        bool isGlideMode = IsGlideMode();

        // Use isFlapMode and isGlideMode instead of handDistance and avgFlapSpeed to determine whether to flap or glide
        if (isFlapMode)
        {
            FlapWings();
            ChangeSpeed();
        }

        if (isGlideMode)
        {
            Glide();
        }

        Turn();
        MoveForward();

        // Store current hand positions for the next frame.
        leftHandPosOld = leftHandPos;
        rightHandPosOld = rightHandPos;

        // Update flap cooldown.
        if (flapCooldown > 0) 
            flapCooldown -= Time.deltaTime;

        CorrectTilt();
    }


    private void InitializeControllers()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left, devices);
        if (devices.Count > 0)
        {
            leftHandDevice = devices[0];
        }

        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right, devices);
        if (devices.Count > 0)
        {
            rightHandDevice = devices[0];
        }
    }

    private bool GetHandPositions(out Vector3 leftHandPos, out Vector3 rightHandPos)
    {
        var leftHandActive = leftHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out leftHandPos);
        var rightHandActive = rightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out rightHandPos);

        if (!leftHandActive)
            Debug.Log("Left hand controller not found");

        if (!rightHandActive)
            Debug.Log("Right hand controller not found");

        return leftHandActive && rightHandActive;
    }

    private void InitializeHeadset()
    {
        headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            headsetRotation = rotation;
        }
    }

    private bool GetHeadsetPosition(out Vector3 headsetPos)
    {
        var headsetActive = headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out headsetPos);

        if (!headsetActive)
            Debug.Log("Headset not found");

        // update rotation
        if (headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
        {
            headsetRotation = rotation;
        }

        return headDevice.isValid;
    }

    private bool IsFlapMode()
    {
        bool isFlapMode = (leftHandPos.z >= headsetPosition.z || rightHandPos.z >= headsetPosition.z) && GetAverageFlapSpeed() > flapThreshold;
        if (isFlapMode)
            Debug.Log("Entered Flap Mode");
        return isFlapMode;
    }


    private bool IsGlideMode()
    {
        bool isGlideMode = !IsGrounded() && leftHandPos.z < headsetPosition.z && rightHandPos.z < headsetPosition.z;
        if (isGlideMode)
            Debug.Log("Entered Glide Mode");
        return isGlideMode;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -Vector3.up, groundCheckDistance);
    }


    private float GetAverageFlapSpeed()
    {
        float leftHandSpeed = leftHandMovement.magnitude / Time.deltaTime;
        float rightHandSpeed = rightHandMovement.magnitude / Time.deltaTime;
        float avgFlapSpeed = (leftHandSpeed + rightHandSpeed) / 2f;

        return avgFlapSpeed;
    }


    private void FlapWings()
    {
        float avgFlapSpeed = GetAverageFlapSpeed();
        if (IsFlapConditionMet() && flapCooldown <= 0)
        {
            isFlapping = true;
            Vector3 flapForce = Vector3.up * flapPower * ((Mathf.Abs(leftHandMovement.y) + Mathf.Abs(rightHandMovement.y)) / 2);

            // Subtract a fraction of the forward velocity from the flap force
            float forwardForce = rb.velocity.z;

            flapForce -= new Vector3(0, 0, forwardForce * 0.2f);  // Adjust the 0.8f to your preference
            Debug.Log("Player velocity before flap: " + rb.velocity );
            Debug.Log("Applying flap force: " + flapForce);
            rb.AddForce(flapForce, ForceMode.Impulse);
            Debug.Log("Player velocity after flap: " + rb.velocity);
            flapCooldown = 0.5f; // prevent flapping too often
        }
        else if (flapCooldown <= 0)
        {
            isFlapping = false;
        }
    }


    private bool IsFlapConditionMet()
    {
        float handDistance = Vector3.Distance(leftHandPos, rightHandPos);
        float avgFlapSpeed = GetAverageFlapSpeed();
        if (handDistance > flapThreshold && avgFlapSpeed > flapThreshold && 
            leftHandMovement.y < 0 && rightHandMovement.y < 0)
        {
            return true;
        }
        return false;
    }

    private void ChangeSpeed()
    {
        float handDistance = Vector3.Distance(leftHandPos, rightHandPos);
        float avgFlapSpeed = GetAverageFlapSpeed();
        // If hands are close together, apply forward force (speed up)
        if (handDistance < closeThreshold)
        {
            targetSpeed = rb.velocity.magnitude * speedUpFactor;
        }
        // If hands are not close together, target speed is normal speed
        else
        {
            targetSpeed = baseSpeed;
        }

        // Gradually change speed towards target speed
        float newSpeed = Mathf.Lerp(rb.velocity.magnitude, targetSpeed, speedChangeRate * Time.deltaTime);
        rb.velocity = rb.velocity.normalized * newSpeed;
    }

    private void Glide()
    {
        if (!isFlapping && !IsGrounded())
        {
            float glideRatio = GetGlideRatio();
            float avgFlapSpeed = GetAverageFlapSpeed();
            float handDistance = Vector3.Distance(leftHandPos, rightHandPos);

            // Calculate glide force
            float newGlideForce = glideFactor * glideRatio + minGlideForce;

            // Smoothly transition between old and new glide force
            glideForce = Mathf.Lerp(glideForce, newGlideForce, Time.deltaTime * glideSmoothness);

            // Add forward force based on glide force
            rb.AddForce(transform.forward * glideForce, ForceMode.Force);

            // Adjust velocity to maintain forward momentum
            if (rb.velocity.magnitude < glideSpeedThreshold)
            {
                rb.velocity = new Vector3(rb.velocity.x, glideDrag * rb.velocity.y, rb.velocity.z * glideDrag);
            }

            // Forward force based on the headset tilt
            Vector3 headsetForward = headsetPosition - transform.position;
            if (Vector3.Dot(headsetForward, transform.forward) > 0) // if the player is looking forward
            {
                rb.AddForce(transform.forward * glideFactor, ForceMode.Force); // Add forward force during glide
            }

            // Add these inside the Glide() function
            //ChangeSpeed(handDistance, avgFlapSpeed);
            //Turn(leftHandPos, rightHandPos);
            MoveForward();
        }
    }

    /*private void Turn()
    {
        // Calculate the turn direction based on the relative height of the hands
        float newTurningDirection = rightHandPos.y - leftHandPos.y;

        // If the hands are almost at the same height, stop turning
        if (Mathf.Abs(newTurningDirection) < turnThreshold)
        {
            turningDirection = null;
            Debug.Log("Not Turning");
        }
        else
        {
            // If we're already turning
            if (turningDirection.HasValue)
            {
                // And the new turn direction is in the opposite direction, interpolate the turningDirection towards the new direction
                if (Mathf.Sign(newTurningDirection) != Mathf.Sign(turningDirection.Value))
                {
                    Debug.Log("Changing Turning Direction");
                    turningDirection = Mathf.Lerp(turningDirection.Value, newTurningDirection, Time.deltaTime * directionChangeSmoothness);
                }
            }
            else  // We're not currently turning, so start turning in the new direction
            {
                turningDirection = newTurningDirection;
            }

            // If we're turning, apply the rotation
            if (turningDirection.HasValue)
            {
                // Calculate turn modifier based on forward speed
                float turnModifier = Mathf.Clamp(rb.velocity.magnitude / maxForwardSpeed, 0, 1);

                // Interpolate the currentTurnSpeed towards the desired speed
                float desiredTurnSpeed = Mathf.Abs(newTurningDirection) * turnFactor * turnModifier;
                currentTurnSpeed = Mathf.Lerp(currentTurnSpeed, desiredTurnSpeed, Time.deltaTime * turnSmoothness);

                // Apply the rotation. This creates a banking effect, like an airplane.
                Vector3 bankingAxis = Vector3.Cross(transform.up, transform.forward);
                rb.rotation *= Quaternion.AngleAxis(currentTurnSpeed, bankingAxis);

                // Apply the steering force
                Vector3 steeringForce = transform.right * Mathf.Sign(turningDirection.Value) * currentTurnSpeed * rb.velocity.magnitude;
                rb.AddForce(steeringForce, ForceMode.Force);

                // Log the direction of the turn
                if(turningDirection.Value > 0)
                    Debug.Log("Turning Left by " + currentTurnSpeed.ToString() + " degrees");
                else
                    Debug.Log("Turning Right by " + currentTurnSpeed.ToString() + " degrees");
            }
        }
    }*/

    private void Turn()
    {
        if(!isFlapping){
            Vector3 headsetForward = headsetRotation * Vector3.forward;

            Vector3 headToRightHand = rightHandPos - headsetPosition;
            Vector3 headToLeftHand = leftHandPos - headsetPosition;

            float rightHandBankAngle = Vector3.Angle(headsetForward, headToRightHand);
            float leftHandBankAngle = Vector3.Angle(headsetForward, headToLeftHand);

            float newTurningDirection = leftHandBankAngle - rightHandBankAngle;



            // If the hands are almost at the same banking angle, stop turning
            if (Mathf.Abs(newTurningDirection) < turnThreshold)
            {
                turningDirection = null;
                Debug.Log("Not Turning");
            }
            else
            {
                // If we're already turning
                if (turningDirection.HasValue)
                {
                    // And the new turn direction is in the opposite direction, interpolate the turningDirection towards the new direction
                    if (Mathf.Sign(newTurningDirection) != Mathf.Sign(turningDirection.Value))
                    {
                        Debug.Log("Changing Turning Direction");
                        turningDirection = Mathf.Lerp(turningDirection.Value, newTurningDirection, Time.deltaTime * directionChangeSmoothness);
                    }
                }
                else  // We're not currently turning, so start turning in the new direction
                {
                    turningDirection = newTurningDirection;
                }

                // If we're turning, apply the rotation
                if (turningDirection.HasValue)
                {
                    // Calculate turn modifier based on forward speed
                    float turnModifier = Mathf.Clamp(rb.velocity.magnitude / maxForwardSpeed, 0, 1);

                    // Interpolate the currentTurnSpeed towards the desired speed
                    float desiredTurnSpeed = Mathf.Abs(newTurningDirection) * turnFactor * turnModifier;
                    currentTurnSpeed = Mathf.Lerp(currentTurnSpeed, desiredTurnSpeed, Time.deltaTime * turnSmoothness);
                    Debug.Log("turn speed: " + currentTurnSpeed);
                    // Apply the rotation. This creates a banking effect, like an airplane.
                    Vector3 bankingAxis = Vector3.Cross(transform.up, transform.forward);
                    Debug.DrawRay(rb.position, bankingAxis, Color.green);

                    //rb.rotation *= Quaternion.AngleAxis(currentTurnSpeed, bankingAxis);

                    // Log the direction of the turn
                    if(turningDirection.Value > 0)
                        Debug.Log("Turning Left by " + currentTurnSpeed.ToString() + " degrees");
                    else
                        Debug.Log("Turning Right by " + currentTurnSpeed.ToString() + " degrees");
                }
            }
        }
    }




    private void MoveForward()
    {
        if(!IsGrounded()){
            InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            Quaternion headRotation;
            headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out headRotation);
            Vector3 headForward = headRotation * Vector3.forward;

            if (headForward.y < 0) 
                rb.AddForce(transform.forward * -headForward.y * forwardTiltFactor);
        }
    }

    private void CorrectTilt()
    {
        // Compute the correction rotation as a quaternion
        Quaternion correction = Quaternion.FromToRotation(rb.transform.up, Vector3.up);

        // Apply the correction rotation, scaled by some factor to control the speed of correction
        float correctionSpeed = 1f;  // Adjust this value to your liking
        rb.rotation = Quaternion.Lerp(rb.rotation, correction * rb.rotation, correctionSpeed * Time.deltaTime);
    }


    private float GetGlideRatio()
    {
        float leftHandDistance = Vector3.Distance(leftHandPos, headsetPosition);
        float rightHandDistance = Vector3.Distance(rightHandPos, headsetPosition);
        float averageDistance = (leftHandDistance + rightHandDistance) / 2;

        float maxGlideDistance = 1.0f; // You may need to adjust this based on your VR setup

        // Return a ratio between 0 (hands close to head) and 1 (hands at max glide distance or further)
        return Mathf.Clamp01(averageDistance / maxGlideDistance);
    }
}
