using System;
using System.Collections.Generic;
using System.Linq;
using static UnityEditor.VFX.VFXAbstractRenderedOutput;

namespace UnityEditor.VFX
{
    class VFXSRPSubOutput : VFXModel
    {
        public void Init(VFXAbstractRenderedOutput owner)
        {
            if (m_Owner != null)
                throw new InvalidOperationException("Owner is already set");
            if (owner == null)
                throw new NullReferenceException("Owner cannot be null");

            m_Owner = owner;    
        }

        private VFXAbstractRenderedOutput m_Owner;
        public VFXAbstractRenderedOutput owner => m_Owner;
    }
}
