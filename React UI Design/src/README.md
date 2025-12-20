
# NOVA Optimizer - Modular Architecture Guide

## Adding New Optimization Categories

The UI is designed to be fully modular. Adding a new optimization category requires just 2 steps:

### Step 1: Create Your Page Component

Create a new file in `pages/` (e.g., `pages/AudioOptimization.tsx`):

```tsx
import { useState } from 'react'
import { VolumeIcon, MicIcon, HeadphonesIcon } from 'lucide-react'
import { OptimizationPageLayout } from '../components/OptimizationPageLayout'
import { OptimizationToggle } from '../components/OptimizationToggle'
import { OptimizationSlider } from '../components/OptimizationSlider'
import { MetricCard } from '../components/MetricCard'

export function AudioOptimization() {
  const [settings, setSettings] = useState({
    exclusiveMode: true,
    lowLatency: true,
    bufferSize: 512
  })

  return (
    <OptimizationPageLayout
      title="Audio Optimization"
      description="Reduce latency and improve sound quality"
      icon={VolumeIcon}
      iconGradient="from-orange-400 to-amber-500"
      applyButtonGradient="from-orange-400 to-amber-500"
      onApply={() => console.log('Apply audio settings')}
      quickActions={[
        { label: 'Reset Audio Drivers', onClick: () => {} },
        { label: 'Test Latency', onClick: () => {} }
      ]}
    >
      {/* Metrics */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <MetricCard
          title="Latency"
          value={12}
          unit="ms"
          icon={<HeadphonesIcon className="w-5 h-5" />}
          color="orange"
          index={0}
        />
        {/* Add more metrics */}
      </div>

      {/* Controls */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="rounded-2xl bg-space-dark/60 backdrop-blur-xl border border-white/5 p-6">
          <h2 className="text-lg font-orbitron font-semibold text-star-white mb-4">
            Audio Settings
          </h2>
          
          <OptimizationToggle
            label="Exclusive Mode"
            description="Give applications exclusive control of audio device"
            enabled={settings.exclusiveMode}
            onChange={(v) => setSettings(prev => ({ ...prev, exclusiveMode: v }))}
            color="purple"
          />
          
          <OptimizationSlider
            label="Buffer Size"
            description="Lower values reduce latency but may cause crackling"
            value={settings.bufferSize}
            min={128}
            max={2048}
            step={128}
            unit=" samples"
            onChange={(v) => setSettings(prev => ({ ...prev, bufferSize: v }))}
            color="cyan"
          />
        </div>
      </div>
    </OptimizationPageLayout>
  )
}
```

### Step 2: Register in Categories Config

Open `config/categories.tsx` and add your category:

```tsx
import { VolumeIcon } from 'lucide-react'
import { AudioOptimization } from '../pages/AudioOptimization'

export const categories: CategoryConfig[] = [
  // ... existing categories ...
  {
    id: 'audio',
    label: 'Audio',
    icon: VolumeIcon,
    component: AudioOptimization,
    color: 'orange',
    description: 'Audio latency and quality'
  },
]
```

**That's it!** The sidebar and routing will automatically update.

## Available Colors

Choose from these predefined color themes:
- `purple` - Nebula purple/violet
- `cyan` - Bright cyan/blue
- `pink` - Nebula pink/rose
- `green` - Emerald/green
- `orange` - Orange/amber
- `blue` - Blue variants

## Component Library

### OptimizationPageLayout
Wrapper for consistent page structure with header, content area, and apply button.

### MetricCard
Display system metrics with animated progress bars and optional warning states.

### OptimizationToggle
Toggle switch with spring animation, description, and color variants.

### OptimizationSlider
Range slider with real-time value display and custom min/max/step.

## Architecture Benefits

✅ **Zero Core File Changes** - Add categories without touching App.tsx or Sidebar.tsx  
✅ **Type-Safe** - Full TypeScript support with CategoryConfig interface  
✅ **Consistent Design** - Reusable components maintain visual consistency  
✅ **Easy Maintenance** - All categories in one config file  
✅ **Scalable** - Add unlimited categories without performance impact  

## File Structure

```
src/
├── config/
│   └── categories.tsx       # Category registry (modify this to add categories)
├── types/
│   └── category.ts          # TypeScript interfaces
├── components/
│   ├── StarField.tsx        # Animated background
│   ├── Sidebar.tsx          # Dynamic navigation
│   ├── MetricCard.tsx       # Metric display
│   ├── OptimizationToggle.tsx
│   ├── OptimizationSlider.tsx
│   └── OptimizationPageLayout.tsx  # Page template
├── pages/
│   ├── Dashboard.tsx
│   ├── CPUOptimization.tsx
│   ├── GPUOptimization.tsx
│   └── [YourNewCategory].tsx  # Add new pages here
└── App.tsx                   # Main app (no changes needed)
```

## Best Practices

1. **Use OptimizationPageLayout** - Maintains consistent structure and animations
2. **Follow naming conventions** - `[Category]Optimization.tsx` for page files
3. **Choose appropriate colors** - Match the category's purpose (e.g., orange for audio, green for storage)
4. **Add descriptions** - Help users understand what each category optimizes
5. **Include quick actions** - Provide one-click utilities when relevant
