using System;
using UnityEngine;

namespace FishingGameTool.CustomAttribute
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class BetterHeaderAttribute : PropertyAttribute
    {
        public string _headerText;
        public int _fontSize;

        public BetterHeaderAttribute(string headerText, int fontSize = 16)
        {
            _headerText = headerText;
            _fontSize = fontSize;
        }
    }
}
