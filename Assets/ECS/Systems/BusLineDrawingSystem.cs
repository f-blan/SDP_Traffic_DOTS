using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

[UpdateAfter(typeof(BusMovementSystem))]
public class BusLineDrawingSystem : SystemBase
{
    private NativeList<int2> nativeListGraph;
    private NativeList<Vector3> nativeListWorldPoint;
    private bool set;

    GameObject line;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        nativeListGraph = new NativeList<int2>(Allocator.Persistent);
        set = false;
    }

    protected override void OnUpdate()
    {
        if(!set){
            NativeList<int2> localNativeList = nativeListGraph;
            int verse = 1;
            Entities.WithAll<BusPathComponent,SelectedVehicleTag>().ForEach((in BusPathComponent busPathComponent, in VehicleMovementData vehicleMovementData) => {
                verse = busPathComponent.verse;
                if(localNativeList.Length == 0){
                    ref BlobArray<PathElement> arrayPath = ref busPathComponent.pathArrayReference.Value.pathArray;
                    for(int i = 0; i < busPathComponent.pathLength; i++){
                        localNativeList.Add(new int2(arrayPath[i].x, arrayPath[i].y));//Getting node position
                    } 
                    localNativeList.Add(new int2(arrayPath[0].x, arrayPath[0].y));
                } 
                // Debug.Log(localNativeList.Length);
            }).Run(); //Can't be done in parallel, check how much does it affect performance
            if(localNativeList.Length == 0){
                return;
            }

            nativeListWorldPoint = new NativeList<Vector3>(Allocator.Temp);

            for(int i = 0; i < localNativeList.Length; i++){ 
                nativeListWorldPoint.Add(Map_Setup.Instance.CityGraph.GetGraphNode(localNativeList[i].x, localNativeList[i].y).GetReferenceTile().GetWorldPosition());
                // Debug.Log("Node position: " + localNativeList[i].x + "," + localNativeList[i].y);
                // Debug.Log("World position: " + Map_Setup.Instance.CityGraph.GetGraphNode(localNativeList[i].x, localNativeList[i].y).GetReferenceTile().GetWorldPosition());
            } 

            line = new GameObject();
            LineRenderer localDrawLine = line.AddComponent<LineRenderer>();
            line.name = "BusLineTrace";
            localDrawLine.material = new Material(Shader.Find("Sprites/Default"));

            localDrawLine.startWidth = 0.1f;
            localDrawLine.endWidth = localDrawLine.startWidth;

            localDrawLine.startColor = (verse == -1 ? Color.red : Color.blue);
            localDrawLine.endColor = localDrawLine.startColor;

            localDrawLine.positionCount = localNativeList.Length; 
            localDrawLine.SetPositions(nativeListWorldPoint.ToArray());

            nativeListWorldPoint.Dispose();
            set = true;
        }
    }


    protected override void OnDestroy()
    {
        base.OnDestroy();
        nativeListGraph.Dispose();
    }

}
