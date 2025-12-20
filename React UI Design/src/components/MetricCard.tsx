import React from 'react';
import { motion } from 'framer-motion';
type MetricCardProps = {
  title: string;
  value: string | number;
  unit?: string;
  icon: ReactNode;
  percentage?: number;
  trend?: 'up' | 'down' | 'stable';
  color?: 'purple' | 'cyan' | 'pink' | 'green';
  index?: number;
  isWarning?: boolean;
};
export function MetricCard({
  title,
  value,
  unit,
  icon,
  percentage,
  trend,
  color = 'purple',
  index = 0,
  isWarning = false
}: MetricCardProps) {
  const colorClasses = {
    purple: {
      gradient: 'from-nebula-purple to-nebula-violet',
      glow: 'shadow-nebula-purple/30',
      text: 'text-nebula-purple',
      bg: 'bg-nebula-purple'
    },
    cyan: {
      gradient: 'from-nebula-cyan to-blue-400',
      glow: 'shadow-nebula-cyan/30',
      text: 'text-nebula-cyan',
      bg: 'bg-nebula-cyan'
    },
    pink: {
      gradient: 'from-nebula-pink to-rose-400',
      glow: 'shadow-nebula-pink/30',
      text: 'text-nebula-pink',
      bg: 'bg-nebula-pink'
    },
    green: {
      gradient: 'from-emerald-400 to-green-500',
      glow: 'shadow-emerald-400/30',
      text: 'text-emerald-400',
      bg: 'bg-emerald-400'
    }
  };
  const colors = colorClasses[color];
  return <motion.div initial={{
    opacity: 0,
    y: 20
  }} animate={{
    opacity: 1,
    y: 0
  }} transition={{
    delay: index * 0.1,
    duration: 0.5,
    ease: 'easeOut'
  }} className={`
        relative overflow-hidden rounded-2xl p-5
        bg-space-dark/60 backdrop-blur-xl
        border border-white/5
        ${isWarning ? 'pulse-warning' : ''}
      `}>
      {/* Gradient accent line */}
      <div className={`absolute top-0 left-0 right-0 h-0.5 bg-gradient-to-r ${colors.gradient}`} />

      {/* Background glow */}
      <div className={`absolute -top-20 -right-20 w-40 h-40 rounded-full bg-gradient-to-br ${colors.gradient} opacity-10 blur-3xl`} />

      <div className="relative z-10">
        {/* Header */}
        <div className="flex items-center justify-between mb-4">
          <div className={`p-2.5 rounded-xl bg-gradient-to-br ${colors.gradient} bg-opacity-20`}>
            <span className="text-white/90">{icon}</span>
          </div>
          {trend && <div className={`flex items-center gap-1 text-sm ${trend === 'up' ? 'text-red-400' : trend === 'down' ? 'text-emerald-400' : 'text-star-dim'}`}>
              {trend === 'up' && '↑'}
              {trend === 'down' && '↓'}
              {trend === 'stable' && '→'}
            </div>}
        </div>

        {/* Title */}
        <p className="text-star-dim text-sm font-medium mb-1 uppercase tracking-wider">
          {title}
        </p>

        {/* Value */}
        <div className="flex items-baseline gap-1.5">
          <span className="text-3xl font-orbitron font-bold text-star-white">
            {value}
          </span>
          {unit && <span className="text-star-dim text-sm font-medium">{unit}</span>}
        </div>

        {/* Progress bar */}
        {percentage !== undefined && <div className="mt-4">
            <div className="h-1.5 bg-white/5 rounded-full overflow-hidden">
              <motion.div initial={{
            width: 0
          }} animate={{
            width: `${percentage}%`
          }} transition={{
            delay: index * 0.1 + 0.3,
            duration: 0.8,
            ease: 'easeOut'
          }} className={`h-full rounded-full bg-gradient-to-r ${colors.gradient}`} />
            </div>
            <p className={`text-xs mt-1.5 ${colors.text}`}>
              {percentage}% utilized
            </p>
          </div>}
      </div>
    </motion.div>;
}