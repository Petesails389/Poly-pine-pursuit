using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

public class TerrainGeneration : MonoBehaviour
{
    
    [SerializeField] private float size;
    [SerializeField] private float resolution = 0.5f;

    [SerializeField] private float scale1;
    [SerializeField] private float scale2;
    [SerializeField] private float scale3;

    [SerializeField] private float scale1y;
    [SerializeField] private float scale2y;
    [SerializeField] private float scale3y;

    [SerializeField] private ItemSet[] itemSets;

    //the terrain mesh
    private Mesh mesh;
    //the seed to be used
    int seed;

    public void GenerateTerrain(){
        seed = GameObject.Find("GameManager").GetComponent<GameManager>().GetSeed();

        Random.InitState((int) seed);
        mesh = new Mesh();
        //generates a new terrain mesh
        GenerateMesh();
        //applies the new mesh to the components that need it
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;

        //populates the map
        foreach (ItemSet itemSet in itemSets){
            //GenerateItems(itemSet);
        }

        AdjustWalls(); //moves the walls to the edge of the allowed map
    }

    private void AdjustWalls() {
        GameObject.Find("Wall1").transform.position += new Vector3(size/2,0,0);
        GameObject.Find("Wall1").transform.localScale = new Vector3(size,100,1);
        GameObject.Find("Wall2").transform.position += new Vector3(size/2,0,size);
        GameObject.Find("Wall2").transform.localScale = new Vector3(size,100,1);
        GameObject.Find("Wall3").transform.position += new Vector3(0,0,size/2);
        GameObject.Find("Wall3").transform.localScale = new Vector3(size,100,1);
        GameObject.Find("Wall4").transform.position += new Vector3(size,0,size/2);
        GameObject.Find("Wall4").transform.localScale = new Vector3(size,100,1);
    }


    private void GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        //populates veticies array
       for (float x = -size; x < size*2; x += resolution)
        {
            for (float z = -size; z < size*2; z += resolution)
            {
                //add the verticies for the first triangle
                vertices.Add(GetNewVertex(x+1,z));
                vertices.Add(GetNewVertex(x,z+1));
                vertices.Add(GetNewVertex(x,z));
                //adds the first triangle
                triangles.Add(vertices.Count-1);
                triangles.Add(vertices.Count-2);
                triangles.Add(vertices.Count-3);


                //add the verticies for the second triangle
                vertices.Add(GetNewVertex(x+1,z+1));
                vertices.Add(GetNewVertex(x,z+1));
                vertices.Add(GetNewVertex(x+1,z));
                //adds the second triangle
                triangles.Add(vertices.Count-1);
                triangles.Add(vertices.Count-2);
                triangles.Add(vertices.Count-3);
            }
        }

        //sets up the new mesh
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    //generates items in the world
    private void GenerateItems(ItemSet itemSet) {
        for (int i = 0; i<((size*size*9)/itemSet.density); i++) {
            PlaceItem(itemSet.items[Random.Range(0, itemSet.items.Length)], itemSet.offset);
        }
    }

    //places an item in the world
    private void PlaceItem(GameObject item, float offset)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3 vertex = vertices[Random.Range(0, vertices.Length)];
        float rotation = Random.Range(0f, 360f);
        Instantiate(item, vertex + new Vector3(0,-offset,0), Quaternion.AngleAxis(rotation, Vector3.up));
    }

    //returns a point in space based of 3 layered noise maps and a random offset based of the seed
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
}
