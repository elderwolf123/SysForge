using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace RamOptimizerNova.Controls;

public partial class MetricsCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MetricsCard, string?>(nameof(Title));
    
    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<MetricsCard, string?>(nameof(Value));
    
    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<MetricsCard, string?>(nameof(Unit));
    
    public static readonly StyledProperty<IBrush?> IconProperty =
        AvaloniaProperty.Register<MetricsCard, IBrush?>(nameof(Icon));
    
    public static readonly StyledProperty<IBrush?> ValueColorProperty =
        AvaloniaProperty.Register<MetricsCard, IBrush?>(nameof(ValueColor));
    
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<MetricsCard, CornerRadius>(nameof(CornerRadius));
    
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<MetricsCard, Thickness>(nameof(Padding));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public IBrush? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public IBrush? ValueColor
    {
        get => GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public MetricsCard()
    {
        InitializeComponent();
        
        // Set default value color
        ValueColor = new SolidColorBrush(Color.Parse("#6366F1"));
        
        // Handle value changes to update color based on value
        this.GetObservable(ValueProperty).Subscribe(value =>
        {
            UpdateValueColor(value);
        });
    }
    
    private void UpdateValueColor(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;
            
        // Try to parse the value as a number
        if (double.TryParse(value.Replace("%", "").Replace("GB", "").Replace("MB", "").Replace("KB", ""), out double numericValue))
        {
            // Set color based on value ranges
            if (numericValue >= 80)
            {
                ValueColor = new SolidColorBrush(Color.Parse("#EF4444")); // Red for high values
            }
            else if (numericValue >= 50)
            {
                ValueColor = new SolidColorBrush(Color.Parse("#F59E0B")); // Yellow for medium values
            }
            else
            {
                ValueColor = new SolidColorBrush(Color.Parse("#10B981")); // Green for low values
            }
        }
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateValueColor(Value);
    }
}