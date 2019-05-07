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
        protected VFXAbstractRenderedOutput(VFXDataType dataType) : base(VFXContextType.kOutput, dataType, VFXDataType.kNone) { }

        protected VFXSRPOutputData GetOrCreateSRPData()
        {
            VFXSRPBinder binder = VFXLibrary.currentSRPBinder;
            if (binder == null)
                return null;

            Type outputDataType = binder.SRPOutputDataType;
            if (outputDataType == null)
                return null;

            var outputData = srpData.FirstOrDefault(d => d.GetType() == outputDataType);
            if (outputData == null)
            {
                outputData = (VFXSRPOutputData)ScriptableObject.CreateInstance(outputDataType);
                //outputData.SetOwner(this);
                srpData.Append(outputData);
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
            foreach(var data in srpData)
                data.CollectDependencies(objs);
        }

        public override IEnumerable<VFXSetting> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags)
        {
            var settings = base.GetSettings(listHidden, flags);
            if (currentSrpData != null)
                settings = settings.Concat(currentSrpData.GetSettings(listHidden, flags));
            return settings;
        }

        [SerializeField]
        private List<VFXSRPOutputData> srpData;

        [NonSerialized]
        private VFXSRPOutputData currentSrpData;
    }
}
