using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using static UnityEditor.VFX.VFXAbstractRenderedOutput;

namespace UnityEditor.VFX
{
    class VFXHDRPOutputData : VFXSRPOutputData
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("HDRP")]
        public HDRenderQueue.OpaqueRenderQueue opaqueRenderQueue;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public HDRenderQueue.TransparentRenderQueue transparentRenderQueue;

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (owner.isBlendModeOpaque)
                    yield return "transparentRenderQueue";
                else
                    yield return "opaqueRenderQueue";
            }
        }
    }
}
