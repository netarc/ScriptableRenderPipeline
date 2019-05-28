using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOrder : MonoBehaviour
{
    public Camera cam1x = null;
    public Camera cam075x = null;
    public Camera cam05x = null;
    public Camera cam025x = null;
    public RenderTexture renderTarget = null;

    void Start()
    {

        
    }

    // Update is called once per frame
    void Update()
    {   
        if (cam1x != null || cam075x != null || cam05x != null 
            || cam025x != null || renderTarget != null)
        {   
            //Canera 1x
            ScalableBufferManager.ResizeBuffers(0.001f, 0.001f);
            cam1x.targetTexture = renderTarget;
            cam1x.Render();
            cam1x.targetTexture = null;
            
            //Camera 0.75x
            ScalableBufferManager.ResizeBuffers(0.75F, 0.75F);
            cam075x.targetTexture = renderTarget;
            cam075x.Render();
            cam075x.targetTexture = null;

            //Camera 0.5x
            ScalableBufferManager.ResizeBuffers(0.5F, 0.5F);
            cam05x.targetTexture = renderTarget;
            cam05x.Render();
            cam05x.targetTexture = null;

            //Camera 0.25x
            ScalableBufferManager.ResizeBuffers(0.25F, 0.25F);
            cam025x.targetTexture = renderTarget;
            cam025x.Render();
            cam025x.targetTexture = null;
        }   
    }
}
