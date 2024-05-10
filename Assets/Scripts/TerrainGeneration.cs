using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    
    [SerializeField] private float chunkSize=50f;
    [SerializeField] private float resolution = 0.5f;

    [SerializeField] private float scale1=0.0008f;
    [SerializeField] private float scale2=0.04f;
    [SerializeField] private float scale3=0.1f;

    [SerializeField] private float scale1y=12f;
    [SerializeField] private float scale2y=4f;
    [SerializeField] private float scale3y=1f;

    [SerializeField] private GameObject chunkTemplate;
    [SerializeField] private ItemSet[] itemSets;

    //the seed to be used
    int seed;
    //the total size of the map
    float size;

    public void GenerateTerrain(int _seed, float _size){
        seed = _seed;
        size = _size;

        //int numberOfChunks = (int) Math.Ceiling(size/(chunkSize*2))*2 + 1; //always an odd number of chunks
        int edgeNumber = (int) Math.Ceiling(size/(chunkSize*2)); //chunk number for the edge of the map

        for (int x = -1 * edgeNumber; x <= edgeNumber; x++)
        {
            for (int z = -1 * edgeNumber; z <= edgeNumber; z++)
            {
                GenerateChunk(new Vector2(x, z));
            }
        }


        AdjustWalls(); //moves the walls to the edge of the allowed map
        //announce that terrain generation has finsihed:
        GameObject.Find("GameManager").GetComponent<GameManager>().OnTerrainGenerationFisnished();
    }

    public void GenerateChunk(Vector2 chunkPosition)
    {
        //adjust the chunk position
        chunkPosition = chunkPosition *chunkSize;

        //generates a new terrain mesh
        Mesh mesh = new Mesh();
        mesh = GenerateMesh(mesh, chunkPosition);
        
        //spawn in a new chunk
        GameObject newChunk = Instantiate(chunkTemplate, new Vector3(chunkPosition.x, 0, chunkPosition.y), Quaternion.identity, transform.Find("Chunks"));
        //applies the new mesh to the components that need it
        newChunk.GetComponent<MeshFilter>().mesh = mesh;
        newChunk.GetComponent<MeshCollider>().sharedMesh = mesh;


        //setup the random
        //UnityEngine.Random.InitState((int) (seed + chunkPosition.x + chunkPosition.y));
        //populates the map
        foreach (ItemSet itemSet in itemSets){
            GenerateItems(itemSet, mesh);
        }
    }

    private void AdjustWalls() {
        GameObject.Find("Walls/Wall1").transform.position = new Vector3(0, 0, -size / 2);
        GameObject.Find("Walls/Wall1").transform.localScale = new Vector3(size, 100, 1);
        GameObject.Find("Walls/Wall2").transform.position = new Vector3(0, 0, size / 2);
        GameObject.Find("Walls/Wall2").transform.localScale = new Vector3(size, 100, 1);
        GameObject.Find("Walls/Wall3").transform.position = new Vector3(-size / 2, 0, 0);
        GameObject.Find("Walls/Wall3").transform.localScale = new Vector3(size, 100, 1);
        GameObject.Find("Walls/Wall4").transform.position = new Vector3(size / 2, 0, 0);
        GameObject.Find("Walls/Wall4").transform.localScale = new Vector3(size, 100, 1);
    }


    private Mesh GenerateMesh(Mesh mesh, Vector2 _offset)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        //clalculates an intSize - number of vertex wide
        int intSize = (int)(chunkSize/resolution) + 1;
        //calculates exact resolution
        float newResolution = (float)chunkSize/((int)(chunkSize/resolution));
        //makes a vector3 offset
        Vector3 offset = new Vector3 (_offset.x, 0, _offset.y);

        //populates veticies array
        for (float x = chunkSize * -0.5f; x <= chunkSize * 0.5f; x += newResolution)
        {
            for (float z = chunkSize * -0.5f; z <= chunkSize * 0.5f; z += newResolution)
            {
                //add a new vertex
                vertices.Add(GetNewVertex(x + offset.x, z + offset.z) - offset);
            }
        }
        for (int x = 0; x < intSize -1; x++)
        {
            for (int z = 0; z < intSize -1; z++)
            {
                //add a new triangle
                triangles.Add(intSize*x + z);
                triangles.Add(intSize*x + z + 1);
                triangles.Add(intSize*(x+1) + z);

                //add the second triangle
                triangles.Add(intSize*(x+1) + z);
                triangles.Add(intSize*x + z + 1);
                triangles.Add(intSize*(x+1) + z + 1);
            }
        }
        


        //sets up the new mesh
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    //generates items in the world
    private void GenerateItems(ItemSet itemSet, Mesh mesh) {
        //density is an abritary "percentage" where 100% is one item every 5X5 square
        //Times 4 is temporary to adjust for the fact that the generated area is 4 times the size of the actual size
        for (int i = 0; i<((size*size*4)*(itemSet.density/2500)); i++) {
            PlaceItem(itemSet.items[UnityEngine.Random.Range(0, itemSet.items.Length)], itemSet.offset, mesh);
        }
    }

    //places an item in the world
    private void PlaceItem(GameObject item, float offset, Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3 vertex = vertices[UnityEngine.Random.Range(0, vertices.Length)];
        float rotation = UnityEngine.Random.Range(0f, 360f);
        Instantiate(item, vertex + new Vector3(0,-offset,0), Quaternion.AngleAxis(rotation, Vector3.up),transform.Find("Population"));
    }

    //returns a point in space based of 3 layered noise maps and a UnityEngine.Random offset based of the seed
    private Vector3 GetNewVertex(float x, float z) 
    {
        float newX = x + seed;
        float newZ = z + seed;
        float y1 = Mathf.PerlinNoise(newX * scale1,newZ * scale1)*scale1y;
        float y2 = Mathf.PerlinNoise(newX * scale2,newZ * scale2)*scale2y;
        float y3 = Mathf.PerlinNoise(newX * scale3,newZ * scale3)*scale3y;
        float y = y1+y2+y3;
        return new Vector3(x, y, z);
    }

    public void DestroyPopulation() {
        //destory population
        foreach(Transform child in transform.Find("Population")) {
            Destroy(child.gameObject);
        }
        //destroy chunks
        foreach(Transform child in transform.Find("Chunks")) {
            Destroy(child.gameObject);
        }
    }
}
