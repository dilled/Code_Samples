using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CityHandler : MonoBehaviour
{
    [Serializable]
    public struct WorldCityList
    {
        public List<WorldPayloadData> cities;
    }
    [Serializable]
    public struct WorldPayloadData
    {
            
            public string ownerID;
            public int population;
        //  public int army;
        //  public int farmers;
        //  public int resource;
        //  public List<BuildingPayloadData> buildingPayloadData;
            public string cityName;
            public float[] worldLocation;
    }
    [Serializable]
    public struct CityPayloadData
    {
        public string ownerID;
        public int population;
        public int army;
        public int farmers;
        public int slaves;
        public int resource;
        public List<BuildingPayloadData> buildingPayloadData;
        public string cityName;
        public float[] worldLocation;
        public List<NPCPayloadData> npcPayloadData;
    }
    [Serializable]
    public struct BuildingPayloadData
    {
        public string name;
        public int level;
        public float health;
        public int buildingPrefab;
        public float[] buildingLocation;
        public float[] buildingRotation;
        public float[] buildingScale;
    }
    [Serializable]
    public struct NPCPayloadData
    {
        public string id;
        public string name;
        public int level;
        public float health;
        public int speed;
        public int strength;
        public float[] location;
        public float[] rotation;
        public float[] target;
        public int npcPrefab;
    }
    #region Singleton

    public static CityHandler instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of CityHandler found!");
            return;
        }

        instance = this;
        /*
        if (PlayerPrefs.GetString("CityName") != null)
        {
            cityName = PlayerPrefs.GetString("CityName");
        }
        if (PlayerPrefs.GetString("ownerID") != null)
        {
            ownerID = PlayerPrefs.GetString("ownerID");
        }
        */
    }

    #endregion

    // Callback which is triggered when
    // an item gets added/removed.
    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    //public List<WorldCities> worldCities = new List<WorldCities>();
    
    public List<Building> buildings = new List<Building>();

    public List<GameObject> buildingPrefabs = new List<GameObject>();

    public string ownerID;
    public int population;
    public int army;
    public int farmers;
    public int slaves;
    public int resource;
    public string cityName;
    public Vector3 worldLocation;
    public Vector3 playerSpawnLocation;
    public Vector3 enemySpawnLocation;
    public Text uiStatus;
    public Text uiStatus2;
    public Text cityResources;
    public GameManager gameManager;
    public NPCHandler npcHandler;
    //For building the world map from server payload
    public CityPayloadData GetCityPayloadData()
    {
        var payload = new CityPayloadData
        {
            ownerID = ownerID,
            population = population,
            army = army,
            farmers = farmers,
            slaves = slaves,
            resource = resource,
         //   buildingPayloadData = 
        };
        return payload;
    }
    public void GainResources(int amount)
    {
        resource += amount;
        cityResources.text = resource.ToString();
        gameManager.UpdateCityToServer();
    }
    public void BuildWorld(string payload)
    {
        WorldHandler worldHandler = GetComponentInParent<WorldHandler>();
        //Debug.Log(payload);
        if (payload != null)
        {
            WorldCityList _data = JsonUtility.FromJson<WorldCityList>(payload);
            foreach (WorldPayloadData cdata in _data.cities)
            {
                if (worldHandler.IsNewCity(cdata.ownerID))
                {
                    City citydat = new City
                    {
                        ownerID = cdata.ownerID,
                        population = cdata.population,
                        cityName = cdata.cityName,
                        worldLocation = FromFloat3(cdata.worldLocation)
                    };
                    WorldCities citys = ScriptableObject.CreateInstance<WorldCities>();
                    citys.city = citydat;
         //           Debug.Log(citys);
                    citys.name = cdata.cityName;
                    worldHandler.worldCities.Add(citys);
                    worldHandler.UpdateWorldMap(citys);
                }
            }
        }
    }
    public void BuildCity(string payload)
    {
       // Debug.Log(payload);
        if (payload != null)
        {
            CityPayloadData citydata = JsonUtility.FromJson<CityPayloadData>(payload);
            foreach (BuildingPayloadData data in citydata.buildingPayloadData)
            {
                BuildingData buildingData = new BuildingData
                {
                    name = data.name,
                    level = data.level,
                    health = data.health,
                    buildingPrefab = data.buildingPrefab,
                    buildingLocation = FromFloat3(data.buildingLocation),
                    buildingRotation = FromFloat4(data.buildingRotation),
                    buildingScale = FromFloat3(data.buildingScale)
                };
                Building building = ScriptableObject.CreateInstance<Building>();
                building.buildingData = buildingData;
                
                building.name = data.name;
                buildings.Add(building);
            }
            ownerID = citydata.ownerID;
            population = citydata.population;
            army = citydata.army;
            farmers = citydata.farmers;
            slaves = citydata.slaves;
            resource = citydata.resource;
            cityName = citydata.cityName;
            worldLocation = FromFloat3(citydata.worldLocation);
            float[] playerLocation;
            playerLocation = new float[3];
            playerLocation[0] = citydata.worldLocation[0];
            playerLocation[1] = 17f;
            playerLocation[2] = citydata.worldLocation[2];
            float[] xy;
            xy = new float[2];
            xy[0] = -50;
            xy[1] = 50;
            float[] enemyLocation;
            enemyLocation = new float[3];
            enemyLocation[0] = citydata.worldLocation[0] + xy[UnityEngine.Random.Range(0,xy.Length)];
            enemyLocation[1] = 1f;
            enemyLocation[2] = citydata.worldLocation[2] + xy[UnityEngine.Random.Range(0, xy.Length)];
            playerSpawnLocation = FromFloat3(playerLocation);
            enemySpawnLocation = FromFloat3(enemyLocation);
            if (PlayerPrefs.GetString("CityName") == "")
            {
                PlayerPrefs.SetString("CityName", citydata.cityName);
            }
            if (PlayerPrefs.GetString("ownerID") == "")
            {
                PlayerPrefs.SetString("ownerID", citydata.ownerID);
                gameManager.ownerID = citydata.ownerID;
                gameManager.currentRoom = citydata.ownerID;
                gameManager.inOwnCity = true;
            }
            cityResources.text = resource.ToString();
            uiStatus.text = "Current City ID: " + ownerID;
            uiStatus2.text = "Current City Name: " + cityName;
            BuildCity();
            foreach (Transform rec in npcHandler.gameObject.transform)
            {
                Destroy(rec.gameObject);
            }
            npcHandler.npcCharacters.Clear();

            foreach (NPCPayloadData data in citydata.npcPayloadData)
            {
                NPCData npcData = new NPCData
                {
                    id = data.id,
                    name = data.name,
                    level = data.level,
                    health = data.health,
                    speed = data.speed,
                    strength = data.strength,
                    location = FromFloat3(data.location),
                    rotation = FromFloat4(data.rotation),
                    target = FromFloat3(data.target),
                    npcPrefab = data.npcPrefab
                };
                npcHandler.BuildNPCFromPayload(npcData);
                
            }
            npcHandler.BuildNPCsFromPayload();
        }
    }
    
    public void BuildCity()
    {
      //  Debug.Log("Start City " + buildings.Count);
        transform.position = worldLocation;
        foreach (var building in buildings)
        {
            BuildCity(building);
        }
        
    }
    public void BuildCity(Building building)
    {
        var rec = building.buildingData;
        GameObject currentBuilding = Instantiate(buildingPrefabs[rec.buildingPrefab], rec.buildingLocation, rec.buildingRotation) as GameObject;
        currentBuilding.transform.parent = transform;
        currentBuilding.transform.localPosition = rec.buildingLocation;
     //   currentBuilding.transform.localScale = rec.buildingScale;// FromFloat3(rec.buildingScale);
    }
    
    public CityPayloadData GetPayload()
    {
        //var buildingList = new List<BuildingData>();
        var list = new List<BuildingPayloadData>();

        foreach (var building in buildings)
        {
            var rec = building.buildingData;
            /*var _data = new BuildingData 
            {
                name = rec.name,
                level = rec.level,
                health = rec.health,
                buildingPrefab = rec.buildingPrefab,
                buildingLocation = rec.buildingLocation,
                buildingRotation = rec.buildingRotation,
                buildingScale = rec.buildingScale
            };*/
            
            BuildingPayloadData _dat = new BuildingPayloadData
            {
                name = rec.name,
                level = rec.level,
                health = rec.health,
                buildingPrefab = rec.buildingPrefab,
                buildingLocation = ToFloat3(rec.buildingLocation),
                buildingRotation = ToFloat4(rec.buildingRotation),
                buildingScale = ToFloat3(rec.buildingScale)
            };
            list.Add(_dat);
            //buildingList.Add(_data);
            //Debug.Log(JsonUtility.ToJson(_dat));
        }
        CityPayloadData cityData = new CityPayloadData
        {
            ownerID = ownerID,
            population = population,
            army = army,
            farmers = farmers,
            slaves = slaves,
            resource = resource,
            buildingPayloadData = list,
            cityName = cityName,
            worldLocation = ToFloat3(worldLocation),
            npcPayloadData = npcHandler.GetPayload()
        };
        //Debug.Log(list);
        //Debug.Log(JsonConvert.SerializeObject(list.ToArray(), Formatting.Indented));
        return cityData;
    }
    public static float[] ToFloat3(Vector3 vec)
    {
        var list = new List<float>();

            list.Add(vec.x);
            list.Add(vec.y);
            list.Add(vec.z);

        return list.ToArray();
    }
    public static float[] ToFloat4(Quaternion vec)
    {
        var list = new List<float>();

        list.Add(vec.x);
        list.Add(vec.y);
        list.Add(vec.z);
        list.Add(vec.w);

        return list.ToArray();
    }
    public static Vector3 FromFloat3(float[] vecs)
    {
        Vector3 vector3 = new Vector3(vecs[0], vecs[1], vecs[2]);

        return vector3;
    }
    public static Quaternion FromFloat4(float[] vecs)
    {
        Quaternion rotation = new Quaternion(vecs[0], vecs[1], vecs[2], vecs[3]);

        return rotation;
    }

    /*public void BuildNPCBase()
    {
        foreach (Transform rec in npcHandler.gameObject.transform)
        {
            Destroy(rec.gameObject);
        }
        npcHandler.npcCharacters.Clear();
        npcHandler.BuildNPCs(farmers, army);
    }*/
    
}
