using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFMaskedTextBox.Enums;
using WPFMaskedTextBox.Filters;

namespace WPFMaskedTextBox
{
	public class MaskedTextBox : TextBox
	{
		#region Properties

		/// <summary>
		/// Gets or sets the cached mask to apply to the TextBox
		/// </summary>
		private MaskedTextProvider? MaskProviderCached
		{
			get => (MaskedTextProvider)GetValue(MaskProviderCachedProperty);
			set => SetValue(MaskProviderCachedProperty, value);
		}

		/// <summary>
		/// Dependency property to store the cached mask to apply to the TextBox
		/// </summary>
		private static readonly DependencyProperty MaskProviderCachedProperty =
			DependencyProperty.Register(nameof(MaskProviderCached), typeof(MaskedTextProvider), typeof(MaskedTextBox), new UIPropertyMetadata(null, MaskChanged));

		/// <summary>
		/// Gets or sets the cached mask format string to apply to the TextBox
		/// </summary>
		private string MaskProviderCachedMask
		{
			get => (string)GetValue(MaskProviderCachedMaskProperty);
			set => SetValue(MaskProviderCachedMaskProperty, value);
		}

		/// <summary>
		/// Dependency property to store the mask format string to apply to the TextBox
		/// </summary>
		private static readonly DependencyProperty MaskProviderCachedMaskProperty =
			DependencyProperty.Register(nameof(MaskProviderCachedMask), typeof(string), typeof(MaskedTextBox), new UIPropertyMetadata(string.Empty, MaskChanged));

		/// <summary>
		/// Gets the MaskTextProvider for the specified Mask
		/// </summary>
		public MaskedTextProvider? MaskProvider
		{
			get
			{
				if (!IsMaskProviderUpdated())
				{
					return MaskProviderCached;
				}

				MaskProviderCachedMask = Mask;
				MaskProviderCached = string.IsNullOrEmpty(MaskProviderCachedMask) ? null : new MaskedTextProvider(MaskProviderCachedMask) { PromptChar = PromptChar };
				MaskProviderCached?.Set(Text);
				return MaskProviderCached;
			}
		}

		/// <summary>
		/// Check, is configuration of MaskProvider changed
		/// </summary>
		/// <returns><c>true</c>, if it was changed and <c>false</c>, if it wasn't.</returns>
		private bool IsMaskProviderUpdated()
		{
			var result = false;
			if (MaskProviderCachedMask != Mask)
			{
				MaskProviderCachedMask = Mask;
				result = true;
			}
			if (PromptCharCached != PromptChar)
			{
				PromptCharCached = PromptChar;
				result = true;
			}
			return result;
		}

		/// <summary>
		/// Gets or sets the cached prompt char to apply to the TextBox mask
		/// </summary>
		private char PromptCharCached
		{
			get => (char)GetValue(PromptCharCachedProperty);
			set => SetValue(PromptCharCachedProperty, value);
		}

		/// <summary>
		/// Dependency property to store the cached prompt char to apply to the TextBox mask
		/// </summary>
		private static readonly DependencyProperty PromptCharCachedProperty =
			DependencyProperty.Register(nameof(PromptCharCached), typeof(char), typeof(MaskedTextBox), new UIPropertyMetadata(' ', MaskChanged));

		/// <summary>
		/// Gets or sets the prompt char to apply to the TextBox mask
		/// </summary>
		public char PromptChar
		{
			get => (char)GetValue(PromptCharProperty);
			set => SetValue(PromptCharProperty, value);
		}

		/// <summary>
		/// Dependency property to store the prompt char to apply to the TextBox mask
		/// </summary>
		public static readonly DependencyProperty PromptCharProperty =
			DependencyProperty.Register(nameof(PromptChar), typeof(char), typeof(MaskedTextBox), new UIPropertyMetadata(' ', MaskChanged));

		/// <summary>
		/// Gets or sets the mask to apply to the TextBox
		/// </summary>
		public string Mask
		{
			get => (string)GetValue(MaskProperty);
			set => SetValue(MaskProperty, value);
		}

		/// <summary>
		/// Dependency property to store the mask to apply to the TextBox
		/// </summary>
		public static readonly DependencyProperty MaskProperty =
			DependencyProperty.Register(nameof(Mask), typeof(string), typeof(MaskedTextBox), new UIPropertyMetadata(string.Empty, MaskChanged));

		//callback for when the Mask property is changed
		private static void MaskChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			//make sure to update the text if the mask changes
			if (sender is MaskedTextBox textBox)
			{
				textBox.RefreshText(textBox.MaskProvider, 0);
			}
		}

		/// <summary>
		/// Gets the RegExFilter for the validation Mask.
		/// </summary>
		private DefaultFilter FilterValidator => FilterProvider.Instance.FilterForMaskedType(Filter);

		/// <summary>
		/// Gets a predefined filter for the specified RegExp
		/// </summary>
		public FilterType Filter
		{
			get => (FilterType)GetValue(FilterProperty);
			set => SetValue(FilterProperty, value);
		}

		/// <summary>
		/// Dependency property to store the filter to apply to the TextBox
		/// </summary>
		public static readonly DependencyProperty FilterProperty =
			DependencyProperty.Register(nameof(Filter), typeof(FilterType), typeof(MaskedTextBox), new UIPropertyMetadata(FilterType.Any, MaskChanged));

		#endregion

		/// <summary>
		/// Static Constructor
		/// </summary>
		static MaskedTextBox()
		{
			//override the meta data for the Text Property of the TextBox
			var metaData = new FrameworkPropertyMetadata { CoerceValueCallback = ForceText };
			TextProperty.OverrideMetadata(typeof(MaskedTextBox), metaData);
		}

		//force the text of the control to use the mask
		private static object? ForceText(DependencyObject sender, object? value)
		{
			if (sender is MaskedTextBox textBox)
			{
				if (!string.IsNullOrEmpty(textBox.Mask))
				{
					var provider = textBox.MaskProvider;
					if (provider is not null)
					{
						provider.Set($@"{value}");
						return provider.ToDisplayString();
					}
				}
			}
			return value;
		}

		#region Overrides

		/// <summary>
		/// override this method to replace the characters entered with the mask
		/// </summary>
		/// <param name="e">Arguments for event</param>
		protected override void OnPreviewTextInput(TextCompositionEventArgs e)
		{
			//if the text is readonly do not add the text
			if (IsReadOnly)
			{
				e.Handled = true;
				return;
			}

			var position = SelectionStart;
			var provider = MaskProvider;
			var ifIsPositionInMiddle = position < Text.Length;
			if (provider is not null)
			{
				if (ifIsPositionInMiddle)
				{
					position = GetNextCharacterPosition(position);

					if (Keyboard.IsKeyToggled(Key.Insert))
					{
						if (provider.Replace(e.Text, position))
						{
							position++;
						}
					}
					else
					{
						if (provider.InsertAt(e.Text, position))
						{
							position++;
						}
					}

					position = GetNextCharacterPosition(position);
				}

				RefreshText(provider, position);
				e.Handled = true;
			}

			var textToText = ifIsPositionInMiddle ? Text.Insert(position, e.Text) : $@"{Text}{e.Text}";
			if (!FilterValidator.IsTextValid(textToText))
			{
				e.Handled = true;
			}
			base.OnPreviewTextInput(e);
		}

		/// <summary>
		/// override the key down to handle delete of a character
		/// </summary>
		/// <param name="e">Arguments for the event</param>
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			var provider = MaskProvider;
			if (provider is null)
			{
				return;
			}

			var position = SelectionStart;
			switch (e.Key)
			{
				case Key.Delete:
					if (position < Text.Length)
					{
						if (provider.RemoveAt(position))
						{
							RefreshText(provider, position);
						}

						e.Handled = true;
					}
					break;
				case Key.Space:
					if (provider.InsertAt(@" ", position))
					{
						RefreshText(provider, position);
					}

					e.Handled = true;
					break;
				case Key.Back:
					if (position > 0)
					{
						position--;
						if (provider.RemoveAt(position))
						{
							RefreshText(provider, position);
						}
					}
					e.Handled = true;
					break;
			}
		}

		#endregion

		#region Helper Methods

		//refreshes the text of the TextBox
		private void RefreshText(MaskedTextProvider? provider, int position)
		{
			if (provider is not null)
			{
				Text = provider.ToDisplayString();
				SelectionStart = position;
			}
		}

		//gets the next position in the TextBox to move
		private int GetNextCharacterPosition(int startPosition)
		{
			if (MaskProvider is not null)
			{
				var position = MaskProvider.FindEditPositionFrom(startPosition, true);
				if (position != -1)
				{
					return position;
				}
			}
			return startPosition;
		}

		#endregion
	}
}
