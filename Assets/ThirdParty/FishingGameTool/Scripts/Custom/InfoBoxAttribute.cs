using System;
using UnityEngine;

namespace FishingGameTool.CustomAttribute
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class InfoBoxAttribute : PropertyAttribute
    {
        public string _infoText;

        public InfoBoxAttribute(string infoText)
        {
            _infoText = infoText;
        }
    }
}
