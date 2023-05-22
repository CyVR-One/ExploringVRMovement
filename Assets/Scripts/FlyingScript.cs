using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class FlyingScript : MonoBehaviour
{
    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;

    // Flight parameters
    public float flapThreshold = 0.3f;
    public float closeThreshold = 0.2f;
    public float flapPower = 10f;
    public float flapBoost = 5f;
    public float speedUpFactor = 2f;
    public float glideFactor = 1f;
    public float flightDrag = 0.05f;
    public float speedChangeRate = 1f; 
    public float baseSpeed = 10f;
    public float glideSpeedThreshold = 0.1f; 
    public float glideDrag = 0.02f; 
    public float minGlideForce = 1f; // You can adjust this value based on your needs

    public float forwardTiltFactor = 1.0f; 
    public float turnFactor;

    public float liftCoefficient = 1.0f;
    public float wingArea = 1.0f;
    public float airDensity = 1.225f;

    

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
        Vector3 leftHandPos, rightHandPos;
        bool areHandsActive = GetHandPositions(out leftHandPos, out rightHandPos);
        if (!areHandsActive) return;

        Vector3 headsetPos;
        bool isHeadsetActive = GetHeadsetPosition(out headsetPos);
        if (!isHeadsetActive) return;

        float handDistance = Vector3.Distance(leftHandPos, rightHandPos);
        Vector3 leftHandMovement = leftHandPos - leftHandPosOld;
        Vector3 rightHandMovement = rightHandPos - rightHandPosOld;
        float avgFlapSpeed = GetAverageFlapSpeed(leftHandMovement, rightHandMovement);

        bool isFlapMode = IsFlapMode(leftHandPos, rightHandPos, headsetPos);
        bool isGlideMode = IsGlideMode(leftHandPos, rightHandPos, headsetPos);

        // Use isFlapMode and isGlideMode instead of handDistance and avgFlapSpeed to determine whether to flap or glide
        if (isFlapMode)
        {
            FlapWings(leftHandPos, rightHandPos, leftHandMovement, rightHandMovement, avgFlapSpeed);
            ChangeSpeed(handDistance, avgFlapSpeed);
        }
        if (isGlideMode) Glide(leftHandPos, rightHandPos, headsetPos);

        Turn(leftHandPos, rightHandPos);
        MoveForward();

        // Store current hand positions for the next frame
        leftHandPosOld = leftHandPos;
        rightHandPosOld = rightHandPos;

        // Update flap cooldown
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
    }

    private bool GetHeadsetPosition(out Vector3 headsetPos)
    {
        var headsetActive = headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out headsetPos);

        if (!headsetActive)
            Debug.Log("Headset not found");

        return headsetActive;
    }

    private bool IsFlapMode(Vector3 leftHandPos, Vector3 rightHandPos, Vector3 headsetPos)
    {
        // Consider it flapping mode if either hand is in front of or in line with the headset
        bool isFlapMode = leftHandPos.z >= headsetPos.z || rightHandPos.z >= headsetPos.z;
        if(isFlapMode)
            Debug.Log("Entered Flap Mode");
        return isFlapMode;
    }

    private bool IsGlideMode(Vector3 leftHandPos, Vector3 rightHandPos, Vector3 headsetPos)
    {
        // Consider it gliding mode if both hands are behind the headset
        bool isGlideMode = leftHandPos.z < headsetPos.z && rightHandPos.z < headsetPos.z;
    
        if(isGlideMode)
            Debug.Log("Entered Glide Mode");
        
        return isGlideMode;
    }

    private float GetAverageFlapSpeed(Vector3 leftHandMovement, Vector3 rightHandMovement)
    {
        float leftHandSpeed = leftHandMovement.magnitude / Time.deltaTime;
        float rightHandSpeed = rightHandMovement.magnitude / Time.deltaTime;
        float avgFlapSpeed = (leftHandSpeed + rightHandSpeed) / 2f;

        return (leftHandSpeed + rightHandSpeed) / 2f;
    }

    private void FlapWings(Vector3 leftHandPos, Vector3 rightHandPos, Vector3 leftHandMovement, Vector3 rightHandMovement, float avgFlapSpeed)
    {
        if (IsFlapConditionMet(leftHandPos, rightHandPos, leftHandMovement, rightHandMovement, avgFlapSpeed) && flapCooldown <= 0)
        {
            isFlapping = true;
            Vector3 flapForce = Vector3.up * flapPower * ((Mathf.Abs(leftHandMovement.y) + Mathf.Abs(rightHandMovement.y)) / 2);

            // Subtract a fraction of the forward velocity from the flap force
            float forwardForce = rb.velocity.z;
            flapForce -= new Vector3(0, 0, forwardForce * 0.2f);  // Adjust the 0.8f to your preference
            
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


    private bool IsFlapConditionMet(Vector3 leftHandPos, Vector3 rightHandPos, Vector3 leftHandMovement, Vector3 rightHandMovement, float avgFlapSpeed)
    {
        float handDistance = Vector3.Distance(leftHandPos, rightHandPos);

        // Check the conditions for a flap: hands moving down quickly and spread apart
        if (handDistance > flapThreshold && avgFlapSpeed > flapThreshold && 
            leftHandMovement.y < 0 && rightHandMovement.y < 0)
        {
            return true;
        }

        return false;
    }




    private void ChangeSpeed(float handDistance, float avgFlapSpeed)
    {
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

    private void Glide(Vector3 leftHandPos, Vector3 rightHandPos, Vector3 headsetPosition)
    {
        if (!isFlapping)
        {

            float glideRatio = GetGlideRatio(leftHandPos, rightHandPos, headsetPosition);
        
            // Add forward force based on glide ratio, with some minimum force to keep gliding even with hands close to head
            rb.AddForce(transform.forward * (glideFactor * glideRatio + minGlideForce), ForceMode.Force);
            Vector3 handVector = rightHandPos - leftHandPos;
            float tiltAngle = Vector3.Angle(handVector, transform.up);
            Vector3 glideDirection = Quaternion.AngleAxis(tiltAngle, transform.forward) * transform.up;
            Vector3 newForward = Vector3.Lerp(transform.forward, glideDirection, glideFactor);
            Quaternion targetRotation = Quaternion.FromToRotation(transform.forward, newForward);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, glideFactor * Time.deltaTime);

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
        }
    }






    private void Turn(Vector3 leftHandPos, Vector3 rightHandPos)
    {
        // Calculate the turn direction based on the relative height of the hands
        float turnDirection = rightHandPos.y - leftHandPos.y;
        Vector3 bankingAxis = Vector3.Cross(transform.up, transform.forward);

        // Apply the rotation. This creates a banking effect, like an airplane
        rb.rotation *= Quaternion.AngleAxis(turnDirection * turnFactor, bankingAxis);
        
        // If the player is not trying to turn, apply a rotation to correct back to upright
        if (Mathf.Approximately(turnDirection, 0))
        {
            // Compute the correction rotation as a quaternion
            Quaternion correction = Quaternion.FromToRotation(rb.transform.up, Vector3.up);
            
            // Apply the correction rotation, scaled by some factor to control the speed of correction
            float correctionSpeed = 1f;  // Adjust this value to your liking
            rb.rotation *= Quaternion.Lerp(Quaternion.identity, correction, correctionSpeed * Time.deltaTime);
        }
    }



    private void MoveForward()
    {
        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        Quaternion headRotation;
        headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out headRotation);
        Vector3 headForward = headRotation * Vector3.forward;

        if (headForward.y < 0) 
            rb.AddForce(transform.forward * -headForward.y * forwardTiltFactor);
    }

    private void CorrectTilt()
    {
        // Compute the correction rotation as a quaternion
        Quaternion correction = Quaternion.FromToRotation(rb.transform.up, Vector3.up);

        // Apply the correction rotation, scaled by some factor to control the speed of correction
        float correctionSpeed = 1f;  // Adjust this value to your liking
        rb.rotation = Quaternion.Lerp(rb.rotation, correction * rb.rotation, correctionSpeed * Time.deltaTime);
    }


    private float GetGlideRatio(Vector3 leftHandPos, Vector3 rightHandPos, Vector3 headsetPos)
    {
        float leftHandDistance = Vector3.Distance(leftHandPos, headsetPos);
        float rightHandDistance = Vector3.Distance(rightHandPos, headsetPos);
        float averageDistance = (leftHandDistance + rightHandDistance) / 2;

        float maxGlideDistance = 1.0f; // You may need to adjust this based on your VR setup

        // Return a ratio between 0 (hands close to head) and 1 (hands at max glide distance or further)
        return Mathf.Clamp01(averageDistance / maxGlideDistance);
    }
}
