using Unity.Entities;

public struct CarPathComponent : IComponentData
{
    public int pathIndex;

    //0 = up, 1 = right, 2 = down, 3 = left
    public int direction;
}

