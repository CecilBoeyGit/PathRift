using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;
using TMPro;
using Unity.XR;

// struct = class without functions
[System.Serializable]
public struct Gesture2
{
    public string name;
    public List<Vector3> fingerDatas;
}

public class GestureDetect : MonoBehaviour
{
    public Transform centerEye;

    public TextMeshProUGUI debugText;

    // Add the component that refer to the skeleton hand ("OVRCustomHandPrefab_R" or "OVRCustomHandPrefab_L")
    [Header("Hand Skeleton")]
    public OVRSkeleton skeleton;
    // public GameObject boneDebug, fingerCollider;
    // Transform[] fingerColliders;
    Transform[] debugTransforms;
    public bool boneDebugEnabled = false;

    // List of bones took from the OVRSkeleton
    public List<OVRBone> fingerbones = null;

    public bool hasStarted = false;


    [HideInInspector]
    public int indexNum, middleNum, ringNum, pinkyNum, thumbNum;

    public Vector2[] rotMin, rotMax, currentRot;
    public int[] fingerRotState;
    public float rotThresholdMax1 = 25f, rotThresholdMax2 = 25f, rotThresholdMin1 = 25f, rotThresholdMin2 = 25f;
    int[] fingerIndices = { 3, 7, 12, 17, 22 };
    public int fingerColliderNum = 1;
    Transform[] textTransforms;

    public bool calibrate = false, rightHand = false;
    //public UnityEvent indexRetracted, middleRetracted, ringRetracted, pinkyRetracted;

    // Add an event if you want to make happen when a gesture is not identified
    [Header("Not Recognized Event")]
    public UnityEvent notRecognize;

    void Start()
    {
        currentRot = new Vector2[5];
        fingerRotState = new int[5];

        StartCoroutine(SetUpBones());
    }

    
    public void SetSkeleton()
    {
        fingerbones = new List<OVRBone>(skeleton.Bones);
        // fingerColliders = new Transform[fingerColliderNum];

        // for (int i = 0; i < fingerColliders.Length; i++)
        // {
        //     GameObject fingerCol = Instantiate(fingerCollider);
        //     fingerColliders[i] = fingerCol.transform;
        // }

        if (boneDebugEnabled)
        {
            SetUpBoneDebug();
        }
        
        // interactionBall = Instantiate(interactor, fingerbones[20].Transform.position, fingerbones[20].Transform.rotation);
        // interactionBall.transform.parent = fingerbones[20].Transform;
    }

    void SetUpBoneDebug()
    {
        for (int i = 0; i < fingerbones.Count; i++)
        {
            debugTransforms = new Transform[fingerbones.Count];
            // GameObject debug = Instantiate(boneDebug, fingerbones[i].Transform.position, fingerbones[i].Transform.rotation);
            // debug.transform.parent = fingerbones[i].Transform;
            // debugTransforms[i] = debug.transform;
            debugTransforms[i].GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = "" + i;
        }
    }

    void UpdateDebugRot()
    {
        for (int i = 0; i < debugTransforms.Length; i++)
        {
            debugTransforms[i].rotation = Quaternion.LookRotation(centerEye.position - debugTransforms[i].position);
        }
    }

    IEnumerator SetUpBones()
    {
        while (!skeleton.IsInitialized)
        {
            yield return null;
        }

        SetSkeleton();
        hasStarted = true;
        
    }


    void GetRot()
    {

        for (int i = 0; i < 5; i++)
        {
            if (i == 0)
            {
                currentRot[i] = new Vector2(fingerbones[fingerIndices[i]].Transform.localEulerAngles.x, fingerbones[fingerIndices[i]+1].Transform.localEulerAngles.x);
            }
            else
            {
                currentRot[i] = new Vector2(fingerbones[fingerIndices[i]].Transform.localEulerAngles.x, fingerbones[fingerIndices[i]+1].Transform.localEulerAngles.x);
            }

            float angleOffset1 = currentRot[i].x, angleOffset2 = currentRot[i].y;

            if (currentRot[i].x < 120)
            {
                angleOffset1 += 360;
            }
            if (currentRot[i].y < 120)
            {
                angleOffset2 += 360;
            }

            currentRot[i] = new Vector2(angleOffset1, angleOffset2);
        }
        
    }

    // void CalibrateThresholds()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space) && calibrate)
    //     {
    //         rotMax = new Vector2[5];
    //         for (int i = 0; i < currentRot.Length; i++)
    //         {
    //             rotMax[i] = currentRot[i];
    //         }
    //     }
    //     else if (Input.GetKeyDown(KeyCode.LeftAlt) && calibrate)
    //     {
    //         rotMin = new Vector2[5];
    //         for (int i = 0; i < currentRot.Length; i++)
    //         {
    //             rotMin[i] = currentRot[i];
    //         }
    //     }
    // }

    void Update()
    {
        if (hasStarted.Equals(true))
        {
            //CalibrateThresholds();
            GetRot();
            FingerRotationStates();

            if (boneDebugEnabled)
            {
                UpdateDebugRot();
            }

            // for (int i = 0; i < fingerColliders.Length; i++)
            // {
            //     fingerColliders[i].position = fingerbones[10 + i*5].Transform.position;
            // }
        }
    }

    void FingerRotationStates()
    {
        for (int i = 0; i < currentRot.Length; i++)
        {
            if (currentRot[i].x < rotMax[i].x + rotThresholdMax1 && currentRot[i].y < rotMax[i].y + rotThresholdMax2)
            {
                fingerRotState[i] = 2;
            }
            else if (currentRot[i].x > rotMin[i].x - rotThresholdMin1 && currentRot[i].y > rotMin[i].y - rotThresholdMin2)
            {
                fingerRotState[i] = 0;
            }
            else
            {
                fingerRotState[i] = 1;
            }
        }

        //debugText.text = "Index 0:" + Mathf.RoundToInt(currentRot[0].x) + ", " + Mathf.RoundToInt(currentRot[0].y) + " rotState: " + fingerRotState[0] + "\n" +
        // "Index 1:" + Mathf.RoundToInt(currentRot[1].x) + ", " + Mathf.RoundToInt(currentRot[1].y) + " rotState: " + fingerRotState[1] + "\n" + 
        // "Index 2:" + Mathf.RoundToInt(currentRot[2].x) + ", " + Mathf.RoundToInt(currentRot[2].y) + " rotState: " + fingerRotState[2] + "\n" + 
        // "Index 3:" + Mathf.RoundToInt(currentRot[3].x) + ", " + Mathf.RoundToInt(currentRot[3].y) + " rotState: " + fingerRotState[3] + "\n" +
        // "Index 4:" + Mathf.RoundToInt(currentRot[4].x) + ", " + Mathf.RoundToInt(currentRot[4].y) + " rotState: " + fingerRotState[4] + "\n";
    }
}
