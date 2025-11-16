using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TestGameFlow : MonoBehaviour
{
    public GameObject path, startGameSpherePivot, startGameSphere, pinchDetect;
    public Transform playerHead, trackingSpace;
    public UnityEvent startGame;
    float startHeight = -0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SetHeight());
    }

    IEnumerator SetHeight()
    {
        while (playerHead.localPosition == Vector3.zero)
        {
            yield return null;
        }

        startHeight = trackingSpace.position.y;
        StartCoroutine (SetPath());
    }

    IEnumerator SetPath()
    {
        while ((playerHead.position.y - startHeight) > 0.4f)
        {
            yield return null;
        }

        path.SetActive(false);
        startGameSpherePivot.SetActive(true);
        StartCoroutine(RotateGameStartSphere());
    }

    // Update is called once per frame
    void Update()
    {
        // if (path.activeInHierarchy && (playerHead.position.y - startHeight) < 0.4f)
        // {
        //     path.SetActive(false);
        //     startGameSpherePivot.SetActive(true);
        //     StartCoroutine(RotateGameStartSphere());
        // }
    }

    IEnumerator RotateGameStartSphere()
    {
        while (startGameSphere.activeInHierarchy)
        {
            startGameSpherePivot.transform.position = playerHead.position - new Vector3(0, 0.4f, 0);
            Quaternion pivotRot = playerHead.rotation;
            pivotRot.x = 0;
            pivotRot.z = 0;
            startGameSpherePivot.transform.rotation = Quaternion.RotateTowards(startGameSpherePivot.transform.rotation, pivotRot, 200f * Time.deltaTime);
            
            yield return null;
        }

        path.SetActive(true);
        startGameSpherePivot.SetActive(false);
        pinchDetect.SetActive(false);

        startGame?.Invoke();
    }
}
