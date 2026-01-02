using System;
using FishingGameTool.CustomAttribute;

namespace FishingGameTool.Fishing.Line 
{ 
    [Serializable]
    public class FishingLineStatus
    {
        public float _maxLineLoad = 20f;
        public float _overLoadDuration = 2f;
        public float _maxLineLength = 15f;

        [ReadOnlyField]
        public float _currentLineLength;
        [ReadOnlyField]
        public float _currentOverLoad;
        [ReadOnlyField]
        public float _currentLineLoad;
        [ReadOnlyField]
        public float _attractFloatSpeed;
        [ReadOnlyField]
        public bool _isLineBroken;
    }
}
