using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;

public class TrafficLightSystem : SystemBase
{
    private const float GREEN_INTERVAL = 5f;
    private const float YELLOW_INTERVAL = 1f;
    //private const float RED_INTERVAL = 2f;
    private Material HorizontalMaterial;
    private Material VerticalMaterial;
    private float timer;

    private bool yellow_switched;

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
        yellow_switched = false;
    }

    // Update is called once per frame
    protected override void OnUpdate(){
        
        bool localHorizontalIsGreen = horizontalIsGreen;
        timer += UnityEngine.Time.deltaTime;
        if(timer < GREEN_INTERVAL){
            return;
        }

        if(timer >= GREEN_INTERVAL && timer <= GREEN_INTERVAL+YELLOW_INTERVAL){
            if(yellow_switched) return;

            yellow_switched = true;
            if(horizontalIsGreen){
                HorizontalMaterial.color = UnityEngine.Color.yellow;
            }else{
                VerticalMaterial.color = UnityEngine.Color.yellow;
            }
            Entities.ForEach(( Entity entity, ref TrafficLightComponent trafficLightComponent)=>{
                if(trafficLightComponent.isVertical != localHorizontalIsGreen)
                    trafficLightComponent.isRed= true;
            }).ScheduleParallel();
            return;
        }
        
        timer = 0;
        yellow_switched = false;
        if(horizontalIsGreen){
            HorizontalMaterial.color = UnityEngine.Color.red;
            VerticalMaterial.color = UnityEngine.Color.green;
        }else{
            HorizontalMaterial.color = UnityEngine.Color.green;
            VerticalMaterial.color = UnityEngine.Color.red;
        }

        Entities.ForEach(( Entity entity, ref TrafficLightComponent trafficLightComponent)=>{
            if(trafficLightComponent.isVertical == localHorizontalIsGreen)
            trafficLightComponent.isRed= false;
        }).ScheduleParallel();

        horizontalIsGreen = !horizontalIsGreen;
        
    }

}
