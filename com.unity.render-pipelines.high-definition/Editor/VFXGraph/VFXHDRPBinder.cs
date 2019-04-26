using System;
using UnityEditor.VFX;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.VFX.HDRP
{
    class VFXHDRPBinder : VFXSRPBinder
    {
        public override string templatePath { get { return "Packages/com.unity.visualeffectgraph/Shaders/RenderPipeline/HDRP"; } }
        public override Type SRPAssetType { get { return typeof(HDRenderPipelineAsset); } }

        public override VFXModel CreateSRPOutputData() { return null; } // TODO
    }
}
