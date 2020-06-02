using System.Collections;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using Newtonsoft.Json;
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
        ResolveCloudAnchor,

        // Poll resolving point state until it is ready to use.
        WaitingForResolvedAnchor,

        //check if the queue is filled yet
        WaitingForQueue
    }

    private AppMode m_AppMode = AppMode.TouchToHostCloudAnchor;
    private ARCloudAnchor m_CloudAnchor;
    private string m_CloudAnchorId;
    private Queue<string> cloudAnchorIdQueue = new Queue<string>();
    private bool isQueueReady = false;

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

                cloudAnchorIdQueue.Enqueue(m_CloudAnchor.cloudAnchorId);
                StartCoroutine(saveAnchor(m_CloudAnchor.cloudAnchorId, GPS.Instance.latitude, GPS.Instance.longitude));
                m_CloudAnchor = null;

                m_AppMode = AppMode.ResolveCloudAnchor;
            }
        }
        else if (m_AppMode == AppMode.ResolveCloudAnchor)
        {
            OutputText.text = m_AppMode.ToString();
            //if there is a breadcrumb left to load
            if (cloudAnchorIdQueue.Count >= 1)
            {
                m_CloudAnchorId = cloudAnchorIdQueue.Dequeue();
                OutputText.text = m_CloudAnchorId;
                m_CloudAnchor =
                    AnchorManager.ResolveCloudAnchorId(
                        m_CloudAnchorId);

                if (m_CloudAnchor == null)
                {
                    OutputText.text = "Resolve Failed!";
                    m_CloudAnchorId = string.Empty;
                    //m_AppMode = AppMode.TouchToHostCloudAnchor;
                    return;
                }

                m_CloudAnchorId = string.Empty;

                // Wait for the reference point to be ready.
                m_AppMode = AppMode.WaitingForResolvedAnchor;
            }
            else
            {
                //if the queue is empty, return to placing anchors
                m_AppMode = AppMode.TouchToHostCloudAnchor;
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

                if (cloudAnchorIdQueue.Count <= 0)
                {
                    //if there are no more anchors to process, go back to placing anchors
                    m_AppMode = AppMode.TouchToHostCloudAnchor;
                }
                else
                {
                    //if there are more anchors to resolve, do that.
                    m_AppMode = AppMode.ResolveCloudAnchor;
                }
            }
        }
        else if (m_AppMode == AppMode.WaitingForQueue)
        {
            if (isQueueReady)
            {
                m_AppMode = AppMode.ResolveCloudAnchor;
                return;
            }
            else if (GPS.Instance.latitude != 0)
            {
                StartCoroutine(PopulateAnchorQueue(GPS.Instance.latitude, GPS.Instance.longitude));
            }
        }
    }

    void Start()
    {
        //InputField.onEndEdit.AddListener(OnInputEndEdit);
        m_AppMode = AppMode.WaitingForQueue;
    }

    void HostAnchor()
    {
    }

    void ResolveAnchor(string anchorId)
    {
    }

    IEnumerator saveAnchor(string anchorId, double latitude, double longitude)
    {
        Debug.Log("Started saveAnchor coroutine");
        string data = "{\"id\":\"" + anchorId + "\",\"lat\":\"" + latitude + "\",\"lon\":\"" + longitude + "\"}";
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

    IEnumerator PopulateAnchorQueue(double latitude, double longitude)
    {
        string data = "?lat=" + latitude + "&long=" + longitude;
        using (UnityWebRequest www = UnityWebRequest.Get("https://breadcrumbsar.herokuapp.com/getAnchors" + data))
        {
            //www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
            string responseString = www.downloadHandler.text;
            Debug.Log(responseString);

            AnchorResponse response = JsonConvert.DeserializeObject<AnchorResponse>(responseString);
            AnchorList[] anchorList = response.AnchorList;
            if (anchorList.Length == 0)
            {
                m_AppMode = AppMode.TouchToHostCloudAnchor;
                Debug.Log("did not find any anchors");
                yield break;
            }

            for (var i = 0; i < anchorList.Length; i++)
            {
                cloudAnchorIdQueue.Enqueue(anchorList[i].AnchorId);
                Debug.Log(anchorList[i].AnchorId);
            }

            isQueueReady = true;

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("Download complete!");
                yield break;
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