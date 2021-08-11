using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraUtils : MonoBehaviour
{
    private const float SCROLLSPEED = 1.0f;
    private bool naturalScroll = true;
    private const byte PRIMARYMOUSEBUTTON = 0;
    private bool dragging = false;
    private const float DRAGMULTIPLIER = 0.05f;
    private Vector3 initPos;
    

    // Start is called before the first frame update
    void Start()
    {
         
    }

    // Update is called once per frame
    void Update()
    {
        //Allows to scroll in and out the camera
        if(Input.mouseScrollDelta.y != 0){
            float valToAdd = (naturalScroll ? -1 : 1) * Mathf.Sign(Input.mouseScrollDelta.y) * SCROLLSPEED;
            Camera.main.orthographicSize += Camera.main.orthographicSize + valToAdd < 1 ? 0 : valToAdd;
        }

        if(Input.GetMouseButtonDown(PRIMARYMOUSEBUTTON)){
           dragging = true;
           initPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
           
        }
        else if(dragging){
            transform.position = new Vector3(transform.position.x - (Input.mousePosition.x - initPos.x)*DRAGMULTIPLIER, transform.position.y - (Input.mousePosition.y - initPos.y)*DRAGMULTIPLIER, transform.position.z);
            initPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        }
        if(Input.GetMouseButtonUp(PRIMARYMOUSEBUTTON)){
            dragging = false;
        }
    }
}
