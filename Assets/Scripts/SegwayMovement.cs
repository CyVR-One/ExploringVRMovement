using UnityEngine;
using UnityEngine.XR;

public class SegwayMovement : MonoBehaviour
{
    public float speed = 1.0f;
    public float rotationSpeed = 30.0f; // speed of rotation
    public Transform headTransform; // Drag your VR headset transform here in the inspector.

    private Vector3 initialHeadLocalPosition;
    private float maxZTilt = 0.0f;
    private float minZTilt = 0.0f;

    void Start()
    {
        // We assume that at the start, player stands upright.
        initialHeadLocalPosition = headTransform.localPosition;
        maxZTilt = initialHeadLocalPosition.z;
        minZTilt = initialHeadLocalPosition.z;
    }

    void Update()
    {
        // The tilt is calculated as the current head position relative to the initial head position.
        // We only care about the X (sideways tilt) and Z (forward/backward tilt) components.
        Vector3 tilt = headTransform.localPosition - initialHeadLocalPosition;
        tilt.y = 0; 

        // Update the maximum and minimum Z tilt values
        maxZTilt = Mathf.Max(maxZTilt, headTransform.localPosition.z);
        minZTilt = Mathf.Min(minZTilt, headTransform.localPosition.z);

        // Normalize the Z tilt value to be in the range [-1, 1]
        float normalizedZTilt = (headTransform.localPosition.z - minZTilt) / (maxZTilt - minZTilt) * 2 - 1;

        // We determine the rotation based on the tilt to the left or right
        float rotation = tilt.x * rotationSpeed * Time.deltaTime;

        // We use the normalized Z tilt value to modulate the speed of the segway.
        Vector3 movement = new Vector3(0, 0, normalizedZTilt) * speed * Time.deltaTime;

        // Apply the rotation to the VR Rig
        transform.Rotate(0, rotation, 0);

        // Apply the movement to the VR Rig
        transform.Translate(movement, Space.Self);
    }
}
