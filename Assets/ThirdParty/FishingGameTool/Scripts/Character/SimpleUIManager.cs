using FishingGameTool.Fishing;
using FishingGameTool.Fishing.Line;
using UnityEngine;
using UnityEngine.UI;
using FishingGameTool.CustomAttribute;
using FishingGameTool.Fishing.LootData;
using TMPro;

namespace FishingGameTool.Example
{
    public enum FillDirection
    {
        Vertical,
        Horizontal
    };

    public class SimpleUIManager : MonoBehaviour
    {
        [System.Serializable]
        public class FishingLineLoadBar
        {
            [InfoBox("A reference to a user interface object that will be enabled or disabled.")]
            public GameObject _UIObject;
            public Transform _loadBar;
            public FillDirection _fillDirection;

            [Space, AddButton("Enable Color Gradient", "_fishingLineLoadBar._enableColorGradient")]
            public bool _enableColorGradient = false;

            [Space, ShowVariable("_enableColorGradient")]
            [InfoBox("The storeUIController in which the color will be changed.")]
            public Image _loadBarImage;
            [ShowVariable("_enableColorGradient")]
            public Color _minLineLoadColor = new Color { r = 255, g = 255, b = 255, a = 255 };
            [ShowVariable("_enableColorGradient")]
            public Color _maxLineLoadColor = new Color { r = 255, g = 255, b = 255, a = 255 };
            [ShowVariable("_enableColorGradient")]
            public Color _overloadLineColor = new Color { r = 255, g = 255, b = 255, a = 255 };
        }

        [System.Serializable]
        public class CastForceBar
        {
            [InfoBox("A reference to a user interface object that will be enabled or disabled.")]
            public GameObject _UIObject;
            public Transform _castBar;
            public FillDirection _fillDirection;

            [Space, AddButton("Enable Color Gradient", "_castForceBar._enableColorGradient")]
            public bool _enableColorGradient = false;

            [Space, ShowVariable("_enableColorGradient")]
            [InfoBox("The storeUIController in which the color will be changed.")]
            public Image _castBarImage;
            [ShowVariable("_enableColorGradient")]
            public Color _minCastForceColor = new Color { r = 255, g = 255, b = 255, a = 255 };
            [ShowVariable("_enableColorGradient")]
            public Color _maxCastForceColor = new Color { r = 255, g = 255, b = 255, a = 255 };
        }

        [BetterHeader("UI Settings", 20)]
        public FishingSystem _fishingSystem;
        public TMP_Text _lootInfoText;
        public GameObject _FGTMenu;
        [Space]
        public FishingLineLoadBar _fishingLineLoadBar;
        [Space]
        public CastForceBar _castForceBar;

        #region PRIVATE VARIABLES

        private FishingLineStatus _lineStatus;
        private bool _showMenu = true;

        #endregion

        private void Awake()
        {
            _lineStatus = _fishingSystem._fishingRod._lineStatus;
        }

        private void Update()
        {
            ControlFishingLineLoadBar();
            ControlCastBar();
            ControlMenu();
        }

        private void ControlMenu()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                _showMenu = !_showMenu;

            if (_showMenu)
            {
                _FGTMenu.SetActive(true);
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                _FGTMenu.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void ControlCastBar()
        {
            if (_fishingSystem._advanced._caughtLoot || !_fishingSystem._castInput)
            {
                _castForceBar._UIObject.SetActive(false);
                return;
            }

            _castForceBar._UIObject.SetActive(true);

            float castProgress = CalculateProgess(_fishingSystem._currentCastForce, _fishingSystem._maxCastForce);

            if (_castForceBar._enableColorGradient)
            {
                Color currentCastBarColor = Color32.Lerp(_castForceBar._minCastForceColor, _castForceBar._maxCastForceColor, castProgress);
                _castForceBar._castBarImage.color = currentCastBarColor;
            }

            SetBarScale(_castForceBar._fillDirection, _castForceBar._castBar, castProgress);
        }

        private void ControlFishingLineLoadBar()
        {
            if (!_fishingSystem._advanced._caughtLoot)
            {
                _fishingLineLoadBar._UIObject.SetActive(false);
                _lootInfoText.gameObject.SetActive(false);
                return;
            }

            _lootInfoText.gameObject.SetActive(true);
            _fishingLineLoadBar._UIObject.SetActive(true);

            ShowLootInfo(_fishingSystem._advanced._caughtLootData, _lootInfoText);

            float loadProgress = CalculateProgess(_lineStatus._currentLineLoad, _lineStatus._maxLineLoad);

            if (_fishingLineLoadBar._enableColorGradient)
            {
                Color currentLoadBarColor = Color32.Lerp(_fishingLineLoadBar._minLineLoadColor, _fishingLineLoadBar._maxLineLoadColor, loadProgress);

                if (_lineStatus._currentOverLoad != 0)
                {
                    float overloadProgress = CalculateProgess(_lineStatus._currentOverLoad, _lineStatus._overLoadDuration);
                    currentLoadBarColor = Color32.Lerp(_fishingLineLoadBar._maxLineLoadColor, _fishingLineLoadBar._overloadLineColor, overloadProgress);
                }

                _fishingLineLoadBar._loadBarImage.color = currentLoadBarColor;
            }

            SetBarScale(_fishingLineLoadBar._fillDirection, _fishingLineLoadBar._loadBar, loadProgress);
        }

        private void ShowLootInfo(FishingLootData lootData, TMP_Text infoGameObject)
        {
            infoGameObject.text = lootData._lootName + " | " + lootData._lootTier.ToString() + " | " + lootData._lootDescription;
        }

        private void SetBarScale(FillDirection fillDirection, Transform barTransform, float progress)
        {
            Vector3 barScale = Vector3.zero;

            if (fillDirection == FillDirection.Vertical)
                barScale = new Vector3(barTransform.localScale.x, progress, barTransform.localScale.z);
            else if (fillDirection == FillDirection.Horizontal)
                barScale = new Vector3(progress, barTransform.localScale.y, barTransform.localScale.z);

            barTransform.localScale = barScale;
        }

        private static float CalculateProgess(float input, float max)
        {
            float x = Mathf.InverseLerp(0f, max, input);
            float value = Mathf.Lerp(0f, 1f, x);

            return value;
        }
    }
}
