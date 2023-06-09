using UnityEngine;
using UnityEngine.XR;

public class HandJetpack : MonoBehaviour
{
    public XRNode leftControllerNode = XRNode.LeftHand;
    public XRNode rightControllerNode = XRNode.RightHand;
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    public float maxJetpackForce = 10f;
    public float maxTurningForce = 1f; // Adjust as needed
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Get devices
        leftDevice = InputDevices.GetDeviceAtXRNode(leftControllerNode);
        rightDevice = InputDevices.GetDeviceAtXRNode(rightControllerNode);

        // Check the trigger values (0.0 to 1.0)
        leftDevice.TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue);
        rightDevice.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue);

        // Get controller orientation
        leftDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftHandRotation);
        rightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightHandRotation);

        // Compute force direction based on controller orientation 
        Vector3 leftForceDirection = leftHandRotation * Vector3.down;  // Now using down direction
        Vector3 rightForceDirection = rightHandRotation * Vector3.down;  // Now using down direction

        // Apply force proportional to the trigger value in the opposite direction
        rb.AddForce(-leftForceDirection * maxJetpackForce * leftTriggerValue);
        rb.AddForce(-rightForceDirection * maxJetpackForce * rightTriggerValue);

        // If only one trigger is pressed, apply a turning force
        if (leftTriggerValue > 0f && rightTriggerValue == 0f)
        {
            // Left trigger pressed - turn right
            rb.AddTorque(transform.up * maxTurningForce * leftTriggerValue, ForceMode.VelocityChange);
        }
        else if (rightTriggerValue > 0f && leftTriggerValue == 0f)
        {
            // Right trigger pressed - turn left
            rb.AddTorque(-transform.up * maxTurningForce * rightTriggerValue, ForceMode.VelocityChange);
        }
    }
}
