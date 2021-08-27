using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

// [AlwaysSynchronizeSystem] Unsure if this should be here
[UpdateAfter(typeof(CarPathSystem))]
public class VehicleMovementSystem : SystemBase
{
    // private Map<MapTile> map;
    // private float tileSize;
    // private float3 originPosition;
    private NativeArray<PathUtils.PathNode> graphArray;
    private bool isGraphValid;
    private EndSimulationEntityCommandBufferSystem esEcbs;

    private const float ParkingTime = 5f;

    protected override void OnCreate()
    {
        base.OnCreate();

        isGraphValid=false;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if(isGraphValid){
            graphArray.Dispose();
        }
    }
    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        esEcbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate(){

        float dt = Time.DeltaTime;

        EntityCommandBuffer.ParallelWriter ecbpw = esEcbs.CreateCommandBuffer().AsParallelWriter();

        int graphWidth = Map_Setup.Instance.CityGraph.GetWidth();
        if(!isGraphValid){
            isGraphValid=true;
            graphArray = PathUtils.GetPathNodeArray();
        }
        NativeArray<PathUtils.PathNode> localGraphArray = graphArray;

        //For debugging, add WithoutBurst() in the chain and do Debug.Log
        Entities.WithAll<CarPathBuffer>().ForEach((int entityInQueryIndex, Entity entity, ref Translation translation, ref VehicleMovementData vehicleMovementData, ref DynamicBuffer<CarPathBuffer> carPathBuffer, ref Rotation rotation) => {

            CarPathBuffer lastCarPathBuffer; //Temporary variable for accessing the currently used carPathBuffer

            // //Debug
            // Camera.main.transform.position = new Vector3(translation.Value.x, translation.Value.y, Camera.main.transform.position.z);
            // //End Debug

            if(vehicleMovementData.state == 4){
                ecbpw.RemoveComponent<CarPathBuffer>(entityInQueryIndex, entity);
                ResumeRunning(localGraphArray,ref translation, ref vehicleMovementData, ecbpw,entity,entityInQueryIndex, graphWidth);
                return;
            }
            //state 3 is handled by quadrant system
            if(vehicleMovementData.state == 3){
                return;
            }


            if(vehicleMovementData.state == 2){
                //car is parked
                vehicleMovementData.parkingTimer += dt;
                if(vehicleMovementData.parkingTimer >= ParkingTime){
                    vehicleMovementData.parkingTimer = 0;
                    vehicleMovementData.state = 3;
                    
                }
                return;
            }

            if(vehicleMovementData.state==1){
                ParkingHandler(ref translation, ref vehicleMovementData, ref rotation, localGraphArray, graphWidth, dt);
                return;
            }

            //everything below is state 0 (car following a path)
            lastCarPathBuffer = carPathBuffer[carPathBuffer.Length - 1]; //Path list is inverted
            //If not yet initialized, then, initialize
            if(float.IsNaN(vehicleMovementData.initialPosition.x)){
                vehicleMovementData.initialPosition.x = translation.Value.x; //Set initial position to the starting position of the vehicle
                vehicleMovementData.initialPosition.y = translation.Value.y;

                vehicleMovementData.offset = CarUtils.ComputeOffset(lastCarPathBuffer.cost, vehicleMovementData.direction, -1, carPathBuffer[carPathBuffer.Length - 2].withDirection); //Compoutes offset
                vehicleMovementData.stop = false;
                
            }
            if(vehicleMovementData.stop == true){
                return;
            }
            translation.Value.x += vehicleMovementData.velocity.x * dt;
            translation.Value.y += vehicleMovementData.velocity.y * dt;
            
            //Checks whether or not the vehicle has already reached its temporary destination
            bool checker = CarUtils.ComputeReachedDestination(vehicleMovementData.direction, vehicleMovementData.initialPosition, vehicleMovementData.offset, translation.Value);

            if(checker){
                //Removing last element from the buffer

                int index = PathUtils.CalculateIndex(lastCarPathBuffer.x, lastCarPathBuffer.y, graphWidth);
                vehicleMovementData.curGraphIndex = index;
            
                carPathBuffer.RemoveAt(carPathBuffer.Length - 1);
                

                if(carPathBuffer.IsEmpty){
                    
                    vehicleMovementData.startGraphIndex = index;
                    //function call to setup a random direction to look for parking
                    ParkingTurn( ref vehicleMovementData, ref translation, ref rotation, localGraphArray, index, graphWidth, 1);
                    return;
                }

                //U-turn logic, missing animations
                //ComputeUTurn return an offset that moves one space either x or y depending on where the u turn is being performed
                float2 uTurnOffset = CarUtils.ComputeUTurn(vehicleMovementData.direction, carPathBuffer[carPathBuffer.Length - 1].withDirection);

                // Debug 
                // Debug.Log("Initial Position: " + vehicleMovementData.initialPosition);
                // Debug.Log("U turn offset: " + uTurnOffset);
                // Debug.Log("Vehicle movement data offset: " + vehicleMovementData.offset);
                // End debug

                //Update initial position
                vehicleMovementData.initialPosition.x += vehicleMovementData.offset.x + uTurnOffset.x;
                vehicleMovementData.initialPosition.y += vehicleMovementData.offset.y + uTurnOffset.y;

                //Update translation position in case it goes out of bounds (Systems that have vehicles with very large speeds)
                translation.Value.x = vehicleMovementData.initialPosition.x;
                translation.Value.y = vehicleMovementData.initialPosition.y;

                //If there are enough CarPathBuffers remaining, then, compute new offset
                if(carPathBuffer.Length >= 2){
                    vehicleMovementData.offset = CarUtils.ComputeOffset(carPathBuffer[carPathBuffer.Length - 1].cost, carPathBuffer[carPathBuffer.Length - 1].withDirection, vehicleMovementData.direction, carPathBuffer[carPathBuffer.Length - 2].withDirection);
                }
                else{
                    
                    int nextIndex = PathUtils.CalculateIndex(carPathBuffer[carPathBuffer.Length - 1].x, carPathBuffer[carPathBuffer.Length - 1].y, graphWidth);
                    
                    Unity.Mathematics.Random r = new Unity.Mathematics.Random((uint) nextIndex+1);
                    int nextDirection;
                    do{
                        nextDirection = r.NextInt(0, 3); 
                    }while(localGraphArray[nextIndex].goesTo[nextDirection] == -1);
                    
                    vehicleMovementData.offset = CarUtils.ComputeOffset(carPathBuffer[carPathBuffer.Length - 1].cost, carPathBuffer[carPathBuffer.Length - 1].withDirection, vehicleMovementData.direction, nextDirection);
                    vehicleMovementData.targetDirection=nextDirection;
                }

                lastCarPathBuffer = carPathBuffer[carPathBuffer.Length - 1];

                //Update velocity
                vehicleMovementData.velocity = CarUtils.ComputeVelocity(vehicleMovementData.speed, lastCarPathBuffer.withDirection);
                //Update direction
                vehicleMovementData.direction = lastCarPathBuffer.withDirection;
                //Update rotation
                rotation.Value = Quaternion.Euler(0f,0f,CarUtils.ComputeRotation(lastCarPathBuffer.withDirection));
            }
        }).WithReadOnly(localGraphArray).ScheduleParallel();


        Entities.WithAll<BusPathComponent>().ForEach((Entity entity) => {
            
            

            return;
        }).ScheduleParallel();

        esEcbs.AddJobHandleForProducer(this.Dependency);

        return;
    }
    private static void ParkingTurn(ref VehicleMovementData vehicleMovementData, ref Translation translation, ref Rotation rotation, NativeArray<PathUtils.PathNode> graphArray, int index, int graphWidth, int seedHelp){
        
        PathUtils.PathNode p = graphArray[index];
        int targetDirection = vehicleMovementData.targetDirection;

        NativeArray<int2> dirOffset = new NativeArray<int2>(4, Allocator.Temp);
        dirOffset[0] = new int2(0, +1);
        dirOffset[1] = new int2(+1, 0);
        dirOffset[2] = new int2(0, -1);
        dirOffset[3] = new int2(-1, 0);

        int curGraphIndex = PathUtils.CalculateIndex(p.x + dirOffset[vehicleMovementData.targetDirection].x, p.y + dirOffset[vehicleMovementData.targetDirection].y, graphWidth);
        vehicleMovementData.curGraphIndex = curGraphIndex;
        
        int seed = index + seedHelp+1 + vehicleMovementData.startGraphIndex; 
        
        Unity.Mathematics.Random r = new Unity.Mathematics.Random((uint) seed);
        int nextDirection=targetDirection;

        NativeList<int> availableDirections = new NativeList<int>(Allocator.Temp);

        for(int t=0; t<4; ++t){
            if(graphArray[curGraphIndex].goesTo[t] !=-1){
                availableDirections.Add(t);
            }
        }
        
        nextDirection = availableDirections[r.NextInt(0, availableDirections.Length)];
        availableDirections.Dispose();

        vehicleMovementData.targetDirection = nextDirection;
        
        vehicleMovementData.state = 1;

        float2 uTurnOffset = CarUtils.ComputeUTurn(vehicleMovementData.direction, targetDirection);
        //Update initial position
        vehicleMovementData.initialPosition.x += vehicleMovementData.offset.x + uTurnOffset.x;
        vehicleMovementData.initialPosition.y += vehicleMovementData.offset.y + uTurnOffset.y;

        //Update translation position in case it goes out of bounds (Systems that have vehicles with very large speeds)
        translation.Value.x = vehicleMovementData.initialPosition.x;
        translation.Value.y = vehicleMovementData.initialPosition.y;

        //calculate offset
        vehicleMovementData.offset = CarUtils.ComputeOffset(p.goesTo[targetDirection], targetDirection, vehicleMovementData.direction, nextDirection);
         
         //Update velocity
        vehicleMovementData.velocity = CarUtils.ComputeVelocity(vehicleMovementData.speed, targetDirection);
        //Update direction
        vehicleMovementData.direction = targetDirection;
        //Update rotation
        rotation.Value = Quaternion.Euler(0f,0f,CarUtils.ComputeRotation(targetDirection));
    }

    private static void ParkingHandler( ref Translation translation, ref VehicleMovementData vehicleMovementData, ref Rotation rotation, NativeArray<PathUtils.PathNode> graphArray, int graphWidth, float dt){
        if(vehicleMovementData.stop == true){
                return;
        }
        
        translation.Value.x += vehicleMovementData.velocity.x * dt;
        translation.Value.y += vehicleMovementData.velocity.y * dt;
            
        //Checks whether or not the vehicle has already reached its temporary destination
        bool checker = CarUtils.ComputeReachedDestination(vehicleMovementData.direction, vehicleMovementData.initialPosition, vehicleMovementData.offset, translation.Value);

        //decide where to turn at next intersection (you already know where you're going to turn now) and compute offset
        
        if(checker){
            int seedHelp = (int) math.floor(dt*100000f);
            
            ParkingTurn(ref vehicleMovementData, ref translation,ref rotation, graphArray, vehicleMovementData.curGraphIndex, graphWidth, (int) math.floor(dt*10000000));
            return;
        }

    }

    private static void ResumeRunning(NativeArray<PathUtils.PathNode> graphArray,ref Translation translation, ref VehicleMovementData vehicleMovementData, EntityCommandBuffer.ParallelWriter ecb, Entity entity, int eqi, int graphWidth){
        
        PathUtils.PathNode startNode = graphArray[vehicleMovementData.curGraphIndex];
        int offsetIndex = (vehicleMovementData.direction +3)%4;
        int prevNodeIndex = PathUtils.CalculatePrevNodeIndex(startNode.x, startNode.y,vehicleMovementData.direction,graphWidth);//(startNode.x + translationMoveOffset[offsetIndex].x, startNode.y + translationMoveOffset[offsetIndex].y, graphWidth);
        PathUtils.PathNode prevNode = graphArray[prevNodeIndex];

        int leftoverCost = CarUtils.GetLeftoverCost(vehicleMovementData.direction, vehicleMovementData.initialPosition,prevNode.goesTo[vehicleMovementData.direction] , translation.Value);
        

        //find another endNode for the pathFind
        int seed = eqi+1;
        Unity.Mathematics.Random r = new Unity.Mathematics.Random((uint) seed);
        
        int endNodeIndex;
        do{
            endNodeIndex = r.NextInt(0, graphArray.Length-1);
        }while(endNodeIndex == vehicleMovementData.curGraphIndex);

        PathUtils.PathNode endNode = graphArray[endNodeIndex];

        //add the component that triggers path computation
        ecb.AddComponent<CarPathParams>(eqi, entity, new CarPathParams{direction = vehicleMovementData.direction, 
            init_cost = leftoverCost, 
            startPosition = new int2(startNode.x, startNode.y),
            endPosition = new int2(endNode.x, endNode.y)
        });

        //set initial position so that it gets initalized after pathfing
        vehicleMovementData.initialPosition.x = float.NaN;
        vehicleMovementData.initialPosition.y = float.NaN;
        vehicleMovementData.state = 0;
    }
}