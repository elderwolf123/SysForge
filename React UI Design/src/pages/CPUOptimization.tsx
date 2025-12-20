import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { OptimizationToggle } from '../components/OptimizationToggle';
import { OptimizationSlider } from '../components/OptimizationSlider';
import { MetricCard } from '../components/MetricCard';
import { CpuIcon, ZapIcon, GaugeIcon, ThermometerIcon } from 'lucide-react';
export function CPUOptimization() {
  const [settings, setSettings] = useState({
    priorityBoost: true,
    coreParking: false,
    powerPlan: true,
    threadOptimization: true,
    affinityControl: false,
    cpuPriority: 75,
    coreLimit: 100,
    powerLimit: 95
  });
  const updateSetting = (key: string, value: boolean | number) => {
    setSettings(prev => ({
      ...prev,
      [key]: value
    }));
  };
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
          <div className="p-2 rounded-lg bg-gradient-to-br from-nebula-purple to-nebula-violet">
            <CpuIcon className="w-6 h-6 text-white" />
          </div>
          <h1 className="text-3xl font-orbitron font-bold text-star-white">
            CPU Optimization
          </h1>
        </div>
        <p className="text-star-dim">
          Fine-tune processor performance and power efficiency
        </p>
      </div>

      {/* Current Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <MetricCard title="Current Usage" value={42} unit="%" icon={<CpuIcon className="w-5 h-5" />} percentage={42} color="purple" index={0} />
        <MetricCard title="Clock Speed" value={4.2} unit="GHz" icon={<ZapIcon className="w-5 h-5" />} color="cyan" index={1} />
        <MetricCard title="Active Cores" value="8/8" icon={<GaugeIcon className="w-5 h-5" />} color="green" index={2} />
        <MetricCard title="Temperature" value={58} unit="°C" icon={<ThermometerIcon className="w-5 h-5" />} color="pink" index={3} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Toggle Settings */}
        <motion.div initial={{
        opacity: 0,
        y: 20
      }} animate={{
        opacity: 1,
        y: 0
      }} transition={{
        delay: 0.2
      }} className="rounded-2xl bg-space-dark/60 backdrop-blur-xl border border-white/5 p-6">
          <h2 className="text-lg font-orbitron font-semibold text-star-white mb-4">
            Performance Tweaks
          </h2>

          <OptimizationToggle label="CPU Priority Boost" description="Prioritize foreground applications for better responsiveness" enabled={settings.priorityBoost} onChange={v => updateSetting('priorityBoost', v)} color="purple" />
          <OptimizationToggle label="Disable Core Parking" description="Keep all CPU cores active for maximum performance" enabled={settings.coreParking} onChange={v => updateSetting('coreParking', v)} color="cyan" />
          <OptimizationToggle label="High Performance Power Plan" description="Use aggressive power settings for peak performance" enabled={settings.powerPlan} onChange={v => updateSetting('powerPlan', v)} color="green" />
          <OptimizationToggle label="Thread Optimization" description="Optimize thread scheduling for multi-core workloads" enabled={settings.threadOptimization} onChange={v => updateSetting('threadOptimization', v)} color="purple" />
          <OptimizationToggle label="Process Affinity Control" description="Manually control which cores run specific processes" enabled={settings.affinityControl} onChange={v => updateSetting('affinityControl', v)} color="cyan" />
        </motion.div>

        {/* Slider Settings */}
        <motion.div initial={{
        opacity: 0,
        y: 20
      }} animate={{
        opacity: 1,
        y: 0
      }} transition={{
        delay: 0.3
      }} className="rounded-2xl bg-space-dark/60 backdrop-blur-xl border border-white/5 p-6">
          <h2 className="text-lg font-orbitron font-semibold text-star-white mb-4">
            Advanced Controls
          </h2>

          <OptimizationSlider label="Process Priority Level" description="Default priority for foreground applications" value={settings.cpuPriority} onChange={v => updateSetting('cpuPriority', v)} color="purple" />
          <OptimizationSlider label="Active Core Limit" description="Maximum cores available for applications" value={settings.coreLimit} onChange={v => updateSetting('coreLimit', v)} color="cyan" />
          <OptimizationSlider label="Power Limit" description="Maximum TDP allowed for the processor" value={settings.powerLimit} onChange={v => updateSetting('powerLimit', v)} color="green" />
        </motion.div>
      </div>

      {/* Apply Button */}
      <motion.div initial={{
      opacity: 0
    }} animate={{
      opacity: 1
    }} transition={{
      delay: 0.5
    }} className="mt-8 flex justify-end">
        <button className="px-8 py-3 rounded-xl bg-gradient-to-r from-nebula-purple to-nebula-violet text-white font-orbitron font-semibold tracking-wide hover:shadow-lg hover:shadow-nebula-purple/30 transition-shadow">
          Apply Changes
        </button>
      </motion.div>
    </motion.div>;
}