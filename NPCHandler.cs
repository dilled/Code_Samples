using Firesplash.UnityAssets.SocketIO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class NPCHandler : MonoBehaviour
{
    public bool needSend = false;
    public CityHandler cityHandler;
    public Network network;
    public SocketIOCommunicator sioCom;
    public List<NPC> npcCharacters = new List<NPC>();
    public List<GameObject> npcPrefabs = new List<GameObject>();
    public static List<Vector3> npcPositions = new List<Vector3>();
    public GameObject npcPlayerPrefab;
    public Vector3 startPoint;
    public float spawnRadius = 10f;
    public int amountOfNPC;
    public GameObject currentCity;
    public bool inControlOfNPCs = false;

    public Vector3 currentCityLocation;
    public Vector3[] farmerFields;
    private int farmerDestinationPoint = 0;
    public GameObject foods;
    public List<GameObject> queuedFoods = new List<GameObject>();
    public Toggle needSendToggle;
    public Toggle controlsNpcsToggle;

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
        public bool workAnimState;
    }
    [Serializable]
    struct FoodData
    {
        public string id;
        public float[] location;
    }
    public bool NeedSend()
    {
        if (inControlOfNPCs && needSend)
        {
            needSendToggle.isOn = true;
            return true;
        }
        else
        {
            needSendToggle.isOn = false;
            return false;
        }
    }
    public GameObject GetWork()
    {
        foreach(Transform res in foods.transform)
        {
            if (!res.GetComponent<ResourceHandler>().assigned)
            {
                res.GetComponent<ResourceHandler>().assigned = true;
                return res.gameObject;
            }
        }
        return null;
    }
    public void AssignWork(GameObject item)
    {
        if (!item.GetComponent<ResourceHandler>().assigned)
        {
            queuedFoods.Add(item);
            var slaves = GetComponentsInChildren<SlaveAI>();

            foreach (SlaveAI slave in slaves)
            {
                if (!slave.working && !slave.onWorking && slave.patrol.points.Count < 2)
                {
                    slave.onWorking = true;
                    slave.StartWorking(item);
                    queuedFoods.Remove(item);
                    item.GetComponent<ResourceHandler>().assigned = true;
                    return;
                }
            }
        }
    }
    public void RemoveFoodFromQueue(GameObject item)
    {
        if (queuedFoods.Contains(item))
        {
            queuedFoods.Remove(item);
        }
    }
    public void AddFoodToQueue(GameObject item)
    {
        if (!item.GetComponent<ResourceHandler>().assigned)
        {
            queuedFoods.Add(item);
        }
    }
    public void SendNewTarget(string npcID, Vector3 target, Vector3 currentPosition, bool animState)
    {
        NPCPayloadData npcData = new NPCPayloadData()
        {
            id = npcID,
            location = ToFloat3(currentPosition),
            target = ToFloat3(target),
            workAnimState = animState

        };
        if (inControlOfNPCs && needSend)
        {
            sioCom.Instance.Emit("MoveNPC", JsonUtility.ToJson(npcData));
        }
    }

    public void DeployFood(string id, Vector3 position)
    {
        FoodData foodData = new FoodData
        {
            id = id,
            location = ToFloat3(position)
        };

        sioCom.Instance.Emit("DeployFood", JsonUtility.ToJson(foodData));
    }
    
    public void SendNewTarget(NPC npc)
    {
        var data = npc.npcData;
        NPCPayloadData npcPayload = new NPCPayloadData
        {
            id = data.id,
            name = data.name,
            level = data.level,
            health = data.health,
            speed = data.speed,
            strength = data.strength,
            location = ToFloat3(data.location),
            rotation = ToFloat4(data.rotation),
            target = ToFloat3(data.target),
            npcPrefab = data.npcPrefab,
            workAnimState = data.workAnimState
        };
        if (inControlOfNPCs && needSend)
        {
        //    Debug.Log("Sending NPC Moves!!!");
            sioCom.Instance.Emit("MoveNPC", JsonUtility.ToJson(npcPayload));
        }
    }
    public void SetControl(bool value)
    {
        inControlOfNPCs = value;
        controlsNpcsToggle.isOn = value;

        foreach (Patrol patrol in GetComponentsInChildren<Patrol>())
        {
            patrol.patrol = value;         
        }
        foreach (SlaveAI slave in GetComponentsInChildren<SlaveAI>())
        {
            slave.sioInControl = !value;    
        }
        foreach (FarmerAI farmer in GetComponentsInChildren<FarmerAI>())
        {
            farmer.enabled = value;  
        }
    }
    public Vector3 GetTarget()
    {
        currentCityLocation = currentCity.transform.position;
        var newTarget = new Vector3(currentCityLocation.x + farmerFields[farmerDestinationPoint].x, 0, currentCityLocation.z + farmerFields[farmerDestinationPoint].z);
        
        // Choose the next point in the array as the destination,
        // cycling to the start if necessary.
        farmerDestinationPoint = (farmerDestinationPoint + 1) % farmerFields.Length;
        
        return newTarget;
    }

    public void NPCRequested()
    {
        for (var i = 1; i <= amountOfNPC; i++)
        {
            npcPositions.Add(GetRandomPosition());
        }
    }
    public List<Vector3> GetNPCPositions()
    {
        for (var i = 1; i <= amountOfNPC; i++)
        {
            npcPositions.Add(GetRandomPosition());
        }
        return npcPositions;
    }

    Vector3 GetRandomPosition()
    {
        Vector3 someRandomPoint = startPoint + Random.insideUnitSphere * spawnRadius;
        someRandomPoint.y = 1.5f;
        return someRandomPoint;
    }
    Vector3 GetRandomPosition(Vector3 position)
    {
        Vector3 someRandomPoint = position + Random.insideUnitSphere * spawnRadius;
        someRandomPoint.y = 0f;
        return someRandomPoint;
    }

    void RandomSpawn()
    {
        Vector3 someRandomPoint = startPoint + Random.insideUnitSphere * spawnRadius;
        Debug.Log("NPC spawned " + someRandomPoint);
        GameObject npc = Instantiate(npcPlayerPrefab, someRandomPoint, Quaternion.identity) as GameObject;
        npc.transform.parent = transform;
    }
    public void BuildNPCFromPayload(NPCData npcData)
    {
        NPC npc = ScriptableObject.CreateInstance<NPC>();
        npc.npcData = npcData;
        npc.name = npcData.name;
        npcCharacters.Add(npc);
    }
    public void BuildNPCsFromPayload()
    {
        foreach (var npc in npcCharacters)
        {
            BuildNPC(npc);
        }
    }
    public void BuildNPC(NPC npc)
    {
        var rec = npc.npcData;
        var location = rec.location;
        if (location == Vector3.zero)
        {
            location = GetRandomPosition(currentCity.transform.position);
        }
        GameObject currentNPC = Instantiate(npcPrefabs[rec.npcPrefab], location, rec.rotation) as GameObject;
        currentNPC.transform.parent = transform;
        currentNPC.GetComponent<Patrol>().SetID(rec.id, npc);

        network.AddNPC(rec.id, currentNPC);
    }
    public List<CityHandler.NPCPayloadData> GetPayload()
    {
        foreach(Patrol patrol in GetComponentsInChildren<Patrol>())
        {
            patrol.UpdateLocRot();
        }
        var list = new List<CityHandler.NPCPayloadData>();
        
        foreach (var npc in npcCharacters)
        {
            var rec = npc.npcData;
            
            CityHandler.NPCPayloadData _dat = new CityHandler.NPCPayloadData
            {
                id = rec.id,
                name = rec.name,
                level = rec.level,
                health = rec.health,
                speed = rec.speed,
                strength = rec.strength,
                location = ToFloat3(rec.location),
                rotation = ToFloat4(rec.rotation),
                target = ToFloat3(rec.target),
                npcPrefab = rec.npcPrefab

            };
            list.Add(_dat);
        }
        
        return list;
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
}

