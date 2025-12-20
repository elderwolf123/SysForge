import React, { useState, memo } from 'react';
import { motion } from 'framer-motion';
import { OptimizationToggle } from '../components/OptimizationToggle';
import { OptimizationSlider } from '../components/OptimizationSlider';
import { MetricCard } from '../components/MetricCard';
import { MonitorIcon, ZapIcon, ThermometerIcon, FanIcon } from 'lucide-react';
export function GPUOptimization() {
  const [settings, setSettings] = useState({
    hardwareAcceleration: true,
    gameMode: true,
    variableRefresh: true,
    shaderCache: true,
    lowLatencyMode: false,
    powerTarget: 100,
    fanSpeed: 65,
    memoryClockOffset: 50
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
          <div className="p-2 rounded-lg bg-gradient-to-br from-nebula-cyan to-blue-400">
            <MonitorIcon className="w-6 h-6 text-white" />
          </div>
          <h1 className="text-3xl font-orbitron font-bold text-star-white">
            GPU Optimization
          </h1>
        </div>
        <p className="text-star-dim">
          Maximize graphics performance and visual quality
        </p>
      </div>

      {/* Current Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <MetricCard title="GPU Usage" value={67} unit="%" icon={<MonitorIcon className="w-5 h-5" />} percentage={67} color="cyan" index={0} />
        <MetricCard title="Core Clock" value={1875} unit="MHz" icon={<ZapIcon className="w-5 h-5" />} color="purple" index={1} />
        <MetricCard title="Temperature" value={72} unit="°C" icon={<ThermometerIcon className="w-5 h-5" />} color="pink" index={2} isWarning={true} />
        <MetricCard title="Fan Speed" value={65} unit="%" icon={<FanIcon className="w-5 h-5" />} color="green" index={3} />
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
            Graphics Features
          </h2>

          <OptimizationToggle label="Hardware Acceleration" description="Use GPU for video decoding and rendering" enabled={settings.hardwareAcceleration} onChange={v => updateSetting('hardwareAcceleration', v)} color="cyan" />
          <OptimizationToggle label="Game Mode" description="Prioritize GPU resources for gaming applications" enabled={settings.gameMode} onChange={v => updateSetting('gameMode', v)} color="purple" />
          <OptimizationToggle label="Variable Refresh Rate" description="Enable G-Sync/FreeSync for smoother gameplay" enabled={settings.variableRefresh} onChange={v => updateSetting('variableRefresh', v)} color="green" />
          <OptimizationToggle label="Shader Cache" description="Pre-compile shaders for faster game loading" enabled={settings.shaderCache} onChange={v => updateSetting('shaderCache', v)} color="cyan" />
          <OptimizationToggle label="Ultra Low Latency Mode" description="Minimize input lag for competitive gaming" enabled={settings.lowLatencyMode} onChange={v => updateSetting('lowLatencyMode', v)} color="purple" />
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
            Performance Tuning
          </h2>

          <OptimizationSlider label="Power Target" description="Maximum power limit for the GPU" value={settings.powerTarget} onChange={v => updateSetting('powerTarget', v)} color="cyan" />
          <OptimizationSlider label="Fan Speed Override" description="Manual fan speed control" value={settings.fanSpeed} onChange={v => updateSetting('fanSpeed', v)} color="green" />
          <OptimizationSlider label="Memory Clock Offset" description="Increase VRAM frequency for better bandwidth" value={settings.memoryClockOffset} min={0} max={500} unit="MHz" onChange={v => updateSetting('memoryClockOffset', v)} color="purple" />
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
        <button className="px-8 py-3 rounded-xl bg-gradient-to-r from-nebula-cyan to-blue-400 text-white font-orbitron font-semibold tracking-wide hover:shadow-lg hover:shadow-nebula-cyan/30 transition-shadow">
          Apply Changes
        </button>
      </motion.div>
    </motion.div>;
}