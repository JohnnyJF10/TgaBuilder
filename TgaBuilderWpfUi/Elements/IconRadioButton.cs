using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace TgaBuilderWpfUi.Elements
{
    internal class IconRadioButton : RadioButton
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(IconElement),
            typeof(IconRadioButton),
            new PropertyMetadata(null, null, IconElement.Coerce)
            );

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
             nameof(CornerRadius),
             typeof(CornerRadius),
             typeof(IconRadioButton),
             new FrameworkPropertyMetadata(
                 default(CornerRadius),
                 FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender
             ));

        [Bindable(true)]
        [Category("Appearance")]
        public IconElement? Icon
        {
            get => (IconElement?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, (object)value);
        }

        public IconRadioButton()
        {
            DefaultStyleKey = typeof(IconRadioButton);
        }
    }
}
