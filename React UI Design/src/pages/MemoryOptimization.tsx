import React, { useState, memo } from 'react';
import { motion } from 'framer-motion';
import { OptimizationToggle } from '../components/OptimizationToggle';
import { OptimizationSlider } from '../components/OptimizationSlider';
import { MetricCard } from '../components/MetricCard';
import { MemoryStickIcon, DatabaseIcon, ArchiveIcon, RefreshCwIcon } from 'lucide-react';
export function MemoryOptimization() {
  const [settings, setSettings] = useState({
    memoryCompression: true,
    superfetch: false,
    prefetch: true,
    virtualMemory: true,
    memoryPriority: false,
    pagingFileSize: 8192,
    standbyListClean: 70,
    workingSetLimit: 80
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
          <div className="p-2 rounded-lg bg-gradient-to-br from-nebula-pink to-rose-400">
            <MemoryStickIcon className="w-6 h-6 text-white" />
          </div>
          <h1 className="text-3xl font-orbitron font-bold text-star-white">
            Memory Optimization
          </h1>
        </div>
        <p className="text-star-dim">
          Optimize RAM usage and virtual memory settings
        </p>
      </div>

      {/* Current Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <MetricCard title="RAM Used" value={8.2} unit="GB" icon={<MemoryStickIcon className="w-5 h-5" />} percentage={51} color="pink" index={0} />
        <MetricCard title="Available" value={7.8} unit="GB" icon={<DatabaseIcon className="w-5 h-5" />} color="green" index={1} />
        <MetricCard title="Cached" value={4.2} unit="GB" icon={<ArchiveIcon className="w-5 h-5" />} color="purple" index={2} />
        <MetricCard title="Page File" value={2.1} unit="GB" icon={<RefreshCwIcon className="w-5 h-5" />} color="cyan" index={3} />
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
            Memory Features
          </h2>

          <OptimizationToggle label="Memory Compression" description="Compress inactive memory to free up RAM" enabled={settings.memoryCompression} onChange={v => updateSetting('memoryCompression', v)} color="purple" />
          <OptimizationToggle label="Disable Superfetch" description="Stop Windows from preloading apps into memory" enabled={settings.superfetch} onChange={v => updateSetting('superfetch', v)} color="cyan" />
          <OptimizationToggle label="Prefetch Optimization" description="Optimize application prefetching behavior" enabled={settings.prefetch} onChange={v => updateSetting('prefetch', v)} color="green" />
          <OptimizationToggle label="Virtual Memory Management" description="Automatically manage paging file size" enabled={settings.virtualMemory} onChange={v => updateSetting('virtualMemory', v)} color="purple" />
          <OptimizationToggle label="Memory Priority Boost" description="Prioritize memory for foreground applications" enabled={settings.memoryPriority} onChange={v => updateSetting('memoryPriority', v)} color="cyan" />
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
            Memory Limits
          </h2>

          <OptimizationSlider label="Paging File Size" description="Maximum size of the virtual memory file" value={settings.pagingFileSize} min={2048} max={16384} step={512} unit="MB" onChange={v => updateSetting('pagingFileSize', v)} color="purple" />
          <OptimizationSlider label="Standby List Cleanup" description="Threshold to clear cached memory" value={settings.standbyListClean} onChange={v => updateSetting('standbyListClean', v)} color="cyan" />
          <OptimizationSlider label="Working Set Limit" description="Maximum memory per application" value={settings.workingSetLimit} onChange={v => updateSetting('workingSetLimit', v)} color="green" />
        </motion.div>
      </div>

      {/* Quick Actions */}
      <motion.div initial={{
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
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Clear Standby List
          </button>
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Flush Working Sets
          </button>
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Empty System Cache
          </button>
        </div>
      </motion.div>

      {/* Apply Button */}
      <motion.div initial={{
      opacity: 0
    }} animate={{
      opacity: 1
    }} transition={{
      delay: 0.5
    }} className="mt-8 flex justify-end">
        <button className="px-8 py-3 rounded-xl bg-gradient-to-r from-nebula-pink to-rose-400 text-white font-orbitron font-semibold tracking-wide hover:shadow-lg hover:shadow-nebula-pink/30 transition-shadow">
          Apply Changes
        </button>
      </motion.div>
    </motion.div>;
}