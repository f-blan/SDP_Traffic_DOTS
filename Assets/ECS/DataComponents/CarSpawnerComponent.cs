using Unity.Entities;
using UnityEngine;

public struct CarSpawnerComponent : IComponentData
{
    //number of cars to spawn in this district
    public int n_cars;
    //spawn delay ATTENTION! this has to have a small difference wrt nearby districts,
    //since i'm not performing any check on whether the tile in which we spawn is already occupied.
    //If a district spawns too early wtr to a nearby one it may happen that its cars will spawn where one
    //car is already at
    public float delay;
    public float timer;

    //district coordinates
    public int d_x;
    public int d_y;
    public Entity entityToSpawn;
    
    public Color color;
}
