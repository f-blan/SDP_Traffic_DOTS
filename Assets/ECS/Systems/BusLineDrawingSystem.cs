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
        //Do not do anything if the line object already exists
        if(line == null){
            //Get query defined above
            NativeArray<BusPathComponent> busPathComponents = GetEntityQuery(queryBusSelected).ToComponentDataArray<BusPathComponent>(Allocator.Temp);
            //Don't do anything if nothing was returned or if the bus has no path
            if(busPathComponents.Length == 0 || busPathComponents[0].pathLength == 0){
                return;
            }
            //Creating an array in which the world points will be stored, given the information obtained from the query above;
            NativeArray<Vector3> worldPoints = new NativeArray<Vector3>(busPathComponents[0].pathLength+1, Allocator.Temp);
            //Getting array of path elements
            PathElement[] pathElements = busPathComponents[0].pathArrayReference.Value.pathArray.ToArray();
            //Mapping nodes to world points
            for(int i = 0; i < pathElements.Length; i++){ 
                worldPoints[i] = Map_Setup.Instance.CityGraph.GetGraphNode(pathElements[i].x, pathElements[i].y).GetReferenceTile().GetWorldPosition();
            } 
            worldPoints[pathElements.Length] = Map_Setup.Instance.CityGraph.GetGraphNode(pathElements[0].x, pathElements[0].y).GetReferenceTile().GetWorldPosition();

            //Drawing line code
            line = new GameObject(); //Creating a blank game object
            LineRenderer localDrawLine = line.AddComponent<LineRenderer>(); //Add line renderer component to the game object
            line.name = "BusLineTrace"; //Setting name
            localDrawLine.material = new Material(Shader.Find("Sprites/Default")); //Adding shader

            //Setting line width
            localDrawLine.startWidth = 0.1f;
            localDrawLine.endWidth = localDrawLine.startWidth;
            //Setting color
            localDrawLine.startColor = (busPathComponents[0].verse == -1 ? Color.red : Color.blue);
            localDrawLine.endColor = localDrawLine.startColor;
            //Setting amount of points and adding the points
            localDrawLine.positionCount = worldPoints.Length; 
            localDrawLine.SetPositions(worldPoints.ToArray());

            //Disposing of arrays
            busPathComponents.Dispose();
            worldPoints.Dispose();
        }
    }
}
