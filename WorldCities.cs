using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New City", menuName = "City/city")]
public class WorldCities : ScriptableObject
{
    public City city;
}
[System.Serializable]
public class City
{
    public string ownerID;
    public int population;
    //  public int army;
    //  public int farmers;
    //  public int resource;
    //  public List<BuildingPayloadData> buildingPayloadData;
    public string cityName;
    public Vector3 worldLocation;

}
