using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditorForRenderPipeline(typeof(Light), typeof(HDRenderPipelineAsset))]
    sealed partial class HDLightEditor : LightEditor
    {
        // LightType + LightTypeExtent combined
        internal enum LightShape
        {
            Spot,
            Directional,
            Point,
            //Area, <= offline base type not displayed in our case but used for GI of our area light
            Rectangle,
            Tube,
            //Sphere,
            //Disc,
        }

        internal enum DirectionalLightUnit
        {
            Lux = LightUnit.Lux,
        }

        internal enum AreaLightUnit
        {
            Lumen = LightUnit.Lumen,
            Luminance = LightUnit.Luminance,
            Ev100 = LightUnit.Ev100,
        }

        internal enum PunctualLightUnit
        {
            Lumen = LightUnit.Lumen,
            Candela = LightUnit.Candela,
        }

        const float k_MinLightSize = 0.01f; // Provide a small size of 1cm for line light

        // Used for UI only; the processing code must use LightTypeExtent and LightType
        LightShape m_LightShape;
        
        public SerializedHDLight m_SerializedHDLight;

        HDAdditionalLightData[] m_AdditionalLightDatas;
        AdditionalShadowData[] m_AdditionalShadowDatas;

        bool m_UpdateAreaLightEmissiveMeshComponents = false;

        HDShadowInitParameters                m_HDShadowInitParameters;
        Dictionary<HDShadowQuality, Action>   m_ShadowAlgorithmUIs;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Get & automatically add additional HD data if not present
            m_AdditionalLightDatas = CoreEditorUtils.GetAdditionalData<HDAdditionalLightData>(targets, HDAdditionalLightData.InitDefaultHDAdditionalLightData);
            m_AdditionalShadowDatas = CoreEditorUtils.GetAdditionalData<AdditionalShadowData>(targets, HDAdditionalShadowData.InitDefaultHDAdditionalShadowData);
            m_SerializedHDLight = new SerializedHDLight(m_AdditionalLightDatas, m_AdditionalShadowDatas, settings);
            
            // Update emissive mesh and light intensity when undo/redo
            Undo.undoRedoPerformed += () => {
                m_SerializedHDLight.serializedLightDatas.ApplyModifiedProperties();
                foreach (var hdLightData in m_AdditionalLightDatas)
                    if (hdLightData != null)
                        hdLightData.UpdateAreaLightEmissiveMesh();
            };

            // If the light is disabled in the editor we force the light upgrade from his inspector
            foreach (var additionalLightData in m_AdditionalLightDatas)
                additionalLightData.UpgradeLight();

            m_HDShadowInitParameters = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineSettings.hdShadowInitParams;
            m_ShadowAlgorithmUIs = new Dictionary<HDShadowQuality, Action>
            {
                {HDShadowQuality.Low, DrawLowShadowSettings},
                {HDShadowQuality.Medium, DrawMediumShadowSettings},
                {HDShadowQuality.High, DrawHighShadowSettings}
            };
        }

        public override void OnInspectorGUI()
        {
            m_SerializedHDLight.Update();

            //add space before the first collapsible area
            EditorGUILayout.Space();

            // Disable the default light editor for the release, it is just use for development
            /*
            // Temporary toggle to go back to the old editor & separated additional datas
            bool useOldInspector = m_AdditionalLightData.useOldInspector.boolValue;

            if (GUILayout.Button("Toggle default light editor"))
                useOldInspector = !useOldInspector;

            m_AdditionalLightData.useOldInspector.boolValue = useOldInspector;

            if (useOldInspector)
            {
                DrawDefaultInspector();
                ApplyAdditionalComponentsVisibility(false);
                m_SerializedAdditionalShadowData.ApplyModifiedProperties();
                m_SerializedAdditionalLightData.ApplyModifiedProperties();
                return;
            }
            */

            // New editor
            ApplyAdditionalComponentsVisibility(true);

            ResolveLightShape();

            DrawInspector();

            m_SerializedHDLight.Apply();

            if (m_SerializedHDLight.needUpdateAreaLightEmissiveMeshComponents)
                UpdateAreaLightEmissiveMeshComponents();
        }

        void UpdateAreaLightEmissiveMeshComponents()
        {
            foreach (var hdLightData in m_AdditionalLightDatas)
            {
                hdLightData.UpdateAreaLightEmissiveMesh();

                MeshRenderer emissiveMeshRenderer = hdLightData.GetComponent<MeshRenderer>();
                MeshFilter emissiveMeshFilter = hdLightData.GetComponent<MeshFilter>();

                // If the display emissive mesh is disabled, skip to the next selected light
                if (emissiveMeshFilter == null || emissiveMeshRenderer == null)
                    continue;

                // We only load the mesh and it's material here, because we can't do that inside HDAdditionalLightData (Editor assembly)
                // Every other properties of the mesh is updated in HDAdditionalLightData to support timeline and editor records
                emissiveMeshFilter.mesh = UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils.LoadAsset<Mesh>("Runtime/RenderPipelineResources/Mesh/Quad.FBX");
                if (emissiveMeshRenderer.sharedMaterial == null)
                    emissiveMeshRenderer.material = new Material(Shader.Find("HDRenderPipeline/Unlit"));
            }

            m_SerializedHDLight.needUpdateAreaLightEmissiveMeshComponents = false;
        }

        // Internal utilities
        void ApplyAdditionalComponentsVisibility(bool hide)
        {
            // UX team decided that we should always show component in inspector.
            // However already authored scene save this settings, so force the component to be visible
            // var flags = hide ? HideFlags.HideInInspector : HideFlags.None;
            var flags = HideFlags.None;

            foreach (var t in m_SerializedHDLight.serializedLightDatas.targetObjects)
                ((HDAdditionalLightData)t).hideFlags = flags;

            foreach (var t in m_SerializedHDLight.serializedShadowDatas.targetObjects)
                ((AdditionalShadowData)t).hideFlags = flags;
        }

        void ResolveLightShape()
        {
            var type = settings.lightType;

            // Special case for multi-selection: don't resolve light shape or it'll corrupt lights
            if (type.hasMultipleDifferentValues
                || m_SerializedHDLight.serializedLightData.lightTypeExtent.hasMultipleDifferentValues)
            {
                m_LightShape = (LightShape)(-1);
                return;
            }

            var lightTypeExtent = (LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex;

            if (lightTypeExtent == LightTypeExtent.Punctual)
            {
                switch ((LightType)type.enumValueIndex)
                {
                    case LightType.Directional:
                        m_LightShape = LightShape.Directional;
                        break;
                    case LightType.Point:
                        m_LightShape = LightShape.Point;
                        break;
                    case LightType.Spot:
                        m_LightShape = LightShape.Spot;
                        break;
                }
            }
            else
            {
                switch (lightTypeExtent)
                {
                    case LightTypeExtent.Rectangle:
                        m_LightShape = LightShape.Rectangle;
                        break;
                    case LightTypeExtent.Tube:
                        m_LightShape = LightShape.Tube;
                        break;
                }
            }

        }
    }
}
