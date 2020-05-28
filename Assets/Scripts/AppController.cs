﻿using System.Collections;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class AppController : MonoBehaviour
{
    public GameObject HostedPointPrefab;
    public GameObject ResolvedPointPrefab;
    public ARAnchorManager AnchorManager;
    public ARRaycastManager RaycastManager;
    public InputField InputField;
    public Text OutputText;

    private enum AppMode
    {
        // Wait for user to tap screen to begin hosting a point.
        TouchToHostCloudAnchor,

        // Poll hosted point state until it is ready to use.
        WaitingForHostedAnchor,

        // Wait for user to tap screen to begin resolving the point.
        TouchToResolveCloudAnchor,

        // Poll resolving point state until it is ready to use.
        WaitingForResolvedAnchor,
    }

    private AppMode m_AppMode = AppMode.TouchToHostCloudAnchor;
    private ARCloudAnchor m_CloudAnchor;
    private string m_CloudAnchorId;

    void Update()
    {
        if (m_AppMode == AppMode.TouchToHostCloudAnchor)
        {
            OutputText.text = m_AppMode.ToString();

            if (Input.touchCount >= 1
                && Input.GetTouch(0).phase == TouchPhase.Began
                && !EventSystem.current.IsPointerOverGameObject(
                        Input.GetTouch(0).fingerId))
            {
                List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
                RaycastManager.Raycast(Input.GetTouch(0).position, hitResults);
                if (hitResults.Count > 0)
                {
                    Pose pose = hitResults[0].pose;

                    // Create a reference point at the touch.
                    ARAnchor anchor =
                        AnchorManager.AddAnchor(
                            hitResults[0].pose);

                    // Create Cloud Reference Point.
                    m_CloudAnchor =
                        AnchorManager.HostCloudAnchor(
                            anchor);
                    if (m_CloudAnchor == null)
                    {
                        OutputText.text = "Create Failed!";
                        return;
                    }

                    // Wait for the reference point to be ready.
                    m_AppMode = AppMode.WaitingForHostedAnchor;
                }
            }
        }
        else if (m_AppMode == AppMode.WaitingForHostedAnchor)
        {
            OutputText.text = m_AppMode.ToString();

            CloudAnchorState cloudAnchorState =
                m_CloudAnchor.cloudAnchorState;
            OutputText.text += " - " + cloudAnchorState.ToString();

            if (cloudAnchorState == CloudAnchorState.Success)
            {
                GameObject cloudAnchor = Instantiate(
                                             HostedPointPrefab,
                                             Vector3.zero,
                                             Quaternion.identity);
                cloudAnchor.transform.SetParent(
                    m_CloudAnchor.transform, false);

                m_CloudAnchorId = m_CloudAnchor.cloudAnchorId;
                m_CloudAnchor = null;
                
                StartCoroutine(saveAnchor(m_CloudAnchorId, 1.0, 1.0));

                m_AppMode = AppMode.TouchToResolveCloudAnchor;
            }
        }
        else if (m_AppMode == AppMode.TouchToResolveCloudAnchor)
        {
            OutputText.text = m_CloudAnchorId;

            if (Input.touchCount >= 1
                && Input.GetTouch(0).phase == TouchPhase.Began
                && !EventSystem.current.IsPointerOverGameObject(
                        Input.GetTouch(0).fingerId))
            {
                m_CloudAnchor =
                    AnchorManager.ResolveCloudAnchorId(
                        m_CloudAnchorId);
                if (m_CloudAnchor == null)
                {
                    OutputText.text = "Resolve Failed!";
                    m_CloudAnchorId = string.Empty;
                    m_AppMode = AppMode.TouchToHostCloudAnchor;
                    return;
                }

                m_CloudAnchorId = string.Empty;

                // Wait for the reference point to be ready.
                m_AppMode = AppMode.WaitingForResolvedAnchor;
            }
        }
        else if (m_AppMode == AppMode.WaitingForResolvedAnchor)
        {
            OutputText.text = m_AppMode.ToString();

            CloudAnchorState cloudAnchorState =
                m_CloudAnchor.cloudAnchorState;
            OutputText.text += " - " + cloudAnchorState.ToString();

            if (cloudAnchorState == CloudAnchorState.Success)
            {
                GameObject cloudAnchor = Instantiate(
                                             ResolvedPointPrefab,
                                             Vector3.zero,
                                             Quaternion.identity);
                cloudAnchor.transform.SetParent(
                    m_CloudAnchor.transform, false);

                m_CloudAnchor = null;

                m_AppMode = AppMode.TouchToHostCloudAnchor;
            }
        }
    }

    void Start()
    {
        InputField.onEndEdit.AddListener(OnInputEndEdit);
    }
    
    IEnumerator saveAnchor(string anchorId, double lattitude, double longitude )
    {
        string data = "{\"id\":\"" + anchorId + "\",\"lat\":\"" + lattitude + "\",\"lon\":\"" + longitude+ "\"}";
        using (UnityWebRequest www = UnityWebRequest.Put("https://breadcrumbsar.herokuapp.com/saveAnchor", data))
        {
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Upload complete!");
            }
        }
    }

    private void OnInputEndEdit(string text)
    {
        m_CloudAnchorId = string.Empty;

        m_CloudAnchor =
            AnchorManager.ResolveCloudAnchorId(text);
        if (m_CloudAnchor == null)
        {
            OutputText.text = "Resolve Failed!";
            m_AppMode = AppMode.TouchToHostCloudAnchor;
            return;
        }

        // Wait for the reference point to be ready.
        m_AppMode = AppMode.WaitingForResolvedAnchor;
    }
}