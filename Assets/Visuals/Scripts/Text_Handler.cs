using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Text_Handler : MonoBehaviour
{
    [SerializeField] public Text EntitiesText;
    [SerializeField] public Text TimeText;

    private float timer;

    private int seconds;
    private int prevEntities;
    // Start is called before the first frame update
    void Start()
    {
        timer = 0f;
        seconds = 0;
        TimeText.text = "Time: " + seconds + "s";
        prevEntities=0;
    }

    // Update is called once per frame
    void Update()
    {
        timer += UnityEngine.Time.deltaTime;
        if(timer >= 1f){ 
            seconds++;
            timer = 0;
            TimeText.text = "Time: " + seconds + "s";
        }

        if(Map_Setup.Instance.runningEntities >= prevEntities){
            EntitiesText.text = "Entities: " + Map_Setup.Instance.runningEntities;
            prevEntities = Map_Setup.Instance.runningEntities;
        }
    }
}
