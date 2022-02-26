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
        public string Name;
        public Vector3 Location;
        public int Population;
        public int Army;
        public int Farmers;
        public float Health;
        public int Level;
        public BuildingGameObject Building1;
    }

    public void BuildScene(string payload)
    {
        Debug.Log("Build Scene " + payload);
        ScenePayloadData payloadData = JsonUtility.FromJson<ScenePayloadData>(payload);
        Vector3 citylocation = payloadData.Location;
        GameObject city = Instantiate(cityPrefab, citylocation, Quaternion.identity) as GameObject;
        city.transform.name = payloadData.Name;
        CityStats stats = city.GetComponent<CityStats>();
        stats.farmers = payloadData.Farmers;
        stats.army = payloadData.Army;
        stats.population = payloadData.Population;
    }
}
