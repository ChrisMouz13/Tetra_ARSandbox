﻿using System.Collections;
using System.Collections.Generic;
using Orbbec;
using OrbbecUnity;
using UnityEngine;
using UnityEngine.UI;

public class IRLeftImageView : MonoBehaviour
{
    public OrbbecFrameSource frameSource;
    
    private Texture2D irTexture;
    

    void Update()
    {
        var obIrFrame = frameSource.GetIrLeftFrame();

        if(obIrFrame == null || obIrFrame.width == 0 || obIrFrame.height == 0 || obIrFrame.data == null || obIrFrame.data.Length == 0)
        {
            return;
        }
        if(obIrFrame.frameType != FrameType.OB_FRAME_IR_LEFT)
        {
            return;
        }
        if(irTexture == null)
        {
            if(obIrFrame.format == Format.OB_FORMAT_Y8)
            {
                irTexture = new Texture2D(obIrFrame.width, obIrFrame.height, TextureFormat.R8, false);
            }
            else
            {
                irTexture = new Texture2D(obIrFrame.width, obIrFrame.height, TextureFormat.RG16, false);
            }
            GetComponent<Renderer>().material.mainTexture = irTexture;
        }
        if(irTexture.width != obIrFrame.width || irTexture.height != obIrFrame.height)
        {
            irTexture.Reinitialize(obIrFrame.width, obIrFrame.height);
        }
        irTexture.LoadRawTextureData(obIrFrame.data);
        irTexture.Apply();
    }
}