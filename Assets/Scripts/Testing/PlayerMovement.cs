using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float movementSpeed = 2f;
    [SerializeField]
    private float turnSpeed = 180f;

    private void Update()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            movement += transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement -= transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement += transform.right;
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement -= transform.right;
        }
        transform.position += movement.normalized * movementSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
        }
    }
}
