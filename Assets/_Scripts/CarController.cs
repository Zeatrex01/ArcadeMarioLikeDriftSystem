using UnityEngine;

public class CarController : MonoBehaviour {

    public float speed = 10f;
    public float turnSpeed = 100f;

    void Update() {
        
        // Simple movement
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        
        // Move forward/backward
        transform.Translate(Vector3.forward * vertical * speed * Time.deltaTime);
        
        // Turn left/right
        transform.Rotate(Vector3.up * horizontal * turnSpeed * Time.deltaTime);
    }
}