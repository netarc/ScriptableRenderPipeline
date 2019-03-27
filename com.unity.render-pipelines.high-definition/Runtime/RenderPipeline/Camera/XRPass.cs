// This file contain the two main data structures controlled by the XRSystem.
// XRView contains the parameters required to render (proj and view matrices, viewport, etc)
// XRPass holds the render target information and a list of XRView.
// When a pass has 2+ views, hardware instancing will be active.
// To avoid allocating every frame, XRView is a struct and XRPass is pooled.

// XRTODO(2019.3) Deprecate legacy code
// XRTODO(2020.1) Remove legacy code
#if UNITY_2019_2_OR_NEWER
    #define USE_XR_SDK
#endif

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
#if USE_XR_SDK
using UnityEngine.Experimental.XR;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    internal struct XRView
    {
        internal readonly Matrix4x4 projMatrix;
        internal readonly Matrix4x4 viewMatrix;
        internal readonly Rect viewport;
        internal readonly Mesh occlusionMesh;
        internal readonly Camera.StereoscopicEye legacyStereoEye;

        internal XRView(Camera camera, Camera.StereoscopicEye eye)
        {
            projMatrix = camera.GetStereoProjectionMatrix(eye);
            viewMatrix = camera.GetStereoViewMatrix(eye);
            viewport = camera.pixelRect;
            occlusionMesh = null;
            legacyStereoEye = eye;
        }

#if USE_XR_SDK
        internal XRView(XRDisplaySubsystem.XRRenderParameter renderParameter)
        {
            projMatrix = renderParameter.projection;
            viewMatrix = renderParameter.view;
            viewport = renderParameter.viewport;
            occlusionMesh = renderParameter.occlusionMesh;
            legacyStereoEye = (Camera.StereoscopicEye)(-1);
        }
#endif
    }

    public class XRPass
    {
        private readonly List<XRView> views = new List<XRView>(2);

        internal bool enabled { get => views.Count > 0; }
        internal bool xrSdkEnabled { get; private set; }

        internal int passId { get; private set; }
        internal int cullingPassId  { get; private set; }

        // Ability to specify where to render the pass
        internal RenderTargetIdentifier  renderTarget     { get; private set; }
        internal RenderTextureDescriptor renderTargetDesc { get; private set; }
        static RenderTargetIdentifier    invalidRT = -1;
        internal bool                    renderTargetValid { get => renderTarget != invalidRT; }

        // Access to view information
        internal Matrix4x4 GetProjMatrix(int viewIndex = 0) { return views[viewIndex].projMatrix; }
        internal Matrix4x4 GetViewMatrix(int viewIndex = 0) { return views[viewIndex].viewMatrix; }
        internal Rect GetViewport(int viewIndex = 0)        { return views[viewIndex].viewport; }

        // Instanced views support (instanced draw calls or multiview extension)
        internal int viewCount { get => views.Count; }
        internal bool instancingEnabled { get => viewCount > 1; }

        // XRTODO(2019.3) : remove once XRE-445 is done
        // We need an intermediate target to render the mirror view
        public RenderTexture tempRenderTexture { get; private set; } = null;

        // Legacy multipass support
        internal int  legacyMultipassEye      { get => (int)views[0].legacyStereoEye; }
        internal bool legacyMultipassEnabled  { get => enabled && !instancingEnabled && legacyMultipassEye >= 0; }

        internal static XRPass Create(int passId)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.passId = passId;
            passInfo.cullingPassId = -1;
            passInfo.views.Clear();
            passInfo.renderTarget = invalidRT;
            passInfo.renderTargetDesc = default;
            passInfo.xrSdkEnabled = false;
            passInfo.tempRenderTexture = null;

            return passInfo;
        }

        internal void AddView(Camera camera, Camera.StereoscopicEye eye)
        {
            AddViewInternal(new XRView(camera, eye));
        }

#if USE_XR_SDK
        internal static XRPass Create(XRDisplaySubsystem.XRRenderPass xrRenderPass)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.passId = xrRenderPass.renderPassIndex;
            passInfo.cullingPassId = xrRenderPass.cullingPassIndex;
            passInfo.views.Clear();
            passInfo.renderTarget = xrRenderPass.renderTarget;
            passInfo.renderTargetDesc = xrRenderPass.renderTargetDesc;
            passInfo.xrSdkEnabled = true;

            Debug.Assert(passInfo.renderTargetValid, "Invalid render target from XRDisplaySubsystem!");

            // XRTODO(2019.3) : remove once XRE-445 is done
            {
                // Local function to check if 2 descriptors are similar.
                // Note: we can't use the base Equals because the flags are different (CreatedFromScript)
                bool Equals(RenderTextureDescriptor descA, RenderTextureDescriptor descB)
                {
                    return descA.width == descB.width && descA.height == descB.height && descA.colorFormat == descB.colorFormat;
                }

                if (passInfo.tempRenderTexture == null || !Equals(passInfo.tempRenderTexture.descriptor, xrRenderPass.renderTargetDesc))
                {
                    if (passInfo.tempRenderTexture != null)
                        passInfo.tempRenderTexture.Release();

                    passInfo.tempRenderTexture = new RenderTexture(xrRenderPass.renderTargetDesc);
                    passInfo.tempRenderTexture.Create();
                }
            }

            return passInfo;
        }

        // XRTODO(2019.3) : remove once XRE-445 is done
        ~XRPass()
        {
            if (tempRenderTexture != null)
                tempRenderTexture.Release();
        }

        internal void AddView(XRDisplaySubsystem.XRRenderParameter xrSdkRenderParameter)
        {
            AddViewInternal(new XRView(xrSdkRenderParameter));
        }
#endif
        internal static void Release(XRPass xrPass)
        {
            GenericPool<XRPass>.Release(xrPass);
        }

        private void AddViewInternal(XRView xrView)
        {
            views.Add(xrView);

            // Validate memory limitations
            Debug.Assert(views.Count <= TextureXR.kMaxSliceCount);
        }

        internal void StartLegacyStereo(Camera camera, CommandBuffer cmd, ScriptableRenderContext renderContext)
        {
            if (enabled && camera.stereoEnabled)
            {
                // Reset scissor and viewport for C++ stereo code
                cmd.DisableScissorRect();
                cmd.SetViewport(camera.pixelRect);

                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (legacyMultipassEnabled)
                    renderContext.StartMultiEye(camera, legacyMultipassEye);
                else
                    renderContext.StartMultiEye(camera);
            }
        }

        internal void StopLegacyStereo(Camera camera, CommandBuffer cmd, ScriptableRenderContext renderContext)
        {
            if (enabled && camera.stereoEnabled)
            {
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                renderContext.StopMultiEye(camera);
            }
        }

        internal void EndCamera(HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!enabled)
                return;

            if (xrSdkEnabled)
            {
                // XRTODO(2019.3) : remove once XRE-445 is done
                if (tempRenderTexture && hdCamera.camera.targetTexture == null)
                {
                    // Multipass only for now
                    if (viewCount == 1)
                    {
                        // Blit to device
                        cmd.SetRenderTarget(renderTarget);
                        cmd.SetViewport(hdCamera.finalViewport);
                        HDUtils.BlitQuad(cmd, tempRenderTexture, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), 0, false);

                        // Mirror view (only works with stereo for now)
                        if (passId < 2)
                        {
                            cmd.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
                            cmd.SetViewport(hdCamera.camera.pixelRect);

                            Vector4 scaleBiasRT = new Vector4(0.5f, 1, passId * 0.5f, 0);
                            HDUtils.BlitQuad(cmd, tempRenderTexture, new Vector4(1, 1, 0, 0), scaleBiasRT, 0, true);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            else
            {
                renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Pushes to XR headset and/or display mirror
                if (legacyMultipassEnabled)
                    renderContext.StereoEndRender(hdCamera.camera, legacyMultipassEye, legacyMultipassEye == 1);
                else
                    renderContext.StereoEndRender(hdCamera.camera);
            }
        }
    }
}
