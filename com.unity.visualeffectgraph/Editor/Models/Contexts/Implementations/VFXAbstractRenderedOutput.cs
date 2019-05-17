using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractRenderedOutput : VFXContext
    {
        public enum BlendMode
        {
            Additive,
            Alpha,
            Masked,
            AlphaPremultiplied,
            Opaque,
        }

        [VFXSetting, Header("Render States")]
        public BlendMode blendMode = BlendMode.Alpha;

        protected VFXAbstractRenderedOutput(VFXDataType dataType) : base(VFXContextType.kOutput, dataType, VFXDataType.kNone) { }

        private static VFXSRPOutputData s_DefaultSRPData = null;
        public VFXSRPOutputData GetSRPData()
        {
            if (currentSrpData == null)
            {
                if (s_DefaultSRPData == null)
                    s_DefaultSRPData = ScriptableObject.CreateInstance<VFXSRPOutputData>();
                return s_DefaultSRPData;
            }

            return currentSrpData;
        }

        private VFXSRPOutputData GetOrCreateSRPData()
        {
            VFXSRPBinder binder = VFXLibrary.currentSRPBinder;
            if (binder == null)
                return null;

            Type outputDataType = binder.SRPOutputDataType;
            if (outputDataType == null)
                return null;

            var outputData = srpData.FirstOrDefault(d => d != null && d.GetType() == outputDataType);
            if (outputData == null)
            {
                outputData = (VFXSRPOutputData)ScriptableObject.CreateInstance(outputDataType);
                srpData.Add(outputData);
            }

            return outputData;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (srpData == null)
                srpData = new List<VFXSRPOutputData>();

            currentSrpData = GetOrCreateSRPData();
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs)
        {
            base.CollectDependencies(objs);
            foreach (var data in srpData)
                if (data != null)
                {
                    objs.Add(data);
                    data.CollectDependencies(objs);
                }
        }

        public override VFXSetting GetSetting(string name)
        {
            VFXSetting setting = base.GetSetting(name);
            if (!setting.valid && currentSrpData != null)
                setting = currentSrpData.GetSetting(name);
            return setting;
        }

        public override IEnumerable<VFXSetting> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags)
        {
            var settings = base.GetSettings(listHidden, flags);
            if (currentSrpData != null)
                settings = settings.Concat(currentSrpData.GetSettings(listHidden, flags));
            return settings;
        }

        protected virtual void WriteBlendMode(VFXShaderWriter writer)
        {
            if (blendMode == BlendMode.Additive)
                writer.WriteLine("Blend SrcAlpha One");
            else if (blendMode == BlendMode.Alpha)
                writer.WriteLine("Blend SrcAlpha OneMinusSrcAlpha");
            else if (blendMode == BlendMode.AlphaPremultiplied)
                writer.WriteLine("Blend One OneMinusSrcAlpha");
        }

        [SerializeField]
        private List<VFXSRPOutputData> srpData;

        [NonSerialized]
        private VFXSRPOutputData currentSrpData;
    }
}
