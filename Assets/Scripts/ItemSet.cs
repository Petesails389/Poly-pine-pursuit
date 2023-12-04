using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSet", menuName = "ItemSet")]
public class ItemSet : ScriptableObject
{
    [SerializeField] public float density;
    [SerializeField] public GameObject[] items;
    [SerializeField] public float offset; // spawn offset on the y axis - used to ensure it clips through the ground and doesn't float for itemsets that contain bigger objects
}