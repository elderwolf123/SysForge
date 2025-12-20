import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { OptimizationToggle } from '../components/OptimizationToggle';
import { OptimizationSlider } from '../components/OptimizationSlider';
import { MetricCard } from '../components/MetricCard';
import { HardDriveIcon, DatabaseIcon, TrashIcon, FolderIcon } from 'lucide-react';
export function StorageOptimization() {
  const [settings, setSettings] = useState({
    trimSSD: true,
    indexing: false,
    defragSchedule: false,
    storageOptimization: true,
    compactOS: false,
    tempCleanupDays: 7,
    recycleLimit: 10,
    hibernateSize: 75
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
          <div className="p-2 rounded-lg bg-gradient-to-br from-emerald-400 to-green-500">
            <HardDriveIcon className="w-6 h-6 text-white" />
          </div>
          <h1 className="text-3xl font-orbitron font-bold text-star-white">
            Storage Optimization
          </h1>
        </div>
        <p className="text-star-dim">
          Optimize disk performance and manage storage space
        </p>
      </div>

      {/* Current Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <MetricCard title="Used Space" value={766} unit="GB" icon={<HardDriveIcon className="w-5 h-5" />} percentage={77} color="green" index={0} />
        <MetricCard title="Free Space" value={234} unit="GB" icon={<DatabaseIcon className="w-5 h-5" />} color="cyan" index={1} />
        <MetricCard title="Temp Files" value={4.2} unit="GB" icon={<TrashIcon className="w-5 h-5" />} color="pink" index={2} />
        <MetricCard title="System Files" value={28} unit="GB" icon={<FolderIcon className="w-5 h-5" />} color="purple" index={3} />
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
            Disk Features
          </h2>

          <OptimizationToggle label="SSD TRIM Optimization" description="Enable TRIM for better SSD performance and longevity" enabled={settings.trimSSD} onChange={v => updateSetting('trimSSD', v)} color="green" />
          <OptimizationToggle label="Disable Search Indexing" description="Stop Windows from indexing files for faster disk access" enabled={settings.indexing} onChange={v => updateSetting('indexing', v)} color="cyan" />
          <OptimizationToggle label="Scheduled Defragmentation" description="Automatically defragment HDDs on schedule" enabled={settings.defragSchedule} onChange={v => updateSetting('defragSchedule', v)} color="purple" />
          <OptimizationToggle label="Storage Sense" description="Automatically free up space by removing temp files" enabled={settings.storageOptimization} onChange={v => updateSetting('storageOptimization', v)} color="green" />
          <OptimizationToggle label="Compact OS" description="Compress Windows system files to save space" enabled={settings.compactOS} onChange={v => updateSetting('compactOS', v)} color="cyan" />
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
            Cleanup Settings
          </h2>

          <OptimizationSlider label="Temp File Cleanup Age" description="Delete temporary files older than this" value={settings.tempCleanupDays} min={1} max={30} unit=" days" onChange={v => updateSetting('tempCleanupDays', v)} color="green" />
          <OptimizationSlider label="Recycle Bin Limit" description="Maximum size of recycle bin" value={settings.recycleLimit} min={1} max={50} unit="GB" onChange={v => updateSetting('recycleLimit', v)} color="cyan" />
          <OptimizationSlider label="Hibernation File Size" description="Size of hibernation file relative to RAM" value={settings.hibernateSize} onChange={v => updateSetting('hibernateSize', v)} color="purple" />
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
            Run Disk Cleanup
          </button>
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Analyze Disk Usage
          </button>
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Empty Recycle Bin
          </button>
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Clear Temp Files
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
        <button className="px-8 py-3 rounded-xl bg-gradient-to-r from-emerald-400 to-green-500 text-white font-orbitron font-semibold tracking-wide hover:shadow-lg hover:shadow-emerald-400/30 transition-shadow">
          Apply Changes
        </button>
      </motion.div>
    </motion.div>;
}