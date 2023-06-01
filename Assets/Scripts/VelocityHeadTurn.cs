using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VelocityHeadTurn : MonoBehaviour
{
    public XRNode leftHand = XRNode.LeftHand;
    public XRNode rightHand = XRNode.RightHand;
    public XRNode head = XRNode.Head;
    public float baseSpeed = 1.0f;
    public float forwardTiltSensitivity = 5.0f;
    public float backwardTiltSensitivity = 10.0f;
    public float rotationSpeed = 30.0f;

    private Vector3 lastLeftHandPosition;
    private Vector3 lastRightHandPosition;
    private Vector3 initialHeadPosition;
    private Quaternion initialHeadRotation;
    private float lastUpdateTime;

    void Start()
    {
        InputDevice leftHandDevice = InputDevices.GetDeviceAtXRNode(leftHand);
        InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(rightHand);
        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(head);
        leftHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out lastLeftHandPosition);
        rightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out lastRightHandPosition);
        headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out initialHeadPosition);
        headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out initialHeadRotation);
        // zero out the initial rotation
        initialHeadRotation.eulerAngles = new Vector3(0, initialHeadRotation.eulerAngles.y, 0);
        lastUpdateTime = Time.time;
    }

    void Update()
    {
        InputDevice leftHandDevice = InputDevices.GetDeviceAtXRNode(leftHand);
        InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(rightHand);
        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(head);

        Vector3 currentLeftHandPosition, currentRightHandPosition;
        leftHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out currentLeftHandPosition);
        rightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out currentRightHandPosition);

        Quaternion currentHeadRotation;
        headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out currentHeadRotation);

        Vector3 tilt = currentHeadRotation.eulerAngles - initialHeadRotation.eulerAngles;
        tilt.x = (tilt.x > 180) ? tilt.x - 360 : tilt.x;
        tilt.y = (tilt.y > 180) ? tilt.y - 360 : tilt.y;
        tilt.z = (tilt.z > 180) ? tilt.z - 360 : tilt.z;

        // Calculate movement deltas for each hand
        Vector3 leftHandDelta = currentLeftHandPosition - lastLeftHandPosition;
        Vector3 rightHandDelta = currentRightHandPosition - lastRightHandPosition;

        // Calculate the time delta
        float timeDelta = Time.time - lastUpdateTime;

        // Calculate the velocities for each hand
        Vector3 leftHandVelocity = leftHandDelta / timeDelta;
        Vector3 rightHandVelocity = rightHandDelta / timeDelta;

        // Calculate the average velocity of both hands
        float averageVelocity = (leftHandVelocity.magnitude + rightHandVelocity.magnitude) / 2.0f;

        // Calculate forward direction based on the forward/backward tilt of the headset
        float forwardTilt = tilt.x > 0 ? 1 : -1;

        // Rotate the player based on the left/right tilt of the headset
        float rotationTilt = tilt.y * rotationSpeed * Time.deltaTime;
        transform.Rotate(0, rotationTilt, 0);

        // Move the player in the forward direction with a speed based on the hand velocity
        transform.position += transform.forward * baseSpeed * averageVelocity * forwardTilt * Time.deltaTime;

        // Save the current positions and time for the next update
        lastLeftHandPosition = currentLeftHandPosition;
        lastRightHandPosition = currentRightHandPosition;
        lastUpdateTime = Time.time;
    }

}
