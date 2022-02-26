using Firesplash.UnityAssets.SocketIO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public string serverVersion = "1";
    public SocketIOCommunicator sioCom;
    public CamHandler camHandler;
    public Network network;
    public InputField loginName;
    public bool inOwnCity = false;
    public string ownerID;
    public string cityName;
    
    public string currentRoom;
    public Text uiStatus;
    public Text serverTime;
    public Text dbUpdateTime;
    public Text cityResources;
    public bool isAttacking = false;
    [Serializable]
    struct ChangeRoomData
    {
        public string currentRoom;
        public string newRoom;
        public bool wasControllingNPC;
        public bool isAttacking;
    }
    [Serializable]
    struct ServerTimePayload
    {
        public string serverTimeNow;
        public string lastDBUpdateTime;
    }
    public void UpdateCityToServer()
    {
        network.UpdateCityToServer();
    }
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
    
    public void SetCityName()
    {
        if (loginName.text.Length > 3)
        {
            cityName = loginName.text;
                        
            if (PlayerPrefs.GetString("CityName") == "")
            {
                PlayerPrefs.SetString("CityName", cityName);
                loginName.gameObject.SetActive(false);
                network.enabled = true;
            }
            else if (PlayerPrefs.GetString("CityName") == cityName)
            {
                loginName.gameObject.SetActive(false);
                network.enabled = true;
            }
            
        }
    }
    private void Awake()
    {
        if (PlayerPrefs.GetString("CityName") != "")
        {
            cityName = PlayerPrefs.GetString("CityName");
            loginName.text = cityName;
        }
        if (PlayerPrefs.GetString("ownerID") != "")
        {
            ownerID = PlayerPrefs.GetString("ownerID");
            currentRoom = ownerID;
            inOwnCity = true;
        }
        
    }
    
    public void ChangeRoom(string newRoomOwnerID, bool attack)
    {
        Debug.Log("change room"+ newRoomOwnerID);
        isAttacking = attack;
        ChangeRoomData changeRoomData = new ChangeRoomData()
        {
            currentRoom = currentRoom,
            newRoom = newRoomOwnerID,
            wasControllingNPC = network.npcHandler.inControlOfNPCs,
            isAttacking = attack
        };
        network.npcHandler.SetControl(false);
        //debug.text = JsonUtility.ToJson(changeRoomData).ToString();

        sioCom.Instance.Emit("ChangeRoom", JsonUtility.ToJson(changeRoomData));
        currentRoom = newRoomOwnerID;
        uiStatus.text = "Current City ID: " + currentRoom;
        inOwnCity = ownerID == currentRoom;
    }
    public void SwitchCams()
    {
        camHandler.SwitchCams();
    }

    private void Update()
    {

        if (Keyboard.current[Key.Tab].wasPressedThisFrame)
        {
            SwitchCams();
        }
    }
    public void UpdateServerTime(string payload)
    {
        ServerTimePayload data = JsonUtility.FromJson<ServerTimePayload>(payload);
        serverTime.text = data.serverTimeNow;
        dbUpdateTime.text = data.lastDBUpdateTime;
    }
}
