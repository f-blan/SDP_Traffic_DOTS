using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class CameraUtils : MonoBehaviour
{

    struct PositionVehicleCameraJob: IJobParallelFor
    {
        [ReadOnly,NativeDisableParallelForRestriction]
        public NativeArray<Translation> vehicleTranslations;
        [ReadOnly]
        public Vector3 worldClickPosition;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> vehicleIndex;
        
        public void Execute(int i)
        {
            Translation translation = vehicleTranslations[i];

            if(System.Math.Truncate(translation.Value.x) == System.Math.Truncate(worldClickPosition.x) && System.Math.Truncate(translation.Value.y) == System.Math.Truncate(worldClickPosition.y)){
                vehicleIndex[0] = i;
            }
        }
    }

    private const int INNERLOOP_BATCH_COUNT = 64;
    private const byte PRIMARY_MOUSE_BUTTON = 0;
    private const byte SECONDARY_MOUSE_BUTTON = 1;
    private Translation cameraFollowsVehicleTranslation = new Translation{Value = new float3(0,0,-5)};
    private bool naturalScroll = true;
    private bool dragging = false;
    private Vector3 initPos;
    private EntityQuery query;
    private int cameraFollowsVehicleTranslationIndex = -1;
    private Entity followEntity;
    [SerializeField] private float scrollSpeed = 1.0f;
    [SerializeField] private float dragMultiplier = 0.04f;
    void Start(){
        query = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<VehicleMovementData>(), ComponentType.ReadOnly<Translation>()); //Could need an update once different vehicle types are added
    }

    // Update is called once per frame
    void Update()
    {

        //Allows to scroll in and out the camera using the mouse wheel
        ScrollingInAndOutUsingMouseWheel();

        //Follows the vehicle that is clicked
        FollowSelectedVehicle();

        //Allows to drag the camera around the map
        DragCameraAroundMap();
        
    } 

    private void ScrollingInAndOutUsingMouseWheel(){
        if(Input.mouseScrollDelta.y != 0){
            float valToAdd = (naturalScroll ? -1 : 1) * Mathf.Sign(Input.mouseScrollDelta.y) * scrollSpeed;

            Camera.main.orthographicSize += Camera.main.orthographicSize + valToAdd < 1 ? 0 : valToAdd;
        }
    }

    private void DragCameraAroundMap(){
        //Check if the mouse button was pressed
        if(Input.GetMouseButtonDown(PRIMARY_MOUSE_BUTTON)){
           dragging = true;
           initPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        else if(dragging){
            Vector3 curPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(transform.position.x - (curPosition.x - initPos.x)*dragMultiplier, transform.position.y - (curPosition.y - initPos.y)*dragMultiplier, transform.position.z);
            initPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
        if(Input.GetMouseButtonUp(PRIMARY_MOUSE_BUTTON)){
            dragging = false;
        }
    }

    private void FollowSelectedVehicle(){
        //Unlock the camera from following the vehicle by resetting the index

        if(Input.GetMouseButton(SECONDARY_MOUSE_BUTTON) && (cameraFollowsVehicleTranslationIndex != -1 || followEntity != Entity.Null)){
            cameraFollowsVehicleTranslationIndex = -1;
            World.DefaultGameObjectInjectionWorld.EntityManager.RemoveComponent<SelectedVehicleTag>(followEntity);
            var line = GameObject.Find("BusLineTrace");
            if(line != null){
                Destroy(line);
            }
            followEntity = Entity.Null;
            return;
        }

        if(cameraFollowsVehicleTranslationIndex != -1 || followEntity != Entity.Null){            
            Vector3 tmpPosition = World.DefaultGameObjectInjectionWorld.EntityManager.GetComponentData<Translation>(followEntity).Value;
            tmpPosition.z = -10;
            transform.position = tmpPosition; 
            return;
        }

        if(Input.GetMouseButtonDown(PRIMARY_MOUSE_BUTTON)){
            //Code repetition is necessary due to 
            NativeArray<Translation> vehicleTranslations = query.ToComponentDataArray<Translation>(Allocator.TempJob);
            NativeArray<Entity> vehicleEntities = query.ToEntityArray(Allocator.Persistent);
            NativeArray<int> vehicleIndex = new NativeArray<int>(1,Allocator.TempJob);
            vehicleIndex[0] = -1;

            var job = new PositionVehicleCameraJob()
            {
                vehicleIndex = vehicleIndex,
                worldClickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition),
                vehicleTranslations = vehicleTranslations
            };
            
            //Scheduling of the job and waiting until it has completed
            JobHandle jobHandle = job.Schedule(vehicleTranslations.Length, INNERLOOP_BATCH_COUNT);
            jobHandle.Complete();

            if(vehicleIndex[0] != -1){
                cameraFollowsVehicleTranslationIndex = vehicleIndex[0];
                cameraFollowsVehicleTranslation = vehicleTranslations[vehicleIndex[0]];
                followEntity = vehicleEntities[vehicleIndex[0]];
                transform.position = new Vector3(cameraFollowsVehicleTranslation.Value.x, cameraFollowsVehicleTranslation.Value.y, transform.position.z);
                if(World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<BusPathComponent>(followEntity)){
                    World.DefaultGameObjectInjectionWorld.EntityManager.AddComponent<SelectedVehicleTag>(followEntity);
                }
            }
            //Dispose of Nativearrays
            vehicleTranslations.Dispose();
            vehicleEntities.Dispose();
            vehicleIndex.Dispose();
        }

        
    }
}
