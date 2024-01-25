using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{

    private float startPosition, length;
    public Transform cam;
    // How much the background moves relative to the camera
    // 0 means the background doesn't move at all
    // 1 means the background moves at the same speed as the camera
    public float parallaxEffect;
    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float distance = (cam.transform.position.x * parallaxEffect);
        float remainder = (cam.transform.position.x * (1 - parallaxEffect));
        transform.position = new Vector3(startPosition + distance, transform.position.y, transform.position.z);

        if (remainder > startPosition + length)
        {
            startPosition += length;
        }
        else if (remainder < startPosition - length)
        {
            startPosition -= length;
        }
    }
}
