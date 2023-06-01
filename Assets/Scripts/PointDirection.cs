using UnityEngine;
using UnityEngine.XR;

public class PointDirection : MonoBehaviour
{
    public XRNode leftControllerNode = XRNode.LeftHand;
    public XRNode rightControllerNode = XRNode.RightHand;
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    public float maxJetpackForce = 10f;
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
        Vector3 leftForceDirection = leftHandRotation * Vector3.up;    // Now using up direction
        Vector3 rightForceDirection = rightHandRotation * Vector3.up;  // Now using up direction

        // Apply force proportional to the trigger value in opposite direction
        rb.AddForce(-leftForceDirection * maxJetpackForce * leftTriggerValue);
        rb.AddForce(-rightForceDirection * maxJetpackForce * rightTriggerValue);
    }
}
