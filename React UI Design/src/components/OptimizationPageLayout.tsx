import React from 'react';
import { motion } from 'framer-motion';
import { BoxIcon } from 'lucide-react';
type OptimizationPageLayoutProps = {
  title: string;
  description: string;
  icon: BoxIcon;
  iconGradient: string;
  children: ReactNode;
  onApply?: () => void;
  applyButtonGradient?: string;
  quickActions?: Array<{
    label: string;
    onClick: () => void;
  }>;
};
/**
 * REUSABLE PAGE LAYOUT TEMPLATE
 *
 * Use this component as a wrapper for all optimization pages to maintain
 * consistent structure and animations.
 *
 * Example usage:
 *
 * <OptimizationPageLayout
 *   title="Audio Optimization"
 *   description="Reduce latency and improve sound quality"
 *   icon={VolumeIcon}
 *   iconGradient="from-orange-400 to-amber-500"
 *   applyButtonGradient="from-orange-400 to-amber-500"
 *   onApply={() => console.log('Apply changes')}
 * >
 *   <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
 *     {/* Your optimization controls here *\/}
 *   </div>
 * </OptimizationPageLayout>
 */
export function OptimizationPageLayout({
  title,
  description,
  icon: Icon,
  iconGradient,
  children,
  onApply,
  applyButtonGradient = 'from-nebula-purple to-nebula-violet',
  quickActions
}: OptimizationPageLayoutProps) {
  return <motion.div initial={{
    opacity: 0,
    x: 20
  }} animate={{
    opacity: 1,
    x: 0
  }} exit={{
    opacity: 0,
    x: -20
  }} transition={{
    duration: 0.3
  }} className="p-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-3 mb-2">
          <div className={`p-2 rounded-lg bg-gradient-to-br ${iconGradient}`}>
            <Icon className="w-6 h-6 text-white" />
          </div>
          <h1 className="text-3xl font-orbitron font-bold text-star-white">
            {title}
          </h1>
        </div>
        <p className="text-star-dim">{description}</p>
      </div>

      {/* Main content */}
      {children}

      {/* Quick Actions (optional) */}
      {quickActions && quickActions.length > 0 && <motion.div initial={{
      opacity: 0,
      y: 20
    }} animate={{
      opacity: 1,
      y: 0
    }} transition={{
      delay: 0.4
    }} className="mt-6 rounded-2xl bg-space-dark/60 backdrop-blur-xl border border-white/5 p-6">
          <h2 className="text-lg font-orbitron font-semibold text-star-white mb-4">
            Quick Actions
          </h2>
          <div className="flex flex-wrap gap-3">
            {quickActions.map((action, index) => <button key={index} onClick={action.onClick} className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
                {action.label}
              </button>)}
          </div>
        </motion.div>}

      {/* Apply Button */}
      {onApply && <motion.div initial={{
      opacity: 0
    }} animate={{
      opacity: 1
    }} transition={{
      delay: 0.5
    }} className="mt-8 flex justify-end">
          <button onClick={onApply} className={`px-8 py-3 rounded-xl bg-gradient-to-r ${applyButtonGradient} text-white font-orbitron font-semibold tracking-wide hover:shadow-lg hover:shadow-nebula-purple/30 transition-shadow`}>
            Apply Changes
          </button>
        </motion.div>}
    </motion.div>;
}