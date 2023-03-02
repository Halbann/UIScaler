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

using SpaceWarp.API.Mods;


namespace Scaler
{
    [MainMod]
    public class ScalerMod : Mod
    {
        #region Fields

        // Main.
        public static bool loaded = false;

        // Paths.
        private static string _assemblyFolder;
        private static string AssemblyFolder =>
            _assemblyFolder ?? (_assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        private static string _settingsPath;
        private static string SettingsPath =>
            _settingsPath ?? (_settingsPath = Path.Combine(AssemblyFolder, "Settings.json"));

        // Scaling.

        private static float _scaleFactor = 100f;
        private static float ScaleFactor
        {
            get { return _scaleFactor; }
            set
            {
                if (value == _scaleFactor)
                    return;

                _scaleFactor = Mathf.Round(value);
                //scaleFactorSlider = value;
                SetScale(_scaleFactor);
            }
        }

        //private static float scaleFactorSlider = _scaleFactor;
        internal static CanvasScalerExtended canvasScaler;
        internal static Vector2 referenceResolution;
        private static float scaleMin = 60f;
        private static float scaleMax = 120f;
        internal static float scaleDefault = 75f;

        #endregion

        #region Main

        public override void OnInitialized()
        {
            if (loaded)
            {
                Destroy(this);
            }

            loaded = true;

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

        void CreateSetting(string text, float min, float max, float current, Action<float> moved)
        {
            GameObject popupconvas = GameManager.Instance.Game.UI.GetPopupCanvas().gameObject;
            GameObject docking = popupconvas.GetChild("Tolerance Distance for Docking");

            GameObject scaleSlider = Instantiate(docking, popupconvas.GetChild("ShowVesselLabels").transform.parent);
            scaleSlider.name = text;
            scaleSlider.GetComponent<SettingsElementDescriptionController>().DescriptionLocalizationKey = "";

            GameObject label = scaleSlider.GetChild("Label");
            label.GetComponent<Localize>().Term = "";
            label.GetComponent<TextMeshProUGUI>().text = text;

            GameObject setting = scaleSlider.GetChild("Setting");
            setting.GetComponent<UIValueBinderGroup>().BindValue(new Property<float>(current));

            GameObject sliderlinear = setting.GetChild("KSP2SliderLinear");
            SliderExtended sliderExtended = sliderlinear.GetComponent<SliderExtended>();
            sliderExtended.onValueChanged.AddListener(new UnityAction<float>(moved));

            var writeNumber = sliderlinear.GetComponent<UIValue_WriteNumber_Slider>();
            writeNumber.SetMappedValueRange(min, max, true);
        }

        static void HookSaveButton()
        {
            GameObject popupconvas = GameManager.Instance.Game.UI.GetPopupCanvas().gameObject;
            GameObject saveButton = popupconvas.GetChild("Apply Settings");

            saveButton.GetComponent<ButtonExtended>().onLeftClick.AddListener(Save);
        }

        void SliderMoved(float value)
        {
            value = Mathf.Lerp(scaleMin, scaleMax, value);
            ScaleFactor = value;
        }

        static void SetScale(float scale)
        {
            canvasScaler.referenceResolution = referenceResolution * (1 / (scale / 100));
            Debug.Log($"[Scaler] Set the scale: {scale} {canvasScaler.referenceResolution}");
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
        public float uiScale = ScalerMod.scaleDefault;
    }
}