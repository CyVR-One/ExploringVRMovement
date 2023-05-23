using UnityEngine;
using UnityEngine.XR;

public class HandJetpack : MonoBehaviour
{
    public XRNode leftControllerNode = XRNode.LeftHand;
    public XRNode rightControllerNode = XRNode.RightHand;
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    private Vector3 initialLeftHandPosition;
    private Vector3 initialRightHandPosition;

    private Vector3 leftHandPosition;
    private Vector3 rightHandPosition;

    private Vector2 joystickInputLeft;
    private Vector2 joystickInputRight;

    public float jetpackForce = 10f;
    public float navigationForce = 3f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>(); 

        // Get initial hand positions
        leftDevice = InputDevices.GetDeviceAtXRNode(leftControllerNode);
        rightDevice = InputDevices.GetDeviceAtXRNode(rightControllerNode);

        leftDevice.TryGetFeatureValue(CommonUsages.devicePosition, out initialLeftHandPosition);
        rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out initialRightHandPosition);
    }

    void Update()
    {
        // Get current hand positions
        leftDevice.TryGetFeatureValue(CommonUsages.devicePosition, out leftHandPosition);
        rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out rightHandPosition);

        // Compute hand movement direction
        Vector3 leftHandDirection = initialLeftHandPosition - leftHandPosition;
        Vector3 rightHandDirection = initialRightHandPosition - rightHandPosition;

        // Average hand movement direction for jetpack thrust
        Vector3 jetpackDirection = ((leftHandDirection + rightHandDirection) / 2.0f).normalized;

        rb.AddForce(jetpackDirection * jetpackForce);

        // Get joystick input from both controllers
        leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickInputLeft);
        rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickInputRight);

        // If either joystick is being used, move in that direction
        Vector3 navigationDirection = Vector3.zero;
        if (joystickInputLeft != Vector2.zero)
        {
            navigationDirection = new Vector3(joystickInputLeft.x, 0, joystickInputLeft.y).normalized;
            rb.AddForce(navigationDirection * navigationForce);
        }

        if (joystickInputRight != Vector2.zero)
        {
            navigationDirection = new Vector3(joystickInputRight.x, 0, joystickInputRight.y).normalized;
            rb.AddForce(navigationDirection * navigationForce);
        }
    }
}
