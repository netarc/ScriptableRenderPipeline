using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

using static UnityEditor.VFX.VFXAbstractRenderedOutput;
using static UnityEngine.Experimental.Rendering.HDPipeline.HDRenderQueue;

namespace UnityEditor.VFX
{
    class VFXHDRPSubOutput : VFXSRPSubOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Header("HDRP")]
        public OpaqueRenderQueue opaqueRenderQueue = OpaqueRenderQueue.Default;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public TransparentRenderQueue transparentRenderQueue = TransparentRenderQueue.Default;

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
