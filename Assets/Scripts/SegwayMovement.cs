using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class SegwayMovement : MonoBehaviour
{
    // Speed of the segway
    public float speed = 1.0f;

    // Sensitivity of the tilt input
    public float tiltSensitivity = 0.1f;

    // Minimum tilt required to start moving
    public float minTilt = 0.2f;

    private InputDevice device;
    private Quaternion currentTilt;
    private Vector3 direction;

    private void Start()
    {
        // Get the input device representing the VR headset
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.Head, devices);
        if (devices.Count > 0)
        {
            device = devices[0];
        }
    }

    private void Update()
    {
        // Get the current tilt of the headset
        device.TryGetFeatureValue(CommonUsages.deviceRotation, out currentTilt);

        // Convert the tilt into a forward direction
        direction = new Vector3(currentTilt.x, 0, currentTilt.z);

        // If the headset is tilted enough forward or backward, start moving
        if (Mathf.Abs(currentTilt.x) > minTilt || Mathf.Abs(currentTilt.z) > minTilt)
        {
            // Normalize the direction and scale it by the speed and the frame duration
            direction.Normalize();
            direction *= speed * Time.deltaTime;

            // Apply the movement to the camera rig
            transform.position += transform.TransformDirection(direction);
        }
    }
}
