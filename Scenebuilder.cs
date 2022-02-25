using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scenebuilder : MonoBehaviour
{
    public GameObject cityPrefab;
    [Serializable]
    public struct BuildingGameObject
    {
        public string buildingID;
        public string buildingName;
        public int buildingLevel;
        public float buildingHealth;
    }
    [Serializable]
    struct ScenePayloadData
    {
        //public string Id;
        public string Name;
        //public bool IsStatic;
        //public bool IsActive;
        //public UTransform Transform;
        public Vector3 Location;
        public int Population;
        public int Army;
        public int Farmers;
        public float Health;
        public int Level;
        public BuildingGameObject Building1;
        /*
        public int UNITYID;
        public string name;
        public Vector3 location;
        public int population;
        public int army;
        public int farmers;
    */
    }

    /*[Serializable]
    public class UserObject
    {
        public int UNITYID;
        public string name;
        
        public Vector3 location;
        public Vector3 rotation;
        public Vector3 scale;
        public int population;
        public int army;
        public int farmers;
    }
    [Serializable]
    public class RootObject
    {
        public UserObject[] users;
    }*/
    public void BuildScene(string payload)
    {
        Debug.Log("Build Scene " + payload);
        //ScenePayloadData[] cityPayload = JsonConvert.DeserializeObject<ScenePayloadData[]>(payload);
        //Debug.Log("Thanks to Json.NET we were able to deode the numbers: " + string.Join(", ", cityPayload));
        ScenePayloadData payloadData = JsonUtility.FromJson<ScenePayloadData>(payload);
        //  var myObject = JsonUtility.FromJson<RootObject>("{\"Id\":}");
        //Transform[] a = cityPayload[0].tr;
        Vector3 citylocation = payloadData.Location;
        GameObject city = Instantiate(cityPrefab, citylocation, Quaternion.identity) as GameObject;
        city.transform.name = payloadData.Name;
        CityStats stats = city.GetComponent<CityStats>();
        stats.farmers = payloadData.Farmers;
        stats.army = payloadData.Army;
        stats.population = payloadData.Population;
        //stats.buildings[0].GetComponent<BuildingStats>().SetStats(payloadData.Building1.buildingLevel, payloadData.Building1.buildingHealth);
        
    }

}
