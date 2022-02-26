using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldHandler : MonoBehaviour
{
    public Camera worldMapCam;
    public List<WorldCities> worldCities = new List<WorldCities>();

    public GameObject cityPrefab;

    public Transform worldGround;


    public void UpdateWorldMap(WorldCities citys)
    {
        var rec = citys.city;
        //   Debug.Log("Build " + building);
        GameObject currentCity = Instantiate(cityPrefab, rec.worldLocation, Quaternion.identity) as GameObject;
        currentCity.transform.parent = transform;
        CityMapActions cityMapActions = currentCity.GetComponent<CityMapActions>();
        cityMapActions.UpdateCityValues(rec); 
    }
    public bool IsNewCity(string value)
    {
        foreach (WorldCities wCity in worldCities)
        {         
            if(wCity.city.ownerID == value)
            {
                Debug.Log("City exists!!!");
                return false;
            }
        }
      //  Debug.Log("City created!!!");
        return true;
    }
}