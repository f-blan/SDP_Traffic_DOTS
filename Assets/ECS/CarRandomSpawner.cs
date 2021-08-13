using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class CarRandomSpawner : MonoBehaviour
{
    [SerializeField] public Material unitMaterial; //Material -> sprite
    [SerializeField] public int minNumber; //Should be a positive number
    [SerializeField] public int maxNumber; //Should be a positive number

    private 

    EntityManager em;

    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        Spawner();
    }

    //Creates a custom mesh so that the dimension aspect of the sprite is kept
    private Mesh CreateMesh(float width, float height){

        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        float halfWidth = width/2f;
        float halfHeight = height/2f;

        vertices[0] = new Vector3(-halfWidth, -halfHeight);
        vertices[1] = new Vector3(-halfWidth, +halfHeight);
        vertices[2] = new Vector3(+halfWidth, +halfHeight);
        vertices[3] = new Vector3(+halfWidth, -halfHeight);

        uv[0] = new Vector2(0,0);
        uv[1] = new Vector2(0,1);
        uv[2] = new Vector2(1,1);
        uv[3] = new Vector2(1,0);

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;

        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.name = "Car Mesh";

        return mesh;
    }

    private void Spawner(){
        //Rolls a random number
        int random = UnityEngine.Random.Range((int) minNumber, (int) maxNumber);
        //Creates a Car archetype.
        EntityArchetype carArchetype = em.CreateArchetype(typeof(Translation),typeof(RenderMesh),typeof(RenderBounds),typeof(LocalToWorld), typeof(CarPathComponent), typeof(CarPathBuffer));
        //Creates an array in which the entities will be returned.
        NativeArray<Entity> entities = new NativeArray<Entity>(random, Allocator.Temp); //TODO: Check what this allocator changes
        //Creates the entities following the archetype and it sotres them in the array.
        em.CreateEntity(carArchetype, entities);
        //Needs a custom mesh in order to keep the texture's aspect ratio
        Mesh mesh = CreateMesh(0.47f, 1f);

        foreach(Entity e in entities){

            //TODO: Needs to put the vehicle in its starting position
            // em.SetComponentData(e, new Translation{
            //     Value = new float3()
            // })

            //Sets the material and the mesh for the given entity
            em.SetSharedComponentData(e, new RenderMesh{
                material = unitMaterial,
                mesh = mesh
            });
        }
    }
}

