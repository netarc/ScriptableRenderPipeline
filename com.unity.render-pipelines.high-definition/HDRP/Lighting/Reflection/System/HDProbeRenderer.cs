using System;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    class HDProbeRenderer
    {
        struct CommonRenderer
        {
            public static void SetupCaptureCameraSettings(
                Camera camera,
                HDProbe.CaptureProperties captureProperties
            )
            {
                camera.farClipPlane = captureProperties.farClipPlane;
                camera.nearClipPlane = captureProperties.nearClipPlane;
                camera.fieldOfView = captureProperties.fieldOfview;

                var add = camera.GetComponent<HDAdditionalCameraData>();
                add.Import(captureProperties.cameraSettings);
            }

            public static void SetupCaptureCameraTransform(
                Camera camera,
                HDProbe probe,
                Vector3 viewerPosition, Quaternion viewerRotation
            )
            {
                Vector3 position; Quaternion rotation;
                probe.GetCaptureTransformFor(
                    viewerPosition, viewerRotation,
                    out position, out rotation
                );
                camera.transform.position = position;
                camera.transform.rotation = rotation;
            }
        }

        interface IProbeRenderer<T>
            where T : HDProbe
        {
            bool Render(T probe, Texture target, Transform viewer);
        }

        struct ReflectionProbeRenderer : IProbeRenderer<HDAdditionalReflectionData>
        {
            public bool Render(HDAdditionalReflectionData probe, Texture target, Transform viewer)
            {
                var cubemapTarget = target as Cubemap;
                var rtTarget = target as RenderTexture;
                if (cubemapTarget == null
                    && (rtTarget == null || rtTarget.dimension != UnityEngine.Rendering.TextureDimension.Cube))
                {
                    Debug.LogWarningFormat("Trying to render a reflection probe in an invalid target: {0}", target);
                    return false;
                }

                var camera = NewCamera(probe.assets.captureFrameSettings, probe.assets.postProcessLayer);

                SetupCaptureCamera(camera, probe, rtTarget, viewer);

                if (cubemapTarget != null)
                    camera.RenderToCubemap(cubemapTarget);
                else if (rtTarget != null)
                    camera.RenderToCubemap(rtTarget);

                CoreUtils.Destroy(camera.gameObject);

                return true;
            }

            void SetupCaptureCamera(
                Camera camera,
                HDAdditionalReflectionData probe,
                RenderTexture target,
                Transform viewer
            )
            {
                camera.aspect = target.width / (float)target.height;

                CommonRenderer.SetupCaptureCameraSettings(camera, probe.captureSettings.common);
                CommonRenderer.SetupCaptureCameraTransform(camera, probe, viewer.position, viewer.rotation);
            }
        }

        ReflectionProbeRenderer m_ReflectionProbeRenderer = new ReflectionProbeRenderer();

        public bool Render(HDProbe probe, Texture target, Transform viewer)
        {
            var standard = probe as HDAdditionalReflectionData;
            var planar = probe as PlanarReflectionProbe;
            if (standard != null)
                return m_ReflectionProbeRenderer.Render(standard, target, viewer);
            if (planar != null)
                throw new NotImplementedException();
            return false;
        }

        static Camera NewCamera(FrameSettings frameSettings, PostProcessLayer postLayer)
        {
            var go = new GameObject("__Probe Camera");
            var camera = go.AddComponent<Camera>();
            var add = go.AddComponent<HDAdditionalCameraData>();

            frameSettings.CopyTo(add.GetFrameSettings());

            if (postLayer != null)
            {
                // TODO: copy post process settings when initializing the capture camera.
                //var layer = go.AddComponent<PostProcessLayer>();
                //EditorUtility.CopySerialized(postLayer, layer);
            }

            return camera;
        }
    }
}
