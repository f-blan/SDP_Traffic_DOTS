using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;

public class TrafficLightSystem : SystemBase
{
    private const float YELLOW_INTERVAL = 1f; //Always constant since yellow light should last pretty much the same time
    private const float offset = 0.3f;
    // Update is called once per frame

    
    protected override void OnUpdate(){
        float deltaTime = Time.DeltaTime;
        Entities.WithAll<TrafficLightComponent>().ForEach(( ref TrafficLightComponent trafficLightComponent, ref Translation translation) => {
            
            trafficLightComponent.timer += deltaTime;

            if(trafficLightComponent.timer > trafficLightComponent.greenLightDuration && trafficLightComponent.state ==2){
                trafficLightComponent.isRed = true;
                trafficLightComponent.timer = 0;
                trafficLightComponent.state = 1;
                translation.Value = trafficLightComponent.baseTranslation;
            }
            else if(trafficLightComponent.timer >= YELLOW_INTERVAL && trafficLightComponent.state == 1){
                trafficLightComponent.timer = 0;
                trafficLightComponent.state = 0;
                translation.Value.y = trafficLightComponent.baseTranslation.y + offset;
            }
            else if(trafficLightComponent.timer >= trafficLightComponent.greenLightDuration + YELLOW_INTERVAL && trafficLightComponent.state == 0){
                trafficLightComponent.timer = 0;
                trafficLightComponent.isRed = false;
                translation.Value.y = trafficLightComponent.baseTranslation.y-offset;
                trafficLightComponent.state = 2;
                return;
            }
            return;

        }).ScheduleParallel();
    }

}
