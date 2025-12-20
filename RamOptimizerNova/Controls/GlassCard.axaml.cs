using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace RamOptimizerNova.Controls;

public partial class GlassCard : UserControl
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<GlassCard, IBrush?>(nameof(Background));
    
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<GlassCard, IBrush?>(nameof(BorderBrush));
    
    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<GlassCard, Thickness>(nameof(Padding));
    
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<GlassCard, CornerRadius>(nameof(CornerRadius));
    
    public static readonly StyledProperty<BoxShadow?> ShadowProperty =
        AvaloniaProperty.Register<GlassCard, BoxShadow?>(nameof(Shadow));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public BoxShadow? Shadow
    {
        get => GetValue(ShadowProperty);
        set => SetValue(ShadowProperty, value);
    }

    public GlassCard()
    {
        InitializeComponent();
    }
}