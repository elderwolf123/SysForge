using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace RamOptimizerNova.Controls;

public partial class ProgressRing : UserControl
{
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(StrokeThickness), 3);
    
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<ProgressRing, IBrush?>(nameof(Stroke));
    
    public static readonly StyledProperty<double> PercentageProperty =
        AvaloniaProperty.Register<ProgressRing, double>(nameof(Percentage), 0);
    
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<ProgressRing, bool>(nameof(IsActive), false);

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double Percentage
    {
        get => GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, Math.Max(0, Math.Min(100, value)));
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private DispatcherTimer? _animationTimer;
    private double _currentOffset = 0;
    private int _direction = 1;

    public ProgressRing()
    {
        InitializeComponent();
        
        // Set default stroke color
        Stroke = new SolidColorBrush(Color.Parse("#6366F1"));
        
        // Handle active state changes
        this.GetObservable(IsActiveProperty).Subscribe(active =>
        {
            UpdateActiveState(active);
        });
        
        // Handle percentage changes
        this.GetObservable(PercentageProperty).Subscribe(percentage =>
        {
            UpdatePercentage(percentage);
        });
        
        // Initialize animation
        InitializeAnimation();
    }
    
    private void InitializeAnimation()
    {
        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        
        _animationTimer.Tick += (sender, e) =>
        {
            if (!IsActive)
                return;
                
            // Animate the stroke dash offset
            _currentOffset += _direction * 2;
            
            if (_currentOffset >= 126)
            {
                _currentOffset = 126;
                _direction = -1;
            }
            else if (_currentOffset <= 0)
            {
                _currentOffset = 0;
                _direction = 1;
            }
            
            StrokeDashOffset = _currentOffset;
        };
    }
    
    private void UpdateActiveState(bool active)
    {
        if (_animationTimer == null)
            return;
            
        if (active)
        {
            _animationTimer.Start();
            PseudoClasses.Add("active");
            PseudoClasses.Remove("inactive");
        }
        else
        {
            _animationTimer.Stop();
            PseudoClasses.Remove("active");
            PseudoClasses.Add("inactive");
        }
    }
    
    private void UpdatePercentage(double percentage)
    {
        // Calculate stroke dash array based on percentage
        var circumference = 2 * Math.PI * 18; // radius = 18
        var dashLength = (percentage / 100) * circumference;
        var gapLength = circumference - dashLength;
        
        StrokeDashArray = new Avalonia.Collections.AvaloniaList<double> { dashLength, gapLength };
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateActiveState(IsActive);
        UpdatePercentage(Percentage);
    }
    
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _animationTimer?.Stop();
    }
}