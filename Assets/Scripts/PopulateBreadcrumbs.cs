using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.Examples;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using UnityEngine.UI;

public class PopulateBreadcrumbs : MonoBehaviour
{
    [SerializeField] AbstractMap _map;

    [SerializeField]
    //[Geocode]
    string[] _locationStrings;

    Vector2d[] _locations;

    [SerializeField] float _spawnScale = 100f;

    [SerializeField] GameObject _markerPrefab;

    List<GameObject> _spawnedObjects;

    private AppMode _mode;

    private bool isListReady = false;

    void Start()
    {
        _mode = AppMode.WaitingForGps;
    }

    private enum AppMode
    {
        WaitingForGps,
        PopulatingMarkers,
        WaitingForMarkerChanges
    }

    void Update()
    {
        if (_mode == AppMode.WaitingForGps)
        {
            if (GPS.Instance.latitude != 0)
            {
                StartCoroutine(GetAnchors(GPS.Instance.latitude, GPS.Instance.longitude));
                _mode = AppMode.PopulatingMarkers;
            }
        }
        else if (_mode == AppMode.PopulatingMarkers)
        {
            if (isListReady)
            {
                _locations = new Vector2d[_locationStrings.Length];
                _spawnedObjects = new List<GameObject>();
                Debug.Log("_locationStrings array: " + _locationStrings.ToString());
                for (int i = 0; i < _locationStrings.Length; i++)
                {
                    var locationString = _locationStrings[i];
                    _locations[i] = Conversions.StringToLatLon(locationString);
                    var instance = Instantiate(_markerPrefab);
                    instance.transform.localPosition = _map.GeoToWorldPosition(_locations[i], true);
                    instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
                    _spawnedObjects.Add(instance);
                }

                _mode = AppMode.WaitingForMarkerChanges;
            }
            
        }
        else if (_mode == AppMode.WaitingForMarkerChanges)
        {
            int count = _spawnedObjects.Count;
            for (int i = 0; i < count; i++)
            {
                var spawnedObject = _spawnedObjects[i];
                var location = _locations[i];
                spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location, true);
                spawnedObject.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
            }
        }
    }

    IEnumerator GetAnchors(double latitude, double longitude)
    {
        string data = "?lat=" + latitude + "&long=" + longitude;
        using (UnityWebRequest www = UnityWebRequest.Get("https://breadcrumbsar.herokuapp.com/getAnchors" + data))
        {
            //www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            String responseString = www.downloadHandler.text;
            AnchorResponse response = JsonConvert.DeserializeObject<AnchorResponse>(responseString);
            AnchorList[] anchorList = response.AnchorList;

            _locationStrings = new string[anchorList.Length];
            for (var i = 0; i < anchorList.Length; i++)
            {
                _locationStrings[i] = anchorList[i].Latitude + ", " + anchorList[i].Longitude;
                Debug.Log(_locationStrings[i]);
            }
            
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Download complete!");
                isListReady = true;
                yield break;
            }
        }
    }
}