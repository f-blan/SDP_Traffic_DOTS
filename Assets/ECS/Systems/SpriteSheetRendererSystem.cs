using Unity.Entities;
using Unity.Transforms;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class SpriteSheetRendererSystem : SystemBase
{
    protected override void OnUpdate(){

        float deltaTime = Time.DeltaTime;

        Entities.WithAll<SpriteSheetAnimationComponent>().ForEach((ref SpriteSheetAnimationComponent spriteSheetAnimationComponent, in Translation translation) => {

            spriteSheetAnimationComponent.frameTimer += deltaTime;

            float uvWidth = 1f / spriteSheetAnimationComponent.frameCount;
            float uvHeight = 1f;
            float uvOffsetX = uvWidth * spriteSheetAnimationComponent.currentFrame;
            float uvOffsetY = 0f;
            spriteSheetAnimationComponent.uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);

            spriteSheetAnimationComponent.matrix = Matrix4x4.TRS(translation.Value, Quaternion.identity, Vector3.one);
        
        }).ScheduleParallel();

        EntityQuery entityQuery = GetEntityQuery(typeof(SpriteSheetAnimationComponent));
        NativeArray<SpriteSheetAnimationComponent> animationDataArray = entityQuery.ToComponentDataArray<SpriteSheetAnimationComponent>(Allocator.Temp);

        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        Camera cameraMain = Camera.main; 
        Vector4[] uv = new Vector4[1];
        Mesh quadMesh = Map_Visual.instance.mesh;
        Material material = Map_Visual.instance.trafficLightSpriteSheet; 
        int shaderPropertyId = Shader.PropertyToID("_MainTex_UV");

        int sliceCount = 1023;

        for(int i = 0; i < animationDataArray.Length; i+=sliceCount){

            int sliceSize = math.min(animationDataArray.Length - i, sliceCount);
            List<Matrix4x4> matrixList = new List<Matrix4x4>();
            List<Vector4> uvList = new List<Vector4>();
            for(int j = 0; j < sliceSize ; j++){
                matrixList.Add(animationDataArray[i+j].matrix);
                uvList.Add(animationDataArray[i+j].uv);
            }
            materialPropertyBlock.SetVectorArray(shaderPropertyId,uvList);

            Graphics.DrawMeshInstanced(quadMesh, 0, material, matrixList, materialPropertyBlock); 
        }
    }
}
