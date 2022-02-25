using Firesplash.UnityAssets.SocketIO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

#if HAS_JSON_NET
//If Json.Net is installed, this is required for Example 6. See documentation for informaiton on how to install Json.NET
using Newtonsoft.Json;
#endif

public class Network : MonoBehaviour
{
    public string localhost = "localhost:8123";
    public string heroku = "sleepy-brook-59080.herokuapp.com/";

    public Text uiStatus;
    public Text debug;

    public SocketIOCommunicator sioCom;

    Dictionary<string, GameObject> players;
    public GameObject playerPrefab;
    public GameObject playerGameobject;
    public GameObject otherPlayers;
    public GameObject enemyPlayers;

    Dictionary<string, GameObject> npcs;
    public GameObject npcPlayerPrefab;
    public GameObject npcPlayers;
    

    public NPCHandler npcHandler;
    public CityHandler cityHandler;
    public GameManager gameManager;

    [Serializable]
    struct CityData
    {
        public string ownerID;
        public string cityName;
    //    public Vector3 worldLocation;
    }
    [Serializable]
    struct FoodData
    {
        public string id;
        public float[] location;
    }
    [Serializable]
    struct PayloadData
    {
        public string sid;
        public float speed;
        public float verticalVelocity;
        public float rotation;
        public Vector3 location;
        public Vector2 inputMove;
        public string animState;
        public bool isAttacking;
    }

    [Serializable]
    struct NPCPayloadData
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
    struct ScenePayloadData
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
    

    void Start()
    {
        sioCom.Instance.On("connect", (string data) => {
            Debug.Log("LOCAL: Hey, we are connected!");
            uiStatus.text = "Socket.IO Connected. Doing work...";
            CityData city = new CityData()
            {
                ownerID = gameManager.ownerID,
                cityName = gameManager.cityName,
            //    worldLocation = gameManager.worldLocation
            };
            
            sioCom.Instance.Emit("IsServerReady", gameManager.serverVersion);
           // sioCom.Instance.Emit("KnockKnock", JsonUtility.ToJson(city));
        });
        sioCom.Instance.On("ServerIsReady", (string data) => {
            Debug.Log("ServerIsReady: "+ data);
            CityData city = new CityData()
            {
                ownerID = gameManager.ownerID,
                cityName = gameManager.cityName,
                //    worldLocation = gameManager.worldLocation
            };
            sioCom.Instance.Emit("KnockKnock", JsonUtility.ToJson(city));
        });
        
        sioCom.Instance.On("ServerMessage", (string payload) =>
        {
            Debug.Log("RECEIVED a ServerMessage event" + payload);
            //We will always receive a payload object as Socket.IO does not distinguish. In case the server sent nothing (as it will do in this example) the object will be null.
            if (payload == null)
                Debug.Log("RECEIVED a ServerMessage event without payload data??");
            
            uiStatus.text = payload.ToString();
         //   gameManager.RestartScene();
        });

        sioCom.Instance.On("BuildScene", (string payload) =>
        {
            //Debug.Log("RECEIVED a BuildScene event" + payload);
            if (payload != "")
            {
                foreach (Transform rec in cityHandler.gameObject.transform)
                {
                    Destroy(rec.gameObject);
                }
                foreach (Transform rec in cityHandler.npcHandler.foods.gameObject.transform)
                {
                    Destroy(rec.gameObject);
                }
                cityHandler.npcHandler.queuedFoods.Clear();
                npcs.Clear();
                cityHandler.buildings.Clear();
                cityHandler.BuildCity(payload);

                //    cityHandler.BuildNPCBase();
                if (!gameManager.isAttacking)
                {
                    playerGameobject.transform.position = cityHandler.playerSpawnLocation;
                }
                else
                {
                    playerGameobject.transform.position = cityHandler.enemySpawnLocation;
                }
                var needSend = NeedSend();
                playerGameobject.GetComponent<NetworkMove>().needSend = needSend;
                npcHandler.needSend = needSend;
            }
            else
            {
                Debug.Log("BuildScene payload was NULL!!!");
            }
        });

        sioCom.Instance.On("SetNPCControl", (string payload) =>
        {
            Debug.Log("RECEIVED a SetNPCControl event" + payload);
            if (payload == "true")
            {
                //npcHandler.inControlOfNPCs = true;
                npcHandler.SetControl(true);
                Debug.Log("SetNPCControl Data!!!" + payload);
                

            }
            else
            {
                //npcHandler.inControlOfNPCs = false;
                npcHandler.SetControl(false);
                Debug.Log("SetNPCControl payload was false!!!");
            }
            
        });

        sioCom.Instance.On("WorldData", (string payload) =>
        {
            Debug.Log("RECEIVED a WorldData event" + payload);
            if (payload != null)
            {
                
               // Debug.Log("World Data!!!" + payload);
                cityHandler.BuildWorld(payload);
                
            }
            else
            {
                Debug.Log("WorldData payload was NULL!!!");
            }
            debug.text = debug.text + "WorldData " + payload.ToString();
            
  //          playerGameobject.transform.position = cityHandler.playerSpawnLocation;
  //          playerGameobject.SetActive(true);// = true;
        });

        sioCom.Instance.On("WhosThere", (string payload) =>
        {
            Debug.Log("RECEIVED a WhosThere event" + payload);
            //We will always receive a payload object as Socket.IO does not distinguish. In case the server sent nothing (as it will do in this example) the object will be null.
            if (payload == null)
                Debug.Log("RECEIVED a WhosThere event without payload data??");

            gameManager.camHandler.SwitchCams();
            gameManager.UpdateServerTime(payload);
        //    playerGameobject.transform.position = cityHandler.playerSpawnLocation;
     //       playerGameobject.transform.name = payload;           
        //    playerGameobject.SetActive(true);
            playerGameobject.GetComponent<NetworkMove>().connected = true;
            //As the server just asked for who we are, let's be polite and answer him.
            //EXAMPLE 3: Sending an event with payload data
            sioCom.Instance.Emit("ItsMe", JsonUtility.ToJson(payload));
        });

        sioCom.Instance.On("resetPlayers", (string payload) =>
        {
                foreach (Transform go in otherPlayers.transform)
                {
                    Destroy(go.gameObject);
                }
                players.Clear();
        });

        sioCom.Instance.On("Spawn", (string payload) =>
        {
            if (payload != null)
            {
                playerGameobject.GetComponent<NetworkMove>().needSend = true;
                npcHandler.needSend = true;
            //    Debug.Log("SPAWN!!!" + payload);
                OnSpawned(payload);
            }
            else
            {
                Debug.Log("SPAWN payload was NULL!!!");
            }
        });

        sioCom.Instance.On("spawnNPC", (string payload) =>
        {
            if (payload != null)
            {
                Debug.Log("SPAWN NPC!!!" + payload);
            //    OnSpawnedNPC(payload);
            }
            else
            {
                Debug.Log("SPAWN NPC payload was NULL!!!");
            }
        });

        sioCom.Instance.On("GetNPCData", (string payload) =>
        {
            Debug.Log("Server is fetching NPC data!!!");
            
            //npcPlayers.GetComponent<NPCHandler>().NPCRequested();
            List<Vector3> npcPositions = npcPlayers.GetComponent<NPCHandler>().GetNPCPositions();
            foreach(Vector3 position in npcPositions)
            {
                
                sioCom.Instance.Emit("NpcPositionsToServer", JsonUtility.ToJson(position));
            }
            sioCom.Instance.Emit("NpcPositionsOK");
        });

        sioCom.Instance.On("MoveNPC", (string payload) =>
        {
            if (payload != null)
            {
                //Debug.Log("npc moving " + payload);
                NPCPayloadData payloadData = JsonUtility.FromJson<NPCPayloadData>(payload);
                if (npcs.ContainsKey(payloadData.id))
                {
                    //    Debug.Log("npc moving FOUND" + payload);
                    var npcPlayer = npcs[payloadData.id];

                    Patrol npcMove = npcPlayer.GetComponent<Patrol>();
                    if (npcMove != null)
                    {
                        npcMove.NetworkSetNewTarget(FromFloat3(payloadData.target));
                        npcMove.SetWorkAnimState(payloadData.workAnimState);
                    }
                    else
                    {
                        Debug.Log("Something went wrong when tried to move NPC");
                    }
                }
                else
                {
                    Debug.Log("npc moving NPC NOT FOUND" + payload);
                }
            }
        });


        sioCom.Instance.On("moveRig", (string payload) =>
        {
            if (payload != null)
            {
                PayloadData payloadData = JsonUtility.FromJson<PayloadData>(payload);
                if (players.ContainsKey(payloadData.sid))
                {
                    var player = players[payloadData.sid];
                    
                    SioMover move = player.GetComponent<SioMover>();
                    move.MoveTo(
                        payloadData.inputMove, 
                        payloadData.speed, 
                        payloadData.verticalVelocity, 
                        payloadData.location, 
                        payloadData.rotation
                        );
                }
            }
        });

        sioCom.Instance.On("disconnected", (string payload) =>
        {
            if (payload == null)
                Debug.Log("RECEIVED NULL payload on Disconnect??.");
            else
            {
                OnDisconnected(payload);
                var needSend = NeedSend();
                playerGameobject.GetComponent<NetworkMove>().needSend = needSend;
                npcHandler.needSend = needSend;
            }
        });
        sioCom.Instance.On("disconnectPlayer", (string payload) =>
        {
            OnDisconnect();
        });

        sioCom.Instance.On("changeRoom", (string payload) =>
        {
            foreach (Transform go in otherPlayers.transform)
            {
                Destroy(go.gameObject);
            }
            players.Clear();
            npcs.Clear();
            var needSend = NeedSend();
            playerGameobject.GetComponent<NetworkMove>().needSend = needSend;
            npcHandler.needSend = needSend;
            
            Debug.Log("Player room" + payload);
        
        });

        sioCom.Instance.On("leaveRoom", (string payload) =>
        {
            PayloadData payloadData = JsonUtility.FromJson<PayloadData>(payload);
            Debug.Log("Client leaved: " + payload);
            if (players.ContainsKey(payloadData.sid))
            {
                var player = players[payloadData.sid];
                Destroy(player);
                players.Remove(payloadData.sid);
                var needSend = NeedSend();
                playerGameobject.GetComponent<NetworkMove>().needSend = needSend;
                npcHandler.needSend = needSend;
            }
            else
            {
                Debug.Log("PLAYER DOES NOT EXIST!!!!!!!!!!!!!!!!!!!!!!!!!");
            }

        });
        sioCom.Instance.On("ChangeAnimState", (string payload) =>
        {
            //We will always receive a payload object as Socket.IO does not distinguish. In case the server sent nothing (as it will do in this example) the object will be null.
            if (payload == null)
            {
                Debug.Log("RECEIVED a ChangeAnimState event without payload data??");
            }
            else
            {
                Debug.Log(payload);
                PayloadData payloadData = JsonUtility.FromJson<PayloadData>(payload);
                if (players.ContainsKey(payloadData.sid))
                {
                    var player = players[payloadData.sid];
                    SioMover move = player.GetComponent<SioMover>();
                    move.ChangeAnimationState(payloadData.animState);
                }
                else
                {
                    Debug.Log("PLAYER DOES NOT EXIST!!!!!!!!!!!!!!!!!!!!!!!!!");
                }
            }
        });
        sioCom.Instance.On("DeployFood", (string payload) =>
        {
            //We will always receive a payload object as Socket.IO does not distinguish. In case the server sent nothing (as it will do in this example) the object will be null.
            if (payload == null)
            {
                Debug.Log("RECEIVED a DeployFood event without payload data??");
            }
            else
            {
                FoodData payloadData = JsonUtility.FromJson<FoodData>(payload);
                if (npcs.ContainsKey(payloadData.id))
                {
                    //    Debug.Log("npc moving FOUND" + payload);
                    var npcPlayer = npcs[payloadData.id];

                    Patrol patrol = npcPlayer.GetComponent<Patrol>();
                    if (patrol != null)
                    {
                        patrol.SioDeployFood(FromFloat3(payloadData.location));
                    }
                    else
                    {
                        Debug.Log("Something went wrong when tried to deploy food");
                    }
                }

            }
        });

        players = new Dictionary<string, GameObject>();
        npcs = new Dictionary<string, GameObject>();
        sioCom.Instance.Connect();
    }

    private void OnDisconnected(string payload)
    {
        PayloadData payloadData = JsonUtility.FromJson<PayloadData>(payload);
        Debug.Log("Client disconnected: " + payloadData.sid);
        if (players.ContainsKey(payloadData.sid))
        {
            var player = players[payloadData.sid];
            Destroy(player);
            players.Remove(payloadData.sid);
        }
    }
    public void OnDisconnect()
    {
        sioCom.Instance.Close();
        
        Debug.Log("player disconnected: " );
        
    }
    void OnSpawned(string payload)
    {
        PayloadData payloadData = JsonUtility.FromJson<PayloadData>(payload);
        if (!players.ContainsKey(payloadData.sid))
        {
            Vector3 spawnPosition = payloadData.location; //Vector3.zero;
            

         //   Debug.Log("Player spawned " + spawnPosition + " : " + payload);
            GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity) as GameObject;
            if (!payloadData.isAttacking)
            {
                player.transform.parent = otherPlayers.transform;
            }
            else
            {
                player.transform.parent = enemyPlayers.transform;
                player.transform.tag = "Enemy";
            }
            //Mover movePos = player.GetComponentInChildren<Mover>();
            player.transform.name = payloadData.sid;


            players.Add(payloadData.sid, player);
        }
    }

    public bool NeedSend()
    {
        var others = players.Count > 0;
        return others;
    }
    public void AddNPC(string id, GameObject npc)
    {
        if (!npcs.ContainsKey(id))
        {
            npcs.Add(id, npc);
        }
    }
    public void UpdateCityToServer()
    {
        var payload = cityHandler.GetPayload();
        sioCom.Instance.Emit("UpdatePlayerCity", JsonUtility.ToJson(payload));
    }
    public static Vector3 FromFloat3(float[] vecs)
    {
        //Debug.Log(vecs);
        Vector3 vector3 = new Vector3(vecs[0], vecs[1], vecs[2]);

        return vector3;
    }
    public static Quaternion FromFloat4(float[] vecs)
    {
        Quaternion rotation = new Quaternion(vecs[0], vecs[1], vecs[2], vecs[3]);

        return rotation;
    }
}
