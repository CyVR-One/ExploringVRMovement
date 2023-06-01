using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class SwimmingController : MonoBehaviour
{
    public float baseGlideSpeed = 5.0f;
    public float deceleration = 0.95f;
    public float turningFactor = 1.0f; // Modify this to adjust the speed of turning.

    private Vector3 previousLeftHandPosition;
    private Vector3 previousRightHandPosition;
    private float threshold = 0.05f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            gameObject.AddComponent<Rigidbody>();
            rb = GetComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = false;
        previousLeftHandPosition = GetWorldHandPosition(XRNode.LeftHand);
        previousRightHandPosition = GetWorldHandPosition(XRNode.RightHand);
    }

    void Update()
    {
        Vector3 currentLeftHandPosition = GetWorldHandPosition(XRNode.LeftHand);
        Vector3 currentRightHandPosition = GetWorldHandPosition(XRNode.RightHand);

        Vector3 leftHandMovement = previousLeftHandPosition - currentLeftHandPosition;
        Vector3 rightHandMovement = previousRightHandPosition - currentRightHandPosition;

        bool isLeftHandMoving = leftHandMovement.magnitude > threshold;
        bool isRightHandMoving = rightHandMovement.magnitude > threshold;

        if (isLeftHandMoving)
        {
            rb.AddForce(leftHandMovement.normalized * baseGlideSpeed, ForceMode.Impulse);
            if (!isRightHandMoving) // Only the left hand is moving.
            {
                transform.Rotate(Vector3.up, -turningFactor * Time.deltaTime);
            }
        }

        if (isRightHandMoving)
        {
            rb.AddForce(rightHandMovement.normalized * baseGlideSpeed, ForceMode.Impulse);
            if (!isLeftHandMoving) // Only the right hand is moving.
            {
                transform.Rotate(Vector3.up, turningFactor * Time.deltaTime);
            }
        }

        // Apply deceleration.
        rb.velocity = rb.velocity * deceleration;

        previousLeftHandPosition = currentLeftHandPosition;
        previousRightHandPosition = currentRightHandPosition;
    }

    private Vector3 GetWorldHandPosition(XRNode hand)
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(hand, devices);
        foreach (var device in devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
            {
                return position;
            }
        }

        return Vector3.zero;
    }
}
