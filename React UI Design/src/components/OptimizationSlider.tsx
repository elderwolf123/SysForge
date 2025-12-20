import React from 'react';
import { motion } from 'framer-motion';
type OptimizationSliderProps = {
  label: string;
  description?: string;
  value: number;
  min?: number;
  max?: number;
  step?: number;
  unit?: string;
  onChange: (value: number) => void;
  color?: 'purple' | 'cyan' | 'green';
};
export function OptimizationSlider({
  label,
  description,
  value,
  min = 0,
  max = 100,
  step = 1,
  unit = '%',
  onChange,
  color = 'purple'
}: OptimizationSliderProps) {
  const percentage = (value - min) / (max - min) * 100;
  const colorClasses = {
    purple: {
      gradient: 'from-nebula-purple to-nebula-violet',
      glow: 'shadow-nebula-purple/50'
    },
    cyan: {
      gradient: 'from-nebula-cyan to-blue-400',
      glow: 'shadow-nebula-cyan/50'
    },
    green: {
      gradient: 'from-emerald-400 to-green-500',
      glow: 'shadow-emerald-400/50'
    }
  };
  const colors = colorClasses[color];
  return <div className="py-4 border-b border-white/5 last:border-0">
      <div className="flex items-center justify-between mb-3">
        <div>
          <p className="text-star-white font-medium">{label}</p>
          {description && <p className="text-star-dim text-sm mt-0.5">{description}</p>}
        </div>
        <div className="flex items-baseline gap-1">
          <span className="text-xl font-orbitron font-bold text-star-white">
            {value}
          </span>
          <span className="text-star-dim text-sm">{unit}</span>
        </div>
      </div>

      <div className="relative">
        {/* Track background */}
        <div className="h-2 bg-white/5 rounded-full overflow-hidden">
          {/* Filled track */}
          <motion.div initial={{
          width: 0
        }} animate={{
          width: `${percentage}%`
        }} transition={{
          duration: 0.3
        }} className={`h-full rounded-full bg-gradient-to-r ${colors.gradient}`} />
        </div>

        {/* Slider input */}
        <input type="range" min={min} max={max} step={step} value={value} onChange={e => onChange(Number(e.target.value))} className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" />

        {/* Custom thumb */}
        <motion.div initial={{
        left: 0
      }} animate={{
        left: `calc(${percentage}% - 8px)`
      }} transition={{
        duration: 0.3
      }} className={`
            absolute top-1/2 -translate-y-1/2
            w-4 h-4 rounded-full
            bg-white shadow-lg ${colors.glow}
            pointer-events-none
          `} />
      </div>

      {/* Scale markers */}
      <div className="flex justify-between mt-2 text-xs text-star-dim">
        <span>
          {min}
          {unit}
        </span>
        <span>
          {max}
          {unit}
        </span>
      </div>
    </div>;
}