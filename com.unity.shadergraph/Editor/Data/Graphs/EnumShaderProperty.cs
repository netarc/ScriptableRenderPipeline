using System;
using System.Text;
using UnityEditor.Graphing;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    enum EnumType
    {
        Enum,
        CSharpEnum,
        KeywordEnum
    }

    [Serializable]
    class EnumShaderProperty : AbstractShaderProperty<float>
    {
        public EnumShaderProperty()
        {
            displayName = "Enum";
        }

        public override PropertyType propertyType => PropertyType.Vector1;

        public override Vector4 defaultValue => new Vector4(value, value, value, value);

        public override bool isBatchable => true;

        public override bool isExposable => true;

        public override bool isRenamable => true;

        [SerializeField]
        EnumType m_EnumType = EnumType.Enum;

        public EnumType enumType
        {
            get => m_EnumType;
            set => m_EnumType = value;
        }

        [SerializeField]
        List<string> m_EnumNames = new List<string>();
        
        [SerializeField]
        List<int> m_EnumValues = new List<int>();

        [SerializeField]
        Type m_CSharpEnumType;

        public List<string> enumNames
        {
            get => m_EnumNames;
            set => m_EnumNames = value;
        }

        public List<int> enumValues
        {
            get => m_EnumValues;
            set => m_EnumValues = value;
        }

        public Type cSharpEnumType
        {
            get => m_CSharpEnumType;
            set => m_CSharpEnumType = value;
        }
        
        [SerializeField]
        bool m_Hidden = false;

        public bool hidden
        {
            get => m_Hidden;
            set => m_Hidden = value;
        }

        public override string GetPropertyBlockString()
        {
            var result = new StringBuilder();
            if (hidden)
            {
                result.Append("[HideInInspector] ");
            }

            string enumValuesString = ""; // TODO
            string enumTypeString = enumType.ToString();
            switch (enumType)
            {
                case EnumType.CSharpEnum:
                    enumValuesString = m_CSharpEnumType.ToString();
                    enumTypeString = "Enum";
                    break;
                case EnumType.KeywordEnum:
                    enumValuesString = enumNames.Aggregate((s, e) => s + ", " + e);
                    break;
                default:
                case EnumType.Enum:
                    for (int i = 0; i < enumNames.Count; i++)
                    {
                        int value = (i < enumValues.Count) ? enumValues[i] : i;
                        enumValuesString += (enumNames[i] + ", " + value + ((i != enumNames.Count - 1) ? ", " : ""));
                    }
                    break;
            }
            
            result.Append($"[{enumTypeString}({enumValuesString})] {referenceName}(\"{displayName}\", Float) = {NodeUtils.FloatToShaderValue(value)}");

            return result.ToString();
        }

        public override string GetPropertyDeclarationString(string delimiter = ";")
        {
            return $"float {referenceName}{delimiter}";
        }

        public override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(PropertyType.Vector1)
            {
                name = referenceName,
                floatValue = value
            };
        }

        public override AbstractMaterialNode ToConcreteNode()
        {
            throw new Exception("Enums as node are not yet supported");
        }

        public override AbstractShaderProperty Copy()
        {
            var copied = new EnumShaderProperty();
            copied.displayName = displayName;
            copied.value = value;
            copied.enumValues = enumValues;
            copied.enumNames = enumNames;
            return copied;
        }
    }
}