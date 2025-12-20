import React, { useState } from 'react';
import { motion } from 'framer-motion';
import { OptimizationToggle } from '../components/OptimizationToggle';
import { OptimizationSlider } from '../components/OptimizationSlider';
import { MetricCard } from '../components/MetricCard';
import { WifiIcon, DownloadIcon, UploadIcon, ActivityIcon } from 'lucide-react';
export function NetworkOptimization() {
  const [settings, setSettings] = useState({
    nagleAlgorithm: false,
    networkThrottling: false,
    qosPacketScheduler: true,
    tcpAutoTuning: true,
    dnsOptimization: true,
    tcpWindowSize: 65535,
    dnsCache: 1000,
    connectionLimit: 16
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
            <WifiIcon className="w-6 h-6 text-white" />
          </div>
          <h1 className="text-3xl font-orbitron font-bold text-star-white">
            Network Optimization
          </h1>
        </div>
        <p className="text-star-dim">
          Reduce latency and maximize network throughput
        </p>
      </div>

      {/* Current Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <MetricCard title="Download" value={125} unit="Mbps" icon={<DownloadIcon className="w-5 h-5" />} color="cyan" index={0} />
        <MetricCard title="Upload" value={42} unit="Mbps" icon={<UploadIcon className="w-5 h-5" />} color="purple" index={1} />
        <MetricCard title="Latency" value={18} unit="ms" icon={<ActivityIcon className="w-5 h-5" />} color="green" index={2} />
        <MetricCard title="Packet Loss" value={0.1} unit="%" icon={<WifiIcon className="w-5 h-5" />} color="pink" index={3} />
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
            Network Features
          </h2>

          <OptimizationToggle label="Disable Nagle's Algorithm" description="Reduce latency by sending packets immediately" enabled={settings.nagleAlgorithm} onChange={v => updateSetting('nagleAlgorithm', v)} color="cyan" />
          <OptimizationToggle label="Disable Network Throttling" description="Remove Windows network throttling limits" enabled={settings.networkThrottling} onChange={v => updateSetting('networkThrottling', v)} color="purple" />
          <OptimizationToggle label="QoS Packet Scheduler" description="Prioritize gaming and real-time traffic" enabled={settings.qosPacketScheduler} onChange={v => updateSetting('qosPacketScheduler', v)} color="green" />
          <OptimizationToggle label="TCP Auto-Tuning" description="Automatically optimize TCP window size" enabled={settings.tcpAutoTuning} onChange={v => updateSetting('tcpAutoTuning', v)} color="cyan" />
          <OptimizationToggle label="DNS Optimization" description="Use faster DNS servers and caching" enabled={settings.dnsOptimization} onChange={v => updateSetting('dnsOptimization', v)} color="purple" />
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
            Advanced Settings
          </h2>

          <OptimizationSlider label="TCP Window Size" description="Buffer size for network data" value={settings.tcpWindowSize} min={16384} max={131072} step={8192} unit=" bytes" onChange={v => updateSetting('tcpWindowSize', v)} color="cyan" />
          <OptimizationSlider label="DNS Cache Entries" description="Number of DNS entries to cache" value={settings.dnsCache} min={100} max={5000} step={100} unit="" onChange={v => updateSetting('dnsCache', v)} color="purple" />
          <OptimizationSlider label="Max Connections" description="Maximum simultaneous connections" value={settings.connectionLimit} min={4} max={32} unit="" onChange={v => updateSetting('connectionLimit', v)} color="green" />
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
            Flush DNS Cache
          </button>
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Reset TCP/IP Stack
          </button>
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Run Speed Test
          </button>
          <button className="px-5 py-2.5 rounded-xl bg-white/5 border border-white/10 text-star-white font-medium hover:bg-white/10 transition-colors">
            Ping Test
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
        <button className="px-8 py-3 rounded-xl bg-gradient-to-r from-nebula-cyan to-blue-400 text-white font-orbitron font-semibold tracking-wide hover:shadow-lg hover:shadow-nebula-cyan/30 transition-shadow">
          Apply Changes
        </button>
      </motion.div>
    </motion.div>;
}