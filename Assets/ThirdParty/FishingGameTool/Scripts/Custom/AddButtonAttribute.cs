using System;
using UnityEngine;

namespace FishingGameTool.CustomAttribute
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class AddButtonAttribute : PropertyAttribute
    {
        public string _buttonLabel;
        public string _boolProperty;

        public AddButtonAttribute(string buttonLabel, string boolProperty)
        {
            _buttonLabel = buttonLabel;
            _boolProperty = boolProperty;
        }
    }
}
