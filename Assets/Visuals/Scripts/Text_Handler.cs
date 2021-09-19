using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Text_Handler : MonoBehaviour
{
    [SerializeField] public Text EntitiesText;
    [SerializeField] public Text TimeText;
    [SerializeField] public Text CrowdednessText;
    private float timer;
    private float max_entities;
    private int seconds;
    private int prevEntities;
    // Start is called before the first frame update
    void Start()
    {
        timer = 0f;
        seconds = 0;
        TimeText.text = "Time: " + seconds + "s";
        prevEntities=0;

        int n_districts = Map_Setup.Instance.map_n_districts_x*Map_Setup.Instance.map_n_districts_y;
        float sum_freqs = Map_Setup.Instance.Frequency_District_0+Map_Setup.Instance.Frequency_District_1+Map_Setup.Instance.Frequency_District_2+Map_Setup.Instance.Frequency_District_3;

        max_entities = 126*n_districts*(Map_Setup.Instance.Frequency_District_0/sum_freqs) 
                    + 126*n_districts*(Map_Setup.Instance.Frequency_District_1/sum_freqs)
                    + 136*n_districts*(Map_Setup.Instance.Frequency_District_2/sum_freqs)
                    + 146*n_districts*(Map_Setup.Instance.Frequency_District_3/sum_freqs);
        
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
            EntitiesText.text = "Entities: " + (Map_Setup.Instance.runningEntities-1);
            prevEntities = Map_Setup.Instance.runningEntities;
            float crowd = (float) (Map_Setup.Instance.runningEntities-1)/ max_entities;
            crowd = crowd*100;
            CrowdednessText.text = "Crowdedness: " + crowd.ToString("0.0") + "%"; 
        }
        
    }
}
