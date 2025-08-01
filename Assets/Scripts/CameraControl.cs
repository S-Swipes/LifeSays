using DG.Tweening;
using UnityEngine;
using Unity.Cinemachine;

public class CameraControl : MonoBehaviour
{
    private CinemachineMixingCamera mixingCamera;
    private int activeIndex = 0;
    void Start()
    {
        mixingCamera = GetComponent<CinemachineMixingCamera>();

        // Optional: Reset weights at start
        for (int i = 0; i < mixingCamera.ChildCameras.Count; i++)
        {
            float weight = (i == activeIndex) ? 1f : 0f;
            mixingCamera.SetWeight(i, weight);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            SetWeights(0);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {   
            SetWeights(1);
        }
        
        if (Input.GetKeyDown(KeyCode.D))
        {
            SetWeights(2);
        }
    }

    private void SetWeights(int newIndex)
    {
        if (newIndex ==  activeIndex)
        {
            return;
        }
        
        int outIndex = activeIndex;
        activeIndex = newIndex;
        DOVirtual.Float(0, 1,2, (weight) =>
        {
            mixingCamera.SetWeight(newIndex, weight);
            mixingCamera.SetWeight(outIndex, 1- weight);
        }).SetEase(Ease.Linear);
    }
}