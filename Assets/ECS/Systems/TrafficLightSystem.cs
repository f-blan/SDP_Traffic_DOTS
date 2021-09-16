using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;

public class TrafficLightSystem : SystemBase
{
    private const float YELLOW_INTERVAL = 1f; //Always constant since yellow light should last pretty much the same time

    // Update is called once per frame
    protected override void OnUpdate(){

        Entities.WithAll<TrafficLightComponent>().ForEach((ref SpriteSheetAnimationComponent spriteSheetAnimationComponent, ref TrafficLightComponent trafficLightComponent, in Translation translation) => {

            if(spriteSheetAnimationComponent.frameTimer > trafficLightComponent.greenLightDuration && !trafficLightComponent.isRed){
                trafficLightComponent.isRed = true;
                spriteSheetAnimationComponent.frameTimer = 0;
                spriteSheetAnimationComponent.currentFrame = 1;
            }
            else if(spriteSheetAnimationComponent.frameTimer >= YELLOW_INTERVAL && spriteSheetAnimationComponent.currentFrame == 1){
                spriteSheetAnimationComponent.frameTimer = 0;
                spriteSheetAnimationComponent.currentFrame = 2;
            }
            else if(spriteSheetAnimationComponent.frameTimer >= trafficLightComponent.greenLightDuration + YELLOW_INTERVAL){
                spriteSheetAnimationComponent.currentFrame = 0;
                spriteSheetAnimationComponent.frameTimer = 0;
                trafficLightComponent.isRed = false;
                return;
            }
            return;

        }).ScheduleParallel();
    }

}
