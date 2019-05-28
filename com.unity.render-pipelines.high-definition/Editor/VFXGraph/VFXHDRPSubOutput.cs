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

        public override string GetRenderQueueStr()
        {
            RenderQueueType renderQueueType;
            string prefix = string.Empty;
            if (owner.isBlendModeOpaque)
            {
                prefix = "Opaque";
                renderQueueType = HDRenderQueue.ConvertFromOpaqueRenderQueue(opaqueRenderQueue);
            }
            else
            {
                prefix = "Transparent";
                renderQueueType = HDRenderQueue.ConvertFromTransparentRenderQueue(transparentRenderQueue);
            }

            int renderQueue = HDRenderQueue.ChangeType(renderQueueType, 0, owner.blendMode == BlendMode.Masked) - (int)(owner.isBlendModeOpaque ? Priority.Opaque : Priority.Transparent);
            return prefix + renderQueue.ToString("+#;-#;+0");
        }
    }
}
