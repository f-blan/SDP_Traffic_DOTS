using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hello : MonoBehaviour
{
    private float speed;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = new Vector3(1,0,0);
        transform.localScale = new Vector3(3,3,3);
        speed = 0.01f;
    }

    // Update is called once per frame
    void Update()
    {
        
        if(transform.position[0] >= 4f){
            Debug.Log("switch left");
            speed = -0.01f;
        }else if (transform.position[0]<= -4f){
            Debug.Log("switch right");
            speed = 0.01f;
        }
        transform.position = transform.position + new Vector3(speed,0,0);
    }
}
