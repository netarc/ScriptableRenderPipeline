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

        public bool isBlendModeOpaque { get { return blendMode == BlendMode.Opaque || blendMode == BlendMode.Masked; } }

        protected VFXAbstractRenderedOutput(VFXDataType dataType) : base(VFXContextType.kOutput, dataType, VFXDataType.kNone) { }

        public VFXSRPOutputData srpData => m_CurrentSrpData;
        private VFXSRPOutputData CreateDefaultSRPData()
        {
            var defaultSrpData  = ScriptableObject.CreateInstance<VFXSRPOutputData>();
            defaultSrpData.Init(this);
            return defaultSrpData;
        }

        private VFXSRPOutputData GetOrCreateSRPData()
        {
            VFXSRPBinder binder = VFXLibrary.currentSRPBinder;
            if (binder == null)
                return CreateDefaultSRPData();

            Type outputDataType = binder.SRPOutputDataType;
            if (outputDataType == null)
                return CreateDefaultSRPData();

            var outputData = m_SrpData.FirstOrDefault(d => d != null && d.GetType() == outputDataType);
            if (outputData == null)
            {
                outputData = (VFXSRPOutputData)ScriptableObject.CreateInstance(outputDataType);
                m_SrpData.Add(outputData);
            }

            if (outputData.owner != this)
                outputData.Init(this);

            return outputData;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_SrpData == null)
                m_SrpData = new List<VFXSRPOutputData>();

            m_CurrentSrpData = GetOrCreateSRPData();
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs)
        {
            base.CollectDependencies(objs);
            foreach (var data in m_SrpData)
                if (data != null)
                {
                    objs.Add(data);
                    data.CollectDependencies(objs);
                }
        }

        public override VFXSetting GetSetting(string name)
        {
            VFXSetting setting = base.GetSetting(name);
            if (!setting.valid)
                setting = m_CurrentSrpData.GetSetting(name);
            return setting;
        }

        public override IEnumerable<VFXSetting> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags)
        {
            var settings = base.GetSettings(listHidden, flags);
            settings = settings.Concat(m_CurrentSrpData.GetSettings(listHidden, flags));
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
        private List<VFXSRPOutputData> m_SrpData;

        [NonSerialized]
        private VFXSRPOutputData m_CurrentSrpData;
    }
}
