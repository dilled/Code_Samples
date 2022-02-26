using Firesplash.UnityAssets.SocketIO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

#if HAS_JSON_NET
using Newtonsoft.Json;
#endif

public class NetworkMove : MonoBehaviour
{
    public bool needSend = false;
    public Animator _animator;
    private StarterAssetsInputs _input;
    public SocketIOCommunicator sioCom;
    public Network network;
    public bool connected = false;
    struct ItsMeData
    {
        public string version;
    }
    struct PlayerData
    {
        public float speed;
        public float verticalVelocity;
        public float rotation;
        public Vector3 location;
        public Vector2 inputMove;
    }
    public float lastRot;
    public Vector3 lastLloc;
    public bool isStopped = false;
    public float lastSpeed;

    public float idle_count = 0f;
    public bool idle_1 = true;
    public string currentAnimState;
    const string IDLE_NOSE = "Idle_wipe_nose";
    const string IDLE_ASS = "Idle_wipe_ass";
    public bool firstSend = true;
    ThirdPersonController thirdPersonController;

    public void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == "HQ")
        {
            Debug.Log(other.name);
            GetComponent<ThirdPersonController>().enabled = false;
            var pos = other.GetComponentInParent<CityHandler>().playerSpawnLocation;
            gameObject.transform.position = pos;
            Invoke("ContinueTPSCTRL",.1f);
        }
    }
    public void ContinueTPSCTRL()
    {
        GetComponent<ThirdPersonController>().enabled = true;
    }
    void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();
        thirdPersonController = GetComponent<ThirdPersonController>();
    }
    public bool NeedSend()
    {
        var others = network.otherPlayers.transform.childCount > 0;
        return others;
    }
    public void SendPayload()
    {
        Vector3 location = new Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
        float rotation = thirdPersonController.GetRotation();//wawda transform.eulerAngles.y;
        float speed = thirdPersonController.GetSpeed();
        float verticalVelocity = thirdPersonController.GetVerticalVelocity();
        Vector2 inputMove = thirdPersonController.GetInput();
        //Debug.Log(inputMove);
        PlayerData locRot = new PlayerData()
        {
            speed = speed,
            verticalVelocity = verticalVelocity,
            location = location,
            rotation = rotation,
            inputMove = inputMove,
        };
        if (needSend)
        {  
            sioCom.Instance.Emit("MoveRig", JsonUtility.ToJson(locRot)); 
            lastLloc = location;
            lastRot = rotation;
            lastSpeed = speed;
            isStopped = false;
            idle_count = 0f;
            firstSend = false;
        }
        if (lastLloc == location || lastRot == rotation)
        {
            Debug.Log("nothing to send about player moves!!");
            isStopped = true;
        }
        
    }
    void IdleHandler()
    {
        idle_count += .01f;
        if (idle_count > 20f)
        {
            string idleAnimState;
            if (idle_1)
            {
                idleAnimState = IDLE_NOSE;
            }
            else
            {
                idleAnimState = IDLE_ASS;
            }
           // Debug.Log(idleAnimState);
            ChangeAnimationState(idleAnimState);
            if (NeedSend())
            {
                sioCom.Instance.Emit("ChangeAnimState", idleAnimState);
            }
            idle_1 = !idle_1;
            idle_count = 0f;
        }
    }

    void Update()
    {
        if (transform.position.y < -10f)
        {
            network.OnDisconnect();
        }
        if (connected)
        {
            SendPayload();
        }
        if (isStopped)
        {
            IdleHandler();
        }
        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            _input.cursorLocked = !_input.cursorLocked;
            _input.cursorInputForLook = !_input.cursorInputForLook;
        }
    }
    public void ChangeCursorLockState(bool value)
    {
        if (_input != null)
        {
            _input.cursorLocked = value;
            _input.cursorInputForLook = value;
        }
    }
    public void ChangeAnimationState(string newState)
    {
        if (currentAnimState == newState) return;

        _animator.Play(newState);
        currentAnimState = newState;
    }
}
