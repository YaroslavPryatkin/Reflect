using System;
using UnityEngine;

namespace CustomAttributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SelectSubclassAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class VisibleSubclassAttribute : Attribute { }
    
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class EnableIfAttribute : PropertyAttribute
    {
        public string ConditionFieldName { get; private set; }

        public EnableIfAttribute(string conditionFieldName)
        {
            this.ConditionFieldName = conditionFieldName;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    public class CurveRangeAttribute : PropertyAttribute
    {
        public Rect Bounds { get; }

        public CurveRangeAttribute(float minX, float minY, float maxX, float maxY)
        {
            Bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
