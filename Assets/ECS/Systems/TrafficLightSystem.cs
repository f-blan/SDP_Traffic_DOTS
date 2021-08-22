using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;

public class TrafficLightSystem : SystemBase
{
    private const float SWITCH_INTERVAL = 4f;
    private Material HorizontalMaterial;
    private Material VerticalMaterial;
    private float timer;

    private bool horizontalIsGreen;
    // Start is called before the first frame update
    protected override void OnCreate()
    {
        HorizontalMaterial = Resources.Load<Material>("TrafficLightHorizontal");
        VerticalMaterial = Resources.Load<Material>("TrafficLightVertical");
        
        HorizontalMaterial.color=UnityEngine.Color.green;
        VerticalMaterial.color=UnityEngine.Color.red;
        timer = 0;
        horizontalIsGreen = true;
    }

    // Update is called once per frame
    protected override void OnUpdate(){
        
        timer += UnityEngine.Time.deltaTime;
        if(timer < SWITCH_INTERVAL){
            return;
        }
        
        timer = 0;
        if(horizontalIsGreen){
            HorizontalMaterial.color = UnityEngine.Color.red;
            VerticalMaterial.color = UnityEngine.Color.green;
        }else{
            HorizontalMaterial.color = UnityEngine.Color.green;
            VerticalMaterial.color = UnityEngine.Color.red;
        }

        Entities.ForEach(( Entity entity, ref TrafficLightComponent trafficLightComponent)=>{
            trafficLightComponent.isRed= !trafficLightComponent.isRed;
        }).ScheduleParallel();

        horizontalIsGreen = !horizontalIsGreen;
        
    }

}
