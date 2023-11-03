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
    void Update()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        //populates veticies array
       for (float x = 0; x < size; x += resolution)
        {
            for (float z = 0; z < size; z += resolution)
            {
                float y1 = Mathf.PerlinNoise(x *scale1,z*scale1)*scale1y;
                float y2 = Mathf.PerlinNoise(x *scale2,z*scale2)*scale2y;
                float y3 = Mathf.PerlinNoise(x *scale3,z*scale3)*scale3y;
                float y = y1+y2+y3;
                vertices.Add( new Vector3(x, y, z));
            }
        }

        //generates triangles
        int num = (int) (size/resolution);
        for (int z = 0; z < num-1; z ++)
        {
            for (int x = 0; x < num-1; x ++) 
            {
                //for now it simply generates A triangle will work on the BEST triangle later
                triangles.Add(x + (z * num) +1);
                triangles.Add(x + (z+1)*num);
                triangles.Add(x + z * num);

                triangles.Add(x + (z+1)*num +1);
                triangles.Add(x + (z+1)*num);
                triangles.Add(x + (z * num) +1);
            }
        }

        //sets up the new mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        //applies the new mesh to the componentes
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }
}
