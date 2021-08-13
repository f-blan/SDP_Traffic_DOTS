using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraUtils : MonoBehaviour
{
    private const byte PRIMARYMOUSEBUTTON = 0;
    private bool naturalScroll = true;
    private bool dragging = false;
    private Vector3 initPos;
    [SerializeField] private float scrollSpeed = 1.0f;
    [SerializeField] private float dragMultiplier = 0.04f;

    // Update is called once per frame
    void Update()
    {
        //Allows to scroll in and out the camera
        if(Input.mouseScrollDelta.y != 0){
            float valToAdd = (naturalScroll ? -1 : 1) * Mathf.Sign(Input.mouseScrollDelta.y) * scrollSpeed;
            //If the cmaer
            Camera.main.orthographicSize += Camera.main.orthographicSize + valToAdd < 1 ? 0 : valToAdd;
        }

        if(Input.GetMouseButtonDown(PRIMARYMOUSEBUTTON)){
           dragging = true;
           initPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        }
        else if(dragging){
            transform.position = new Vector3(transform.position.x - (Input.mousePosition.x - initPos.x)*dragMultiplier, transform.position.y - (Input.mousePosition.y - initPos.y)*dragMultiplier, transform.position.z);
            initPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        }
        if(Input.GetMouseButtonUp(PRIMARYMOUSEBUTTON)){
            dragging = false;
        }
    }
}
