using Unity.Entities;

public struct BusPathComponent : IComponentData
{
    public int pathIndex;

    //0 = up, 1 = right, 2 = down, 3 = left
    public int direction;
    

    //might be unnecessary since probably this can be accessed through pathArrayReference
    public int pathLength;
    //as in normal bus lines, i'm spawning two buses for each bus path. Path is circular (buses never stop, since they don't park)
    //so path[0] can be reached after path[pathLength] and vice versa
    //verse == -1 ---> proper verse
    //verse == 1 ----> backward verse
    public int verse;

     public BlobAssetReference<BusPathBlobArray> pathArrayReference;
    
}
