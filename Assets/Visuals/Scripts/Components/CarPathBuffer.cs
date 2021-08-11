using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(20)]
public struct CarPathBuffer : IBufferElementData {

    public int x;
    public int y;
    public int cost;

}
