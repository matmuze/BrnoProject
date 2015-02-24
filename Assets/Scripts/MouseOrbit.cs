using UnityEngine;

//[ExecuteInEditMode]
public class MouseOrbit : MonoBehaviour
{
    public Vector3 target;

    public float xSpeed = 250.0f;
    public float ySpeed = 120.0f;

    public float yMinLimit = -90f;
    public float yMaxLimit = 90f;

    const float DEFAULT_DISTANCE = 5.0f;

    private float x = 0.0f;
    private float y = 0.0f;

    [Range(0, 200)]
    public float distance = 1.5f;

    void Start()
    {
        var angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }
    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
            y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            y = ClampAngle(y, yMinLimit, yMaxLimit);
        }

        if (Input.GetMouseButton(2))
        {
            target -= gameObject.transform.up * Input.GetAxis("Mouse Y") * 0.01f;
            target -= gameObject.transform.right * Input.GetAxis("Mouse X") * 0.01f;
        }

        float scale = 20;

        if (Input.GetKey(KeyCode.W))
        {
            target += gameObject.transform.forward * Time.deltaTime * scale;
        }

        if (Input.GetKey(KeyCode.A))
        {
            target -= gameObject.transform.right * Time.deltaTime * scale;
        }

        if (Input.GetKey(KeyCode.D))
        {
            target += gameObject.transform.right * Time.deltaTime * scale;
        }

        if (Input.GetKey(KeyCode.S))
        {
            target -= gameObject.transform.forward * Time.deltaTime * scale;
        }

        if (Input.GetKey(KeyCode.F))
        {
            distance = DEFAULT_DISTANCE;
        }

        distance += Input.GetAxis("Mouse ScrollWheel");

        distance = Mathf.Max(0.3f, distance);

        var rotation = Quaternion.Euler(y, x, 0.0f);
        var position = rotation * new Vector3(0.0f, 0.0f, -distance) + target;

        transform.rotation = rotation;
        transform.position = position;
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360.0f)
            angle += 360.0f;

        if (angle > 360.0f)
            angle -= 360.0f;

        return Mathf.Clamp(angle, min, max);
    }
}
