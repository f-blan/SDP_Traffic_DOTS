using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(60)]
public struct BusPathBuffer : IBufferElementData {

    public int x;
    public int y;

    //everything else is int2 because each node can be traversed with 2 verses. If verse == -1 consider the .x field, otherwise the .y field
    //this is assuming i can associate the same dynamic buffer to two different entities (the two buses going through the path with different verses)
    //However i'm not sure if it can be done so this file might be subject to change 
    
    public int2 cost;
    public int2 withDirection;
    //-1 if the node is not a bus stop node, cost-3 otherwise for now
    public int2 costToStop;
}

