// No support to Distortion
// No support to Shadows
Shader "Lightweight Render Pipeline/Particles/Unlit"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        _BumpMap("Normal Map", 2D) = "bump" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _SoftParticlesNearFadeDistance("Soft Particles Near Fade", Float) = 0.0
        _SoftParticlesFarFadeDistance("Soft Particles Far Fade", Float) = 1.0
        _CameraNearFadeDistance("Camera Near Fade", Float) = 1.0
        _CameraFarFadeDistance("Camera Far Fade", Float) = 2.0
        _DistortionBlend("Distortion Blend", Float) = 0.5
        _DistortionStrength("Distortion Strength", Float) = 1.0

        // Hidden properties
        [HideInInspector] _Mode("__mode", Float) = 0.0
        [HideInInspector] _BaseColorMode("__colormode", Float) = 0.0
        [HideInInspector] _FlipbookMode("__flipbookmode", Float) = 0.0
        [HideInInspector] _LightingEnabled("__lightingenabled", Float) = 0.0
        [HideInInspector] _EmissionEnabled("__emissionenabled", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _SoftParticlesEnabled("__softparticlesenabled", Float) = 0.0
        [HideInInspector] _CameraFadingEnabled("__camerafadingenabled", Float) = 0.0
        [HideInInspector] _SoftParticleFadeParams("__softparticlefadeparams", Vector) = (0,0,0,0)
        [HideInInspector] _CameraFadeParams("__camerafadeparams", Vector) = (0,0,0,0)
        [HideInInspector] _BaseColorAddSubDiff("__coloraddsubdiff", Vector) = (0,0,0,0)
        [HideInInspector] _ColorMode("_ColorMode", Float) = 0.0
        [HideInInspector] _DistortionEnabled("__distortionenabled", Float) = 0.0
        [HideInInspector] _DistortionStrengthScaled("Distortion Strength Scaled", Float) = 0.1
    }

    Category
    {
        SubShader
        {
            Tags{"RenderType" = "Opaque" "IgnoreProjector" = "True" "PreviewType" = "Plane" "PerformanceChecks" = "False"}

            BlendOp[_BlendOp]
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]
            ColorMask RGB

            Pass
            {
                Name "Unlit"
                HLSLPROGRAM
                // Required to compile gles 2.0 with standard srp library
                #pragma prefer_hlslcc gles
                #pragma exclude_renderers d3d11_9x
                #pragma multi_compile __ SOFTPARTICLES_ON
                #pragma multi_compile_fog
                #pragma target 2.0

                #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
                #pragma shader_feature _ _COLOROVERLAY_ON _COLORCOLOR_ON _COLORADDSUBDIFF_ON
                #pragma shader_feature _NORMALMAP
                #pragma shader_feature _EMISSION
                #pragma shader_feature _FADING_ON
                #pragma shader_feature _DISTORTION_ON
                #pragma shader_feature _REQUIRE_UV2

                #pragma vertex vertParticleUnlit
                #pragma fragment fragParticleUnlit

                #include "ParticlesUnlitInput.hlsl"
                #include "ParticlesUnlitForwardPass.hlsl"
                
                ENDHLSL
            }
        }
    }

    CustomEditor "UnityEditor.Experimental.Rendering.LightweightPipeline.ParticlesLitShaderGUI"
}
