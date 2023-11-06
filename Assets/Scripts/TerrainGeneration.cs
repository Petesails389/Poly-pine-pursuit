using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneration : MonoBehaviour
{
    [SerializeField] private float size = 50f;
    [SerializeField] private float resolution = 0.5f;

    [SerializeField] private float scale1;
    [SerializeField] private float scale2;
    [SerializeField] private float scale3;

    [SerializeField] private float scale1y;
    [SerializeField] private float scale2y;
    [SerializeField] private float scale3y;


    void Start()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        //populates veticies array
       for (float x = 0; x < size; x += resolution)
        {
            for (float z = 0; z < size; z += resolution)
            {
                //add the verticies for the first triangle
                vertices.Add(GetVertex(x+1,z));
                vertices.Add(GetVertex(x,z+1));
                vertices.Add(GetVertex(x,z));
                //adds the first triangle
                triangles.Add(vertices.Count-1);
                triangles.Add(vertices.Count-2);
                triangles.Add(vertices.Count-3);


                //add the verticies for the second triangle
                vertices.Add(GetVertex(x+1,z+1));
                vertices.Add(GetVertex(x,z+1));
                vertices.Add(GetVertex(x+1,z));
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

        //applies the new mesh to the componentes
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    //returns a point in space based of 3 layered noise maps
    private Vector3 GetVertex(float x, float z) 
    {
        float y1 = Mathf.PerlinNoise(x * scale1,z*scale1)*scale1y;
        float y2 = Mathf.PerlinNoise(x * scale2,z*scale2)*scale2y;
        float y3 = Mathf.PerlinNoise(x * scale3,z*scale3)*scale3y;
        float y = y1+y2+y3;
        return new Vector3(x, y, z);
    }
}
