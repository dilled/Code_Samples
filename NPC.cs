using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New NPC", menuName = "Characters/NPC")]
public class NPC : ScriptableObject
{
    public NPCData npcData;
}

[System.Serializable]
public class NPCData
{
    public string id;
    public string name = "New Character";
    public int level;
    public float health;
    public int speed;
    public int strength;
    public Vector3 location;
    public Quaternion rotation;
    public Vector3 target;
    public int npcPrefab;
    public bool workAnimState;

    
    public virtual void ChangeHealth()
    {
        Debug.Log("ChangeHealth" + name);
    }
    

}