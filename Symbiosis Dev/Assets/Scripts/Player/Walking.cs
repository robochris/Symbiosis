using UnityEngine;

public class SimpleMove : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        float verticalInput = Input.GetAxis("Vertical");
        float horizontalInput = Input.GetAxis("Horizontal");
        transform.Translate(Vector3.forward * verticalInput * speed * Time.deltaTime);
        transform.Translate(Vector3.right * horizontalInput * speed * Time.deltaTime);
    }
}
