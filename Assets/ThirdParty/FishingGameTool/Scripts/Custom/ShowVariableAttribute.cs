using System;
using UnityEngine;

namespace FishingGameTool.CustomAttribute
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class ShowVariableAttribute : PropertyAttribute
    {
        public string _boolProperty;

        public ShowVariableAttribute(string boolProperty)
        {
            _boolProperty = boolProperty;
        }
    }
}
