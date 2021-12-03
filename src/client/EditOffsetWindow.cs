using Aki.Common.Utils.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Settings;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using AttachmentVisual = GClass418.GClass419;
using Newtonsoft.Json;
using UnityEngine.Events;
using Notifier = GClass1368;
using UnityEngine.EventSystems;
using JetBrains.Annotations;
using System.Globalization;
using AttachmentsOffset.Utils;

namespace AttachmentsOffset
{
    public enum ActionState
    {
        Get,
        Set
    }
    public enum LimitBounds
    {
        Min,
        Max
    }

    public class EditOffsetWindow : Window, IPointerClickHandler, IEventSystemHandler, GInterface66, GInterface68
    {
        // Config
        public bool ExtendedMode
        {
            get => AttachmentsOffset.ModConfig.extendedMode;
        }
        public OffsetConfig PositionRange
        {
            get => AttachmentsOffset.ModConfig.positionOffset;
        }
        public OffsetConfig RotationRange
        {
            get => AttachmentsOffset.ModConfig.rotationOffset;
        }

        // Misc
        private Action _windowSelectAction;
        private Action<Vector3Json, Vector3Json> saveAction;

        public DefaultUIButton saveButton;
        public DefaultUIButton revertButton;
        public DefaultUIButton resetButton;

        public OffsetableComponent component;

        public GameObject sliderPanel;

        public BetterSlider sliderPosY;
        public BetterSlider sliderPosX;
        public BetterSlider sliderPosZ;
        public BetterSlider sliderRotX;
        public BetterSlider sliderRotY;
        public BetterSlider sliderRotZ;

        public Dictionary<BetterSlider, Dictionary<string, object>> sliders = new Dictionary<BetterSlider, Dictionary<string, object>>();

        private IItemOwner _owner;

        public void OnItemRemoved(GEventArgs2 obj)
        {
            Action onClosed = this.OnClosed;
            if (onClosed == null)
            {
                return;
            }
            onClosed();
        }

        public void OnPointerClick([NotNull] PointerEventData eventData)
        {
            if (this._windowSelectAction != null)
            {
                this._windowSelectAction();
            }
        }

        public override void Close()
        {
            foreach(var slider in sliders)
            {
                slider.Key.Hide();
            }
            Revert(false);

            this._owner.UnregisterView(this);

            this.saveButton.OnClick.RemoveAllListeners();
            this.resetButton.OnClick.RemoveAllListeners();
            this.revertButton.OnClick.RemoveAllListeners();

            this.component = null;
            base.Close();
        }

        private void Save()
        {
            this.saveAction.Invoke(this.component.Position.ToJsonVector(), this.component.Rotation.ToJsonVector());
            component.Save();
            this.OnClosed();
        }

        private void Revert() // Just for delegate to stop crying about optional parameters
        {
            Revert(true);
        }
        private void Revert(bool updateUI = true)
        {
            // For some reason sometimes when the game tries to close the window, it's AFTER the component is deleted.
            if(component != null)
                component.Revert();
            if(updateUI)
                UpdateSliders();
        }

        private void Reset()
        {
            component.Reset();
            UpdateSliders();
        }

        public void UpdateSliders()
        {
            foreach(BetterSlider slider in sliders.Keys)
            {
                UpdateSlider(slider);
            }
        }

        public void UpdateSlider(BetterSlider slider)
        {
            try
            {
                Dictionary<string, object> sliderProps = sliders[slider] as Dictionary<string, object>;
                Dictionary<ActionState, object> sliderValues = sliderProps["values"] as Dictionary<ActionState, object>;
                Dictionary<LimitBounds, float> sliderLimits = sliderProps["limits"] as Dictionary<LimitBounds, float>;

                slider.Show(sliderLimits[LimitBounds.Min], sliderLimits[LimitBounds.Max]);
                slider.UpdateValue((sliderValues[ActionState.Get] as Func<float>).Invoke(), false, sliderLimits[LimitBounds.Min], sliderLimits[LimitBounds.Max]);
            }
            catch { }
        }

        public void AddSliderBinding(BetterSlider slider, Func<float> getVal, Action<float> setVal, OffsetConfig offset)
        {
            if (slider == null || sliders.ContainsKey(slider)) return;

            sliders.Add(slider, new Dictionary<string, object>()
            {
                {
                    "values",
                    new Dictionary<ActionState, object>()
                    {
                        {
                            ActionState.Get,
                            getVal
                        },
                        {
                            ActionState.Set,
                            setVal
                        }
                    }
                },
                {
                    "limits",
                    new Dictionary<LimitBounds, float>()
                    {
                        {
                            LimitBounds.Min,
                            -offset.range
                        },
                        {
                            LimitBounds.Max,
                            offset.range
                        }
                    }
                }
            });
            slider.Bind(setVal);

            UpdateSlider(slider);
        }

        public void Show(OffsetableComponent _component, Action onSelected, Action onClosed, Action<Vector3Json,Vector3Json> save)
        {
            base.Show(onClosed);
            this.component = _component;
            this._owner = this.component.Item.Parent.GetOwner();
            this._owner.RegisterView(this);

            this._windowSelectAction = onSelected;
            this.saveAction = save;

            this.saveButton.OnClick.AddListener(new UnityAction(this.Save));
            this.revertButton.OnClick.AddListener(new UnityAction(this.Revert));
            this.resetButton.OnClick.AddListener(new UnityAction(this.Reset));

            component.Load();

            // fujky
            AddSliderBinding(sliderPosY, () => component.PosY, (val) => component.PosY = val, PositionRange);
            if (ExtendedMode)
            {
                AddSliderBinding(sliderPosX, () => component.PosX, (val) => component.PosX = val, PositionRange);
                AddSliderBinding(sliderPosZ, () => component.PosZ, (val) => component.PosZ = val, PositionRange);
                AddSliderBinding(sliderRotX, () => component.RotX, (val) => component.RotX = val, RotationRange);
                AddSliderBinding(sliderRotY, () => component.RotY, (val) => component.RotY = val, RotationRange);
                AddSliderBinding(sliderRotZ, () => component.RotZ, (val) => component.RotZ = val, RotationRange);
            }
            else
            {
                sliderPosX.panel?.SetActive(false);
                sliderPosZ.panel?.SetActive(false);
                sliderRotX.panel?.SetActive(false);
                sliderRotY.panel?.SetActive(false);
                sliderRotZ.panel?.SetActive(false);
            }

            // LOCALIZATION
            // Buttons
            revertButton.SetHeaderText("REVERT".Localized().ToUpper(), 24);
            resetButton.SetHeaderText("RESET".Localized().ToUpper(), 24);
            // Slider labels
            sliderPosY?.SetLabelText($"{"Front".Localized().ToSentenceCase()}/{"rear".Localized().ToLower()}");
            sliderPosX?.SetLabelText($"{"Left".Localized().ToSentenceCase()}/{"right".Localized().ToLower()}");
            sliderPosZ?.SetLabelText($"{"Up".Localized().ToSentenceCase()}/{"down".Localized().ToLower()}");
            sliderRotX?.SetLabelText("Pitch".Localized().ToSentenceCase());
            sliderRotY?.SetLabelText("Yaw".Localized().ToSentenceCase());
            sliderRotZ?.SetLabelText("Roll".Localized().ToSentenceCase());
            // Set window title (can't use Caption field because BSG never sets anything to it. smh.)
            transform.Find("Inner/Caption Panel/Caption").GetComponent<CustomTextMeshProUGUI>().text = "CHANGE OFFSET".Localized().ToUpper();
        }
        public void SetupWindow()
        {
            // Get the base of the window UI
            Transform windowBase = transform.Find("Inner");

            // Remove the object with colors and character count
            Transform colorsAndCount = windowBase.Find("Contents/ColorsAndCount");

            // Delete tag input field
            Transform tagInput = windowBase.Find("Contents/TagInput");
            GameObject.DestroyImmediate(tagInput.gameObject);

            // Get split dialog prefab, which contains our slider
            ControlSettingsTab controlSettings = MonoBehaviourSingleton<CommonUI>.Instance.SettingsScreen.GetComponentInChildren<ControlSettingsTab>(true);
            FloatSlider floatSliderPrefab = typeof(ControlSettingsTab).GetField("_mouseSensitivity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controlSettings) as FloatSlider;

            // Create betterSlider prefab
            FloatSlider floatSliderPrefabTemp = GameObject.Instantiate<FloatSlider>(floatSliderPrefab);
            GameObject sliderPrefabObject = floatSliderPrefabTemp.gameObject;
            sliderPrefabObject.SetActive(false);
            sliderPrefabObject.transform.parent = AttachmentsOffset.GameObjectStorage;
            sliderPrefabObject.name = "BetterSlider prefab";
            BetterSlider sliderPrefab = BetterSlider.Create(sliderPrefabObject, floatSliderPrefabTemp);
            GameObject.DestroyImmediate(floatSliderPrefabTemp);

            // Add ContentSizeFitter to the window so it actually adapts to changes in strings etc etc
            ContentSizeFitter windowFitter = windowBase.gameObject.AddComponent<ContentSizeFitter>();
            windowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            windowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sliderPanel = new GameObject("Slider panel", new[] { typeof(VerticalLayoutGroup), typeof(ContentSizeFitter) });
            sliderPanel.transform.parent = windowBase.Find("Contents");
            sliderPanel.transform.SetAsFirstSibling();
            VerticalLayoutGroup layout = sliderPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 0, 0); // No need for vertical padding as that comes from contents layout, but need to fit the side padding as layout doesn't have any
            layout.spacing = 0;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            ContentSizeFitter panelFitter = sliderPanel.GetComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;


            BetterSlider createSlider(string label)
            {
                Vector2 size = new Vector2(350, 20);

                GameObject settingPanel = new GameObject("Slider with label", new[] { typeof(LayoutElement), typeof(HorizontalLayoutGroup) });
                settingPanel.transform.parent = sliderPanel.transform;
                HorizontalLayoutGroup horizontal = settingPanel.GetComponent<HorizontalLayoutGroup>();
                horizontal.childForceExpandHeight = false;
                horizontal.childForceExpandWidth = false;
                horizontal.childControlHeight = false;
                horizontal.childControlWidth = true;
                horizontal.spacing = 10;
                horizontal.childAlignment = UnityEngine.TextAnchor.MiddleCenter;
                RectTransform settingsRect = settingPanel.GetComponent<RectTransform>();
                settingsRect.sizeDelta = new Vector2(settingsRect.sizeDelta.x, 40);

                // Label
                GameObject labelObject = GameObject.Instantiate(colorsAndCount.transform.Find("Text").gameObject, settingPanel.transform);
                labelObject.name = "Label";
                CustomTextMeshProUGUI labelTMP = labelObject.GetComponent<CustomTextMeshProUGUI>();
                labelTMP.text = label;
                labelTMP.color = new Color(0.7647f, 0.7725f, 0.698f, 1f);

                // Spacer
                GameObject spacer = new GameObject("Spacer", new[] { typeof(LayoutElement) });
                spacer.transform.parent = settingPanel.transform;
                LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
                spacerLayout.flexibleWidth = 1;

                // Wrapper panel for slider (for aligning, idk)
                Vector2 sliderPanelSize = size + new Vector2(90, 0);
                GameObject sliderWrapper = new GameObject("Slider wrapper", new[] { typeof(LayoutElement) });
                sliderWrapper.transform.parent = settingPanel.transform;
                sliderWrapper.GetComponent<RectTransform>().sizeDelta = sliderPanelSize;
                LayoutElement sliderWrapperLayout = sliderWrapper.GetComponent<LayoutElement>();
                sliderWrapperLayout.minWidth = sliderPanelSize.x;

                GameObject sliderObject = GameObject.Instantiate(sliderPrefab.gameObject, sliderWrapper.transform);
                sliderObject.SetActive(true);
                UnityEngine.GameObject.Destroy(sliderObject.GetComponent<LayoutElement>());
                sliderObject.name = "Slider";
                sliderObject.GetComponent<RectTransform>().sizeDelta = size;
                sliderObject.transform.localPosition = new Vector3(-35, -10, 0);
                BetterSlider slider = sliderObject.GetComponent<BetterSlider>();
                slider.label = labelTMP;
                slider.panel = settingPanel;

                return slider;
            }

            // Create position sliders
            sliderPosY = createSlider($"{"Front".Localized().ToSentenceCase()}/{"rear".Localized().ToLower()}");
            sliderPosX = createSlider($"{"Left".Localized().ToSentenceCase()}/{"right".Localized().ToLower()}");
            sliderPosZ = createSlider($"{"Up".Localized().ToSentenceCase()}/{"down".Localized().ToLower()}");

            //Create rotation sliders
            sliderRotX = createSlider("Pitch".Localized().ToSentenceCase());
            sliderRotY = createSlider("Yaw".Localized().ToSentenceCase());
            sliderRotZ = createSlider("Roll".Localized().ToSentenceCase());

            // Delete these NOW since we needed it to clone the count text before
            GameObject.DestroyImmediate(colorsAndCount.gameObject);

            // Create a layout for buttons
            GameObject buttonPanel = new GameObject("Button panel", new[] { typeof(HorizontalLayoutGroup)});
            buttonPanel.transform.parent = windowBase.Find("Contents");
            buttonPanel.transform.SetAsLastSibling();
            HorizontalLayoutGroup buttonLayout = buttonPanel.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 10;
            buttonLayout.childForceExpandHeight = false;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childControlHeight = false;
            buttonLayout.childControlWidth = false;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;

            // Get the save button
            saveButton.transform.parent = buttonPanel.transform;

            // Create revert button (returns values back)
            GameObject revertButtonGO = GameObject.Instantiate(saveButton.gameObject, buttonPanel.transform);
            revertButtonGO.transform.SetAsFirstSibling();
            revertButton = revertButtonGO.GetComponent<DefaultUIButton>();

            // Create reset button (nulls the values)
            GameObject resetButtonGO = GameObject.Instantiate(saveButton.gameObject, buttonPanel.transform);
            resetButtonGO.transform.SetAsFirstSibling();
            resetButton = resetButtonGO.GetComponent<DefaultUIButton>();
        }
    }
}
