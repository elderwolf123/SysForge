import React from 'react';
import { motion } from 'framer-motion';
type OptimizationToggleProps = {
  label: string;
  description?: string;
  enabled: boolean;
  onChange: (enabled: boolean) => void;
  color?: 'purple' | 'cyan' | 'green';
};
export function OptimizationToggle({
  label,
  description,
  enabled,
  onChange,
  color = 'purple'
}: OptimizationToggleProps) {
  const colorClasses = {
    purple: 'bg-nebula-purple',
    cyan: 'bg-nebula-cyan',
    green: 'bg-emerald-500'
  };
  return <div className="flex items-center justify-between py-4 border-b border-white/5 last:border-0">
      <div className="flex-1 pr-4">
        <p className="text-star-white font-medium">{label}</p>
        {description && <p className="text-star-dim text-sm mt-0.5">{description}</p>}
      </div>

      <button onClick={() => onChange(!enabled)} className={`
          relative w-14 h-7 rounded-full transition-colors duration-300
          ${enabled ? colorClasses[color] : 'bg-white/10'}
          focus:outline-none focus:ring-2 focus:ring-nebula-purple/50
        `} role="switch" aria-checked={enabled}>
        <motion.div initial={false} animate={{
        x: enabled ? 28 : 4,
        scale: enabled ? 1 : 0.9
      }} transition={{
        type: 'spring',
        stiffness: 500,
        damping: 30
      }} className={`
            absolute top-1 w-5 h-5 rounded-full
            bg-white shadow-lg
            ${enabled ? 'shadow-white/30' : ''}
          `} />

        {/* Glow effect when enabled */}
        {enabled && <motion.div initial={{
        opacity: 0
      }} animate={{
        opacity: 1
      }} className={`absolute inset-0 rounded-full ${colorClasses[color]} blur-md opacity-40`} />}
      </button>
    </div>;
}