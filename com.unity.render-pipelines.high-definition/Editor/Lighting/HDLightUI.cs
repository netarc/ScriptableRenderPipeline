using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<HDLightUI, SerializedHDLight>;
    using LightShape = HDLightEditor.LightShape;
    using DirectionalLightUnit = HDLightEditor.DirectionalLightUnit;
    using AreaLightUnit = HDLightEditor.AreaLightUnit;
    using PunctualLightUnit = HDLightEditor.PunctualLightUnit;

    partial class HDLightUI
    {
        static void DrawInspector(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            DrawFoldout(serialized.serializedLightData.showFeatures, "Features", DrawFeatures);
            DrawFoldout(serialized.settings.lightType, "Shape", DrawShape);
            DrawFoldout(serialized.settings.intensity, "Light", DrawLightSettings);

            if (serialized.settings.shadowsType.enumValueIndex != (int)LightShadows.None)
                DrawFoldout(serialized.settings.shadowsType, "Shadows", DrawShadows);
        }

        void DrawFoldout(SerializedProperty foldoutProperty, string title, Action func)
        {
            CoreEditorUtils.DrawSplitter();

            bool state = foldoutProperty.isExpanded;
            state = CoreEditorUtils.DrawHeaderFoldout(title, state);

            if (state)
            {
                EditorGUI.indentLevel++;
                func();
                EditorGUI.indentLevel--;
                GUILayout.Space(2f);
            }

            foldoutProperty.isExpanded = state;
        }

        void DrawFeatures(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            bool disabledScope = m_LightShape == LightShape.Tube || (m_LightShape == LightShape.Rectangle && serialized.settings.isRealtime);

            using (new EditorGUI.DisabledScope(disabledScope))
            {
                bool shadowsEnabled = EditorGUILayout.Toggle(CoreEditorUtils.GetContent("Enable Shadows"), serialized.settings.shadowsType.enumValueIndex != 0);
                serialized.settings.shadowsType.enumValueIndex = shadowsEnabled ? (int)LightShadows.Hard : (int)LightShadows.None;
            }

            EditorGUILayout.PropertyField(serialized.serializedLightData.showAdditionalSettings);
        }

        void DrawShape(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            EditorGUI.BeginChangeCheck(); // For GI we need to detect any change on additional data and call SetLightDirty + For intensity we need to detect light shape change

            EditorGUI.BeginChangeCheck();
            m_LightShape = (LightShape)EditorGUILayout.Popup(s_Styles.shape, (int)m_LightShape, s_Styles.shapeNames);
            if (EditorGUI.EndChangeCheck())
                UpdateLightIntensityUnit();

            if (m_LightShape != LightShape.Directional)
                serialized.settings.DrawRange(false);

            // LightShape is HD specific, it need to drive LightType from the original LightType
            // when it make sense, so the GI is still in sync with the light shape
            switch (m_LightShape)
            {
                case LightShape.Directional:
                    serialized.settings.lightType.enumValueIndex = (int)LightType.Directional;
                    serialized.serializedLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;

                    // Sun disk.
                    EditorGUILayout.Slider(serialized.serializedLightData.sunDiskSize, 0f, 45f, s_Styles.sunDiskSize);
                    EditorGUILayout.Slider(serialized.serializedLightData.sunHaloSize, 0f, 1f, s_Styles.sunHaloSize);
                    EditorGUILayout.PropertyField(serialized.serializedLightData.maxSmoothness, s_Styles.maxSmoothness);
                    break;

                case LightShape.Point:
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    serialized.serializedLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(serialized.serializedLightData.shapeRadius, s_Styles.lightRadius);
                    EditorGUILayout.PropertyField(serialized.serializedLightData.maxSmoothness, s_Styles.maxSmoothness);
                    break;

                case LightShape.Spot:
                    settings.lightType.enumValueIndex = (int)LightType.Spot;
                    serialized.serializedLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Punctual;
                    EditorGUILayout.PropertyField(serialized.serializedLightData.spotLightShape, s_Styles.spotLightShape);
                    var spotLightShape = (SpotLightShape)serialized.serializedLightData.spotLightShape.enumValueIndex;
                    if (spotLightShape == SpotLightShape.Box)
                    {
                        // Box light is a boxed directional light.
                        EditorGUILayout.PropertyField(serialized.serializedLightData.shapeWidth, s_Styles.shapeWidthBox);
                        EditorGUILayout.PropertyField(serialized.serializedLightData.shapeHeight, s_Styles.shapeHeightBox);
                    }
                    else
                    {
                        if (spotLightShape == SpotLightShape.Cone)
                        {
                            settings.DrawSpotAngle();
                            EditorGUILayout.Slider(serialized.serializedLightData.spotInnerPercent, 0f, 100f, s_Styles.spotInnerPercent);
                        }
                        // TODO : replace with angle and ratio
                        else if (spotLightShape == SpotLightShape.Pyramid)
                        {
                            settings.DrawSpotAngle();
                            EditorGUILayout.Slider(serialized.serializedLightData.aspectRatio, 0.05f, 20.0f, s_Styles.aspectRatioPyramid);
                        }

                        EditorGUILayout.PropertyField(serialized.serializedLightData.shapeRadius, s_Styles.lightRadius);
                        EditorGUILayout.PropertyField(serialized.serializedLightData.maxSmoothness, s_Styles.maxSmoothness);
                    }
                    break;

                case LightShape.Rectangle:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_BaseData.type.enumValueIndex = (int)LightType.Rectangle;
                    // In case of change, think to update InitDefaultHDAdditionalLightData()
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    serialized.serializedLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Rectangle;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serialized.serializedLightData.shapeWidth, s_Styles.shapeWidthRect);
                    EditorGUILayout.PropertyField(serialized.serializedLightData.shapeHeight, s_Styles.shapeHeightRect);
                    if (EditorGUI.EndChangeCheck())
                    {

                        settings.areaSizeX.floatValue = serialized.serializedLightData.shapeWidth.floatValue;
                        settings.areaSizeY.floatValue = serialized.serializedLightData.shapeHeight.floatValue;
                    }
                    if (settings.isRealtime)
                        settings.shadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case LightShape.Tube:
                    // TODO: Currently if we use Area type as it is offline light in legacy, the light will not exist at runtime
                    //m_BaseData.type.enumValueIndex = (int)LightType.Rectangle;
                    settings.lightType.enumValueIndex = (int)LightType.Point;
                    serialized.serializedLightData.lightTypeExtent.enumValueIndex = (int)LightTypeExtent.Tube;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(serialized.serializedLightData.shapeWidth, s_Styles.shapeWidthTube);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Fake line with a small rectangle in vanilla unity for GI
                        settings.areaSizeX.floatValue = serialized.serializedLightData.shapeWidth.floatValue;
                        settings.areaSizeY.floatValue = k_MinLightSize;
                    }
                    settings.shadowsType.enumValueIndex = (int)LightShadows.None;
                    break;

                case (LightShape)(-1):
                    // don't do anything, this is just to handle multi selection
                    break;

                default:
                    Debug.Assert(false, "Not implemented light type");
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                // Light size must be non-zero, else we get NaNs.
                serialized.serializedLightData.shapeWidth.floatValue = Mathf.Max(serialized.serializedLightData.shapeWidth.floatValue, k_MinLightSize);
                serialized.serializedLightData.shapeHeight.floatValue = Mathf.Max(serialized.serializedLightData.shapeHeight.floatValue, k_MinLightSize);
                serialized.serializedLightData.shapeRadius.floatValue = Mathf.Max(serialized.serializedLightData.shapeRadius.floatValue, 0.0f);
                m_UpdateAreaLightEmissiveMeshComponents = true;
                ((Light)target).SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
        }

        void UpdateLightIntensityUnit(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            if (m_LightShape == LightShape.Directional)
                serialized.serializedLightData.lightUnit.enumValueIndex = (int)DirectionalLightUnit.Lux;
            else
                serialized.serializedLightData.lightUnit.enumValueIndex = (int)LightUnit.Lumen;
        }

        LightUnit LightIntensityUnitPopup(LightShape shape)
        {
            LightUnit selectedLightUnit;
            LightUnit oldLigthUnit = (LightUnit)serialized.serializedLightData.lightUnit.enumValueIndex;

            EditorGUI.BeginChangeCheck();
            switch (shape)
            {
                case LightShape.Directional:
                    selectedLightUnit = (LightUnit)EditorGUILayout.EnumPopup((DirectionalLightUnit)m_AdditionalLightData.lightUnit.enumValueIndex);
                    break;
                case LightShape.Point:
                case LightShape.Spot:
                    selectedLightUnit = (LightUnit)EditorGUILayout.EnumPopup((PunctualLightUnit)m_AdditionalLightData.lightUnit.enumValueIndex);
                    break;
                default:
                    selectedLightUnit = (LightUnit)EditorGUILayout.EnumPopup((AreaLightUnit)m_AdditionalLightData.lightUnit.enumValueIndex);
                    break;
            }
            if (EditorGUI.EndChangeCheck())
                ConvertLightIntensity(oldLigthUnit, selectedLightUnit);

            return selectedLightUnit;
        }

        void ConvertLightIntensity(LightUnit oldLightUnit, LightUnit newLightUnit)
        {
            float intensity = m_AdditionalLightData.intensity.floatValue;

            // For punctual lights
            if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Candela)
            {
                if (m_LightShape == LightShape.Spot && m_AdditionalLightData.enableSpotReflector.boolValue)
                {
                    // We have already calculate the correct value, just assign it
                    intensity = ((Light)target).intensity;
                }
                else
                    intensity = LightUtils.ConvertPointLightLumenToCandela(intensity);
            }
            if (oldLightUnit == LightUnit.Candela && newLightUnit == LightUnit.Lumen)
            {
                if (m_LightShape == LightShape.Spot && m_AdditionalLightData.enableSpotReflector.boolValue)
                {
                    // We just need to multiply candela by solid angle in this case
                    if ((SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex == SpotLightShape.Cone)
                        intensity = LightUtils.ConvertSpotLightCandelaToLumen(intensity, ((Light)target).spotAngle * Mathf.Deg2Rad, true);
                    else if ((SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex == SpotLightShape.Pyramid)
                    {
                        float angleA, angleB;
                        LightUtils.CalculateAnglesForPyramid(m_AdditionalLightData.aspectRatio.floatValue, ((Light)target).spotAngle * Mathf.Deg2Rad, out angleA, out angleB);

                        intensity = LightUtils.ConvertFrustrumLightCandelaToLumen(intensity, angleA, angleB);
                    }
                    else // Box
                        intensity = LightUtils.ConvertPointLightCandelaToLumen(intensity);
                }
                else
                    intensity = LightUtils.ConvertPointLightCandelaToLumen(intensity);
            }

            // For area lights
            if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Luminance)
                intensity = LightUtils.ConvertAreaLightLumenToLuminance((LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex, intensity, m_AdditionalLightData.shapeWidth.floatValue, m_AdditionalLightData.shapeHeight.floatValue);
            if (oldLightUnit == LightUnit.Luminance && newLightUnit == LightUnit.Lumen)
                intensity = LightUtils.ConvertAreaLightLuminanceToLumen((LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex, intensity, m_AdditionalLightData.shapeWidth.floatValue, m_AdditionalLightData.shapeHeight.floatValue);
            if (oldLightUnit == LightUnit.Luminance && newLightUnit == LightUnit.Ev100)
                intensity = LightUtils.ConvertLuminanceToEv(intensity);
            if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Luminance)
                intensity = LightUtils.ConvertEvToLuminance(intensity);
            if (oldLightUnit == LightUnit.Ev100 && newLightUnit == LightUnit.Lumen)
                intensity = LightUtils.ConvertAreaLightEvToLumen((LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex, intensity, m_AdditionalLightData.shapeWidth.floatValue, m_AdditionalLightData.shapeHeight.floatValue);
            if (oldLightUnit == LightUnit.Lumen && newLightUnit == LightUnit.Ev100)
                intensity = LightUtils.ConvertAreaLightLumenToEv((LightTypeExtent)m_AdditionalLightData.lightTypeExtent.enumValueIndex, intensity, m_AdditionalLightData.shapeWidth.floatValue, m_AdditionalLightData.shapeHeight.floatValue);

            m_AdditionalLightData.intensity.floatValue = intensity;
        }

        void DrawLightSettings(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            settings.DrawColor();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_AdditionalLightData.intensity, s_Styles.lightIntensity);
            m_AdditionalLightData.lightUnit.enumValueIndex = (int)LightIntensityUnitPopup(m_LightShape);
            EditorGUILayout.EndHorizontal();

            // Only display reflector option if it make sense
            if (m_LightShape == LightShape.Spot)
            {
                var spotLightShape = (SpotLightShape)m_AdditionalLightData.spotLightShape.enumValueIndex;
                if ((spotLightShape == SpotLightShape.Cone || spotLightShape == SpotLightShape.Pyramid)
                    && m_AdditionalLightData.lightUnit.enumValueIndex == (int)PunctualLightUnit.Lumen)
                    EditorGUILayout.PropertyField(m_AdditionalLightData.enableSpotReflector, s_Styles.enableSpotReflector);
            }

            settings.DrawBounceIntensity();

            settings.DrawLightmapping();

            EditorGUI.BeginChangeCheck(); // For GI we need to detect any change on additional data and call SetLightDirty

            // No cookie with area light (maybe in future textured area light ?)
            if (!HDAdditionalLightData.IsAreaLight(m_AdditionalLightData.lightTypeExtent))
            {
                settings.DrawCookie();

                // When directional light use a cookie, it can control the size
                if (settings.cookie != null && m_LightShape == LightShape.Directional)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeWidth, s_Styles.cookieSizeX);
                    EditorGUILayout.PropertyField(m_AdditionalLightData.shapeHeight, s_Styles.cookieSizeY);
                    EditorGUI.indentLevel--;
                }
            }

            if (m_AdditionalLightData.showAdditionalSettings.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(!HDUtils.hdrpSettings.supportLightLayers))
                {
                    m_AdditionalLightData.lightLayers.intValue = Convert.ToInt32(EditorGUILayout.EnumFlagsField(s_Styles.lightLayer, (LightLayerEnum)m_AdditionalLightData.lightLayers.intValue));
                }
                EditorGUILayout.PropertyField(m_AdditionalLightData.affectDiffuse, s_Styles.affectDiffuse);
                EditorGUILayout.PropertyField(m_AdditionalLightData.affectSpecular, s_Styles.affectSpecular);
                if (m_LightShape != LightShape.Directional)
                    EditorGUILayout.PropertyField(m_AdditionalLightData.fadeDistance, s_Styles.fadeDistance);
                EditorGUILayout.PropertyField(m_AdditionalLightData.lightDimmer, s_Styles.lightDimmer);
                EditorGUILayout.PropertyField(m_AdditionalLightData.volumetricDimmer, s_Styles.volumetricDimmer);
                if (m_LightShape != LightShape.Directional)
                    EditorGUILayout.PropertyField(m_AdditionalLightData.applyRangeAttenuation, s_Styles.applyRangeAttenuation);

                // Emissive mesh for area light only
                if (HDAdditionalLightData.IsAreaLight(m_AdditionalLightData.lightTypeExtent))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_AdditionalLightData.displayAreaLightEmissiveMesh, s_Styles.displayAreaLightEmissiveMesh);
                    if (EditorGUI.EndChangeCheck())
                        m_UpdateAreaLightEmissiveMeshComponents = true;
                }

                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_UpdateAreaLightEmissiveMeshComponents = true;
                m_AdditionalLightData.fadeDistance.floatValue = Mathf.Max(m_AdditionalLightData.fadeDistance.floatValue, 0.01f);
                ((Light)target).SetLightDirty(); // Should be apply only to parameter that's affect GI, but make the code cleaner
            }
        }

        void DrawBakedShadowParameters(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            switch ((LightType)settings.lightType.enumValueIndex)
            {
                case LightType.Directional:
                    EditorGUILayout.Slider(settings.bakedShadowAngleProp, 0f, 90f, s_Styles.bakedShadowAngle);
                    break;
                case LightType.Spot:
                case LightType.Point:
                    EditorGUILayout.PropertyField(settings.bakedShadowRadiusProp, s_Styles.bakedShadowRadius);
                    break;
            }


            if (settings.isMixed)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(m_AdditionalLightData.nonLightmappedOnly, s_Styles.nonLightmappedOnly);

                if (EditorGUI.EndChangeCheck())
                {
                    ((Light)target).lightShadowCasterMode = m_AdditionalLightData.nonLightmappedOnly.boolValue ? LightShadowCasterMode.NonLightmappedOnly : LightShadowCasterMode.Everything;
                }
            }
        }

        void DrawShadows(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            if (settings.isCompletelyBaked)
            {
                DrawBakedShadowParameters();
                return;
            }

            EditorGUILayout.DelayedIntField(m_AdditionalShadowData.resolution, s_Styles.shadowResolution);
            //EditorGUILayout.Slider(settings.shadowsBias, 0.001f, 1f, s_Styles.shadowBias);
            //EditorGUILayout.Slider(settings.shadowsNormalBias, 0.001f, 1f, s_Styles.shadowNormalBias);
            EditorGUILayout.Slider(m_AdditionalShadowData.viewBiasScale, 0.0f, 15.0f, s_Styles.viewBiasScale);
            EditorGUILayout.Slider(m_AdditionalLightData.shadowNearPlane, HDShadowUtils.k_MinShadowNearPlane, 10f, s_Styles.shadowNearPlane);

            if (settings.isBakedOrMixed)
                DrawBakedShadowParameters();

            // Draw shadow settings using the current shadow algorithm
            HDShadowQuality currentAlgorithm;
            if (settings.lightType.enumValueIndex == (int)LightType.Directional)
                currentAlgorithm = (HDShadowQuality)m_HDShadowInitParameters.directionalShadowQuality;
            else
                currentAlgorithm = (HDShadowQuality)m_HDShadowInitParameters.punctualShadowQuality;
            m_ShadowAlgorithmUIs[currentAlgorithm]();

            // There is currently no additional settings for shadow on directional light
            if (m_AdditionalLightData.showAdditionalSettings.boolValue)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Additional Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_AdditionalShadowData.contactShadows, s_Styles.contactShadows);

                EditorGUILayout.Slider(m_AdditionalShadowData.shadowDimmer, 0.0f, 1.0f, s_Styles.shadowDimmer);
                EditorGUILayout.Slider(m_AdditionalShadowData.volumetricShadowDimmer, 0.0f, 1.0f, s_Styles.volumetricShadowDimmer);

                if (settings.lightType.enumValueIndex != (int)LightType.Directional)
                {
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.fadeDistance, s_Styles.shadowFadeDistance);
                }

                EditorGUILayout.Slider(m_AdditionalShadowData.viewBiasMin, 0.0f, 5.0f, s_Styles.viewBiasMin);
                //EditorGUILayout.PropertyField(m_AdditionalShadowData.viewBiasMax, s_Styles.viewBiasMax);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Slider(m_AdditionalShadowData.normalBiasMin, 0.0f, 5.0f, s_Styles.normalBiasMin);
                if (EditorGUI.EndChangeCheck())
                {
                    // Link min to max and don't expose normalBiasScale (useless when min == max)
                    m_AdditionalShadowData.normalBiasMax.floatValue = m_AdditionalShadowData.normalBiasMin.floatValue;
                }
                //EditorGUILayout.PropertyField(m_AdditionalShadowData.normalBiasMax, s_Styles.normalBiasMax);
                //EditorGUILayout.PropertyField(m_AdditionalShadowData.normalBiasScale, s_Styles.normalBiasScale);
                //EditorGUILayout.PropertyField(m_AdditionalShadowData.sampleBiasScale, s_Styles.sampleBiasScale);
                EditorGUILayout.PropertyField(m_AdditionalShadowData.edgeLeakFixup, s_Styles.edgeLeakFixup);
                if (m_AdditionalShadowData.edgeLeakFixup.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AdditionalShadowData.edgeToleranceNormal, s_Styles.edgeToleranceNormal);
                    EditorGUILayout.Slider(m_AdditionalShadowData.edgeTolerance, 0.0f, 1.0f, s_Styles.edgeTolerance);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
        }
        
        static void DrawLowShadowSettings(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            // Currently there is nothing to display here
        }

        static void DrawMediumShadowSettings(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {

        }

        static void DrawHighShadowSettings(HDLightUI ui, SerializedHDLight serialized, Editor owner)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hight Quality Settings", EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serialized.serializedLightData.shadowSoftness, s_Styles.shadowSoftness);
                EditorGUILayout.PropertyField(serialized.serializedLightData.blockerSampleCount, s_Styles.blockerSampleCount);
                EditorGUILayout.PropertyField(serialized.serializedLightData.filterSampleCount, s_Styles.filterSampleCount);
            }
        }
    }
}
