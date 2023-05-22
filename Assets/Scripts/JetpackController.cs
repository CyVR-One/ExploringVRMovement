using UnityEngine;
using UnityEngine.XR;

public class JetpackController : MonoBehaviour
{
    public XRNode leftControllerNode = XRNode.LeftHand; // Set this to LeftHand or RightHand in the Inspector
    public XRNode rightControllerNode = XRNode.RightHand; // Set this to LeftHand or RightHand in the Inspector

    private bool isJetpackActive = false;
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    private Vector2 joystickInputLeft;
    private Vector2 joystickInputRight;

    public float jetpackForce = 10f;
    public float navigationForce = 3f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Make sure your player GameObject has a Rigidbody component
    }

    void Update()
    {
        // Get left and right devices
        leftDevice = InputDevices.GetDeviceAtXRNode(leftControllerNode);
        rightDevice = InputDevices.GetDeviceAtXRNode(rightControllerNode);

        // Check if both primary buttons are being pressed
        leftDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool isJetpackActiveLeft);
        rightDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool isJetpackActiveRight);
        isJetpackActive = isJetpackActiveLeft || isJetpackActiveRight;

        // Get joystick input from both controllers
        leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickInputLeft);
        rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickInputRight);

        if (isJetpackActive)
        {
            rb.AddForce(Vector3.up * jetpackForce);
        }

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
