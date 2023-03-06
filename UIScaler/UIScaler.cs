using System;
using System.IO;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Newtonsoft.Json;
using I2.Loc;
using TMPro;

using KSP.Game;
using KSP.UI.Binding;
using KSP.UI.Binding.Core;
using KSP.Api.CoreTypes;
using KSP.UI;

using BepInEx;
using SpaceWarp;
using SpaceWarp.API.Mods;

namespace UIScaler
{
    [BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
    [BepInPlugin(ModGuid, ModName, ModVer)]
    public class UIScaler : BaseSpaceWarpPlugin
    {
        public const string ModGuid = "com.github.halbann.uiscaler";
        public const string ModName = "UI Scaler";
        public const string ModVer = "0.1.1";

        #region Fields

        // Paths.
        private static string _assemblyFolder;
        private static string AssemblyFolder =>
            _assemblyFolder ?? (_assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        private static string _settingsPath;
        private static string SettingsPath =>
            _settingsPath ?? (_settingsPath = Path.Combine(AssemblyFolder, "Settings.json"));

        // Scaling.

        private static float _scaleFactor = 100f;
        public static float ScaleFactor
        {
            get { return _scaleFactor; }
            set
            {
                if (value == _scaleFactor)
                    return;

                _scaleFactor = Mathf.Round(value);
                SetScale(_scaleFactor);
            }
        }

        internal static CanvasScalerExtended canvasScaler;
        internal static Vector2 referenceResolution;
        public static float scaleMin = 60f;
        public static float scaleMax = 120f;
        public static float scaleDefault = 80f;

        #endregion

        #region Main

        public override void OnInitialized()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);

            var scalers = FindObjectsOfType<CanvasScalerExtended>();

            canvasScaler = GameManager.Instance.Game.UI.GetRootCanvas().GetComponent<CanvasScalerExtended>();
            referenceResolution = canvasScaler.referenceResolution;

            ScaleFactor = Load();
            CreateSetting("UI Scale", scaleMin, scaleMax, ScaleFactor, SliderMoved);
            HookSaveButton();
        }

        #endregion

        #region Functions

        private void CreateSetting(string text, float min, float max, float current, Action<float> moved)
        {
            // Find the docking tolerance slider.
            GameObject popupconvas = GameManager.Instance.Game.UI.GetPopupCanvas().gameObject;
            GameObject docking = popupconvas.GetChild("Tolerance Distance for Docking");

            // Clone the docking tolerance slider.
            GameObject scaleSlider = Instantiate(docking, popupconvas.GetChild("ShowVesselLabels").transform.parent);
            scaleSlider.name = text;

            // Modify the strings.
            GameObject label = scaleSlider.GetChild("Label");
            label.GetComponent<Localize>().Term = "";
            label.GetComponent<TextMeshProUGUI>().text = text;
            scaleSlider.GetComponent<SettingsElementDescriptionController>().DescriptionLocalizationKey = "";

            // Bind a value for the slider with the initial value.
            GameObject setting = scaleSlider.GetChild("Setting");
            setting.GetComponent<UIValueBinderGroup>().BindValue(new Property<float>(current));

            // Add a function call for when the slider is moved.
            GameObject sliderlinear = setting.GetChild("KSP2SliderLinear");
            SliderExtended sliderExtended = sliderlinear.GetComponent<SliderExtended>();
            sliderExtended.onValueChanged.AddListener(new UnityAction<float>(moved));
            
            // Set the slider range and update it visually.
            var writeNumber = sliderlinear.GetComponent<UIValue_WriteNumber_Slider>();
            writeNumber.SetMappedValueRange(min, max, true);
        }

        private static void HookSaveButton()
        {
            GameObject popupconvas = GameManager.Instance.Game.UI.GetPopupCanvas().gameObject;
            GameObject saveButton = popupconvas.GetChild("Apply Settings");

            saveButton.GetComponent<ButtonExtended>().onLeftClick.AddListener(Save);
        }

        public void SliderMoved(float value)
        {
            value = Mathf.Lerp(scaleMin, scaleMax, value);
            ScaleFactor = value;
        }

        private static void SetScale(float scale)
        {
            canvasScaler.referenceResolution = referenceResolution * (1 / (scale / 100));
            Debug.Log($"[UIScaler] Set the scale: {scale} {canvasScaler.referenceResolution}");
        }

        #endregion

        #region Settings

        private static void Save()
        {
            ScalerSettings settings = new ScalerSettings()
            {
                uiScale = ScaleFactor
            };

            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings));
        }

        private float Load()
        {
            ScalerSettings settings;
            try
            {
                settings = JsonConvert.DeserializeObject<ScalerSettings>(File.ReadAllText(SettingsPath));
            }
            catch (FileNotFoundException)
            {
                settings = new ScalerSettings();
            }

            return settings.uiScale;
        }

        #endregion
    }

    public class ScalerSettings
    {
        public float uiScale = UIScaler.scaleDefault;
    }
}