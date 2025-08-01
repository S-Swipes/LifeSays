using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MainGame : MonoBehaviour
{
   public List<FrogControl> frogControls = new List<FrogControl>(); 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < frogControls.Count; i++)
        {
            int temp = i;
            DOVirtual.DelayedCall(i + 1, () =>
            {
                frogControls[temp].Play();
            });
        }
       
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
