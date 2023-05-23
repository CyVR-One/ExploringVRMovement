using UnityEngine;
using UnityEngine.XR;

public class SegwayMovement : MonoBehaviour
{
    public float speed = 1.0f;
    public Transform headTransform; // Drag your VR headset transform here in the inspector.

    private Vector3 initialHeadLocalPosition;

    void Start()
    {
        // We assume that at the start, player stands upright.
        initialHeadLocalPosition = headTransform.localPosition;
    }

    void Update()
    {
        // The tilt is calculated as the current head position relative to the initial head position.
        // We only care about the X (sideways tilt) and Z (forward/backward tilt) components.
        Vector3 tilt = headTransform.localPosition - initialHeadLocalPosition;
        tilt.y = 0; 

        // Here, tilt magnitude gives an indication of how much the player is leaning.
        float tiltMagnitude = tilt.magnitude;

        // We get the direction of tilt and normalize it.
        Vector3 tiltDirection = tilt.normalized;

        // We use the tilt magnitude to modulate the speed of the segway. The more the user leans, the faster the segway moves.
        Vector3 movement = tiltDirection * tiltMagnitude * speed * Time.deltaTime;

        // Finally, we apply this movement to the segway (or VR Rig).
        transform.position += movement;
    }
}
