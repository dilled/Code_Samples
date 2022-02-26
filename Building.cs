using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "City/Building")]
public class Building : ScriptableObject
{
    public BuildingData buildingData;
}
[System.Serializable]
public class BuildingData
{
    public string name = "New Building";
    public int level;
    public float health;
    public int buildingPrefab;
    public Vector3 buildingLocation;
    public Quaternion buildingRotation;
    public Vector3 buildingScale;
}
