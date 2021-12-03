using EFT.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using AttachmentsOffset.Utils;

namespace AttachmentsOffset
{
    public class BetterSlider : MonoBehaviour, GInterface238<float>
    {
		public static BetterSlider Create(GameObject parentObject, FloatSlider inherited)
        {
			if (parentObject == null || inherited == null) return null;

			BetterSlider result = parentObject.AddComponent<BetterSlider>();
			result.CopyComponentFields<BetterSlider,FloatSlider>(inherited);
			return result;
        }

		private void Awake()
		{
			this._slider.onValueChanged.AddListener(new UnityAction<float>(this.OnUpdateValue));
			if (this._valueInput != null)
			{
				this._valueInput.interactable = true;
				this._valueInput.onEndEdit.AddListener(new UnityAction<string>(this.InputEndEdit));
			}
		}

		private void InputEndEdit(string value)
		{
			float num;
			if (!float.TryParse(value.Replace(',','.'), out num))
			{
				this._valueInput.text = this.CurrentValue().ToString(CultureInfo.InvariantCulture);
			}
			else if (num < this._slider.minValue)
			{
				this._valueInput.text = this._slider.minValue.ToString();
			}
			else if (num > this._slider.maxValue)
			{
				this._valueInput.text = this._slider.maxValue.ToString();
			}
			UpdateValue(num, false);
		}

		private void OnUpdateValue(float value)
		{
			value = GClass495.RoundFloatValue(value, 2);
			string text = value.ToString(CultureInfo.InvariantCulture);
			if (this._valueInput != null && this._valueInput.text != text)
			{
				this._valueInput.SetTextWithoutNotify(text);
			}
			if (this._slider.value.ToString(CultureInfo.InvariantCulture) != text)
			{
				this._slider.value = value;
			}
			Action<float> action = this._valueChanged;
			if (action == null)
			{
				return;
			}
			action(value);
		}

		public void Show(float minValue, float maxValue)
		{
			this._slider.minValue = minValue;
			this._slider.maxValue = maxValue;
		}

		public void SetLabelText(string labelText)
        {
			if (label != null)
   				this.label.text = labelText;
		}

		public void Hide()
        {
			this._valueInput.onEndEdit.RemoveAllListeners();
			this._slider.onValueChanged.RemoveAllListeners();
		}

		public void UpdateValue(float value, bool sendCallback = true, float? min = null, float? max = null)
		{
			if (Math.Abs(this._slider.maxValue) < Mathf.Epsilon)
			{
				return;
			}
			value = ((min == null || max == null) ? Mathf.Clamp(value, this._slider.minValue, this._slider.maxValue) : Mathf.Clamp(value, min.Value, max.Value));
			this.OnUpdateValue(value);
		}

		public void Bind(Action<float> valueChanged)
		{
			this._valueChanged = valueChanged;
		}

		public float CurrentValue()
		{
			return this._slider.value;
		}

#pragma warning disable 0649
		[SerializeField]
		private Slider _slider;

		[SerializeField]
		private CustomTextMeshProInputField _valueInput;

		[SerializeField]
		public CustomTextMeshProUGUI label;

		[SerializeField]
		public GameObject panel;
#pragma warning restore 0649

		private Action<float> _valueChanged;
	}
}
