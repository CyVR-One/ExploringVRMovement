using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ControllerVelocity : MonoBehaviour
{
    public XRNode leftHand = XRNode.LeftHand;
    public XRNode rightHand = XRNode.RightHand;
    public XRNode head = XRNode.Head;
    public float speed = 1.0f;

    private Vector3 lastLeftHandPosition;
    private Vector3 lastRightHandPosition;
    private float lastUpdateTime;

    private bool isFirstUpdate = true;

    public float AverageVelocity { get; private set; }

    void Start()
    {
        InputDevice leftHandDevice = InputDevices.GetDeviceAtXRNode(leftHand);
        InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(rightHand);

        leftHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out lastLeftHandPosition);
        rightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out lastRightHandPosition);
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

        // Calculate movement deltas for each hand
        Vector3 leftHandDelta = currentLeftHandPosition - lastLeftHandPosition;
        Vector3 rightHandDelta = currentRightHandPosition - lastRightHandPosition;

        // Calculate the time delta
        float timeDelta = Time.time - lastUpdateTime;

        // Calculate the velocities for each hand
        Vector3 leftHandVelocity = leftHandDelta / timeDelta;
        Vector3 rightHandVelocity = rightHandDelta / timeDelta;

        // Calculate the average velocity of both hands
        AverageVelocity = (leftHandVelocity.magnitude + rightHandVelocity.magnitude) / 2.0f;

        // If any hand moved
        if (!isFirstUpdate && leftHandVelocity.magnitude > 0.001f || rightHandVelocity.magnitude > 0.001f)
        {
            // Get the headset's rotation
            Quaternion headRotation;
            headDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out headRotation);

            // Calculate forward direction based on the headset's rotation
            Vector3 forwardDirection = headRotation * Vector3.forward;

            // Remove the Y component from the forward direction
            forwardDirection.y = 0;

            // Normalize the forward direction to ensure consistent speed
            forwardDirection = forwardDirection.normalized;

            // Move the player in the forward direction with a speed based on the average velocity
            transform.position += forwardDirection * speed * AverageVelocity * Time.deltaTime;
        }
        isFirstUpdate = false;
        lastLeftHandPosition = currentLeftHandPosition;
        lastRightHandPosition = currentRightHandPosition;
        lastUpdateTime = Time.time;
    }
}
