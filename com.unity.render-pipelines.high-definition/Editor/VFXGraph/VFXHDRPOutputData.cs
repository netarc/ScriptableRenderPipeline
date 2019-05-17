using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.VFX
{
    class VFXHDRPOutputData : VFXSRPOutputData
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("HDRP")]
        public HDRenderQueue.OpaqueRenderQueue opaqueRenderQueue;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public HDRenderQueue.TransparentRenderQueue transparentRenderQueue;
    }
}
