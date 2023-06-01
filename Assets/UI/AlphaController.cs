using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class AlphaController : MonoBehaviour
{
    public enum XRInputFeature
    {
        PrimaryButton,
        Trigger,
        Grip,
        Primary2DAxis,  // Joystick
        IsActive,       // Headset
        HeadsetRotation // Headset Rotation
    }

    public enum XRController
    {
        LeftHand,
        RightHand,
        Headset
    }

    private float inactiveAlpha = 0.3f; // Change inactiveAlpha to 0.3f
    [SerializeField] private float headsetSensitivity = 1.0f;  // Define how much headset rotation affects image movement

    [System.Serializable]
    public class InputImagePair
    {
        public XRController controller;
        public XRInputFeature inputFeature;
        public Image imageToChange;
        public Vector2 originalPosition; // To store the original position of joystick images
    }

    [SerializeField] private List<InputImagePair> inputImagePairs = new List<InputImagePair>();
    private float initialAlpha = 1.0f; // Change initialAlpha to 1.0f
    private float activeAlpha = 1.0f;
    [SerializeField] private float joystickSensitivity = 1.0f;  // Define how much joystick input affects image movement

    private void Start()
    {
        foreach (var pair in inputImagePairs)
        {
            ChangeAlpha(pair.imageToChange, initialAlpha);
            pair.originalPosition = pair.imageToChange.rectTransform.anchoredPosition; // Remember the original position
        }
    }

    private void Update()
    {
        foreach (var pair in inputImagePairs)
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(
                pair.controller == XRController.LeftHand ? XRNode.LeftHand : 
                pair.controller == XRController.RightHand ? XRNode.RightHand : 
                XRNode.CenterEye, devices);

            if (devices.Count > 0)
            {
                InputDevice device = devices[0];
                bool isPressed = false;
                Vector2 joystickValue = Vector2.zero;
                Quaternion headsetRotation = Quaternion.identity;

                switch (pair.inputFeature)
                {
                    case XRInputFeature.PrimaryButton:
                        device.TryGetFeatureValue(CommonUsages.primaryButton, out isPressed);
                        break;
                    case XRInputFeature.Trigger:
                        device.TryGetFeatureValue(CommonUsages.triggerButton, out isPressed);
                        break;
                    case XRInputFeature.Grip:
                        device.TryGetFeatureValue(CommonUsages.gripButton, out isPressed);
                        break;
                    case XRInputFeature.Primary2DAxis:  // Joystick
                        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickValue);
                        isPressed = joystickValue.magnitude > 0.2f;  // Joystick is moved if its value's magnitude is more than 0.2
                        break;
                    case XRInputFeature.IsActive:  // Headset
                        device.TryGetFeatureValue(CommonUsages.userPresence, out isPressed);
                        break;
                    case XRInputFeature.HeadsetRotation:  // Headset Rotation
                        device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out headsetRotation);
                        break;
                }

                if (pair.inputFeature == XRInputFeature.Primary2DAxis)
                {
                    if (isPressed)
                    {
                        Vector2 newPosition = pair.originalPosition + joystickValue * joystickSensitivity;
                        pair.imageToChange.rectTransform.anchoredPosition = Vector2.ClampMagnitude(newPosition - pair.originalPosition, 4.0f) + pair.originalPosition;
                    }
                    else
                    {
                        pair.imageToChange.rectTransform.anchoredPosition = pair.originalPosition; // Reset position when joystick is not moving
                    }
                }
                else if (pair.inputFeature == XRInputFeature.HeadsetRotation)
                {
                    // Convert the quaternion to Euler angles
                    Vector3 eulerRotation = headsetRotation.eulerAngles;

                    // Map the angles to [-180, 180]
                    float pitch = eulerRotation.x > 180 ? eulerRotation.x - 360 : eulerRotation.x;
                    float yaw = eulerRotation.y > 180 ? eulerRotation.y - 360 : eulerRotation.y;

                    // Pitch contributes to Y movement (up and down) - invert the direction by multiplying with -1
                    // Yaw contributes to X movement (left and right)
                    float newY = Mathf.Clamp(pitch * headsetSensitivity, -5f, 5f); // Removed the - sign before pitch
                    float newX = Mathf.Clamp(yaw * headsetSensitivity, -5f, 5f);

                    pair.imageToChange.rectTransform.anchoredPosition = new Vector2(newX, newY);
                }

                // For headset, always set alpha to 100%
                if (pair.inputFeature == XRInputFeature.HeadsetRotation || pair.inputFeature == XRInputFeature.IsActive)
                    ChangeAlpha(pair.imageToChange, activeAlpha);
                else // For other inputs, set alpha based on whether the button is pressed or not
                    ChangeAlpha(pair.imageToChange, isPressed ? activeAlpha : inactiveAlpha);
            }
        }
    }


    private void ChangeAlpha(Image imageToChange, float alpha)
    {
        Color color = imageToChange.color;
        color.a = alpha;
        imageToChange.color = color;
    }
}
