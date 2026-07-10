using System;
using UnityEngine;

namespace SubclassSelector
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SelectSubclassAttribute : PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class VisibleSubclassAttribute : Attribute { }
}
