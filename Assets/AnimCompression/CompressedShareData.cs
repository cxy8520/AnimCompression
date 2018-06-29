using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AnimCompression
{
    /// <summary>
    /// 共享数据集，存储属性，并将其ID化
    /// </summary>
    public class CompressedShareData : ScriptableObject
    {
        [Serializable]
        public class DataField
        {
            //[NonSerialized]
            public string propertyName;
            //[NonSerialized]    
            public string propertyPath;

            public string propertyType;

        }
        public List<DataField> fields = new List<DataField>();
#if UNITY_EDITOR
        public string rawAnimatorGUID;
#endif
        public ushort getFieldIndex(string propertyPath, string propertyType, string propertyName)
        {
            for (ushort i = 0, length = (ushort)fields.Count; i < length; i++)
            {
                var field = fields[i];
                if (field.propertyPath == propertyPath && field.propertyType == propertyType && field.propertyName == propertyName)
                {
                    return i;
                }
            }
            fields.Add(new DataField()
            {
                propertyPath = propertyPath,
                propertyType = propertyType,
                propertyName = propertyName,
            });
            return (ushort)(fields.Count -1);
        }
    }

}