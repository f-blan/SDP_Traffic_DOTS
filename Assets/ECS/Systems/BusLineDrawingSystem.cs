using Unity.Entities;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

[UpdateAfter(typeof(BusMovementSystem))]
public class BusLineDrawingSystem : SystemBase
{

    EntityQueryDesc queryBusSelected = new EntityQueryDesc{All = new ComponentType[]{ComponentType.ReadOnly<SelectedVehicleTag>(), ComponentType.ReadOnly<BusPathComponent>()}};
    GameObject line;

    protected override void OnUpdate()
    {
        if(line == null){
            NativeArray<BusPathComponent> busPathComponents = GetEntityQuery(queryBusSelected).ToComponentDataArray<BusPathComponent>(Allocator.Temp);
            if(busPathComponents.Length == 0 || busPathComponents[0].pathLength == 0){
                return;
            }
            NativeArray<Vector3> worldPoints = new NativeArray<Vector3>(busPathComponents[0].pathLength+1, Allocator.Temp);

            int verse = busPathComponents[0].verse;
            // NativeList<int2> localNativeList = nativeListGraph;

            PathElement[] pathElements = busPathComponents[0].pathArrayReference.Value.pathArray.ToArray();

            for(int i = 0; i < pathElements.Length; i++){ 
                worldPoints[i] = Map_Setup.Instance.CityGraph.GetGraphNode(pathElements[i].x, pathElements[i].y).GetReferenceTile().GetWorldPosition();
                // Debug.Log("Node position: " + localNativeList[i].x + "," + localNativeList[i].y);
                // Debug.Log("World position: " + Map_Setup.Instance.CityGraph.GetGraphNode(localNativeList[i].x, localNativeList[i].y).GetReferenceTile().GetWorldPosition());
            } 
            worldPoints[pathElements.Length] = Map_Setup.Instance.CityGraph.GetGraphNode(pathElements[0].x, pathElements[0].y).GetReferenceTile().GetWorldPosition();

            line = new GameObject();
            LineRenderer localDrawLine = line.AddComponent<LineRenderer>();
            line.name = "BusLineTrace";
            localDrawLine.material = new Material(Shader.Find("Sprites/Default"));

            localDrawLine.startWidth = 0.1f;
            localDrawLine.endWidth = localDrawLine.startWidth;

            localDrawLine.startColor = (verse == -1 ? Color.red : Color.blue);
            localDrawLine.endColor = localDrawLine.startColor;

            localDrawLine.positionCount = worldPoints.Length; 
            localDrawLine.SetPositions(worldPoints.ToArray());

            busPathComponents.Dispose();
            worldPoints.Dispose();
        }
    }
}
