import React from 'react';
import { motion } from 'framer-motion';
import { MetricCard } from '../components/MetricCard';
import { CpuIcon, MonitorIcon, MemoryStickIcon, HardDriveIcon, WifiIcon, ThermometerIcon, ZapIcon, ActivityIcon } from 'lucide-react';
export function Dashboard() {
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
        <motion.h1 initial={{
        opacity: 0,
        y: -10
      }} animate={{
        opacity: 1,
        y: 0
      }} className="text-3xl font-orbitron font-bold text-star-white mb-2">
          System Overview
        </motion.h1>
        <p className="text-star-dim">
          Real-time performance metrics and system health
        </p>
      </div>

      {/* Quick Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <MetricCard title="CPU Usage" value={42} unit="%" icon={<CpuIcon className="w-5 h-5" />} percentage={42} trend="stable" color="purple" index={0} />
        <MetricCard title="GPU Usage" value={67} unit="%" icon={<MonitorIcon className="w-5 h-5" />} percentage={67} trend="up" color="cyan" index={1} />
        <MetricCard title="Memory" value={8.2} unit="GB" icon={<MemoryStickIcon className="w-5 h-5" />} percentage={51} color="pink" index={2} />
        <MetricCard title="Storage" value={234} unit="GB free" icon={<HardDriveIcon className="w-5 h-5" />} percentage={23} color="green" index={3} />
      </div>

      {/* Secondary Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <MetricCard title="Network" value={125} unit="Mbps" icon={<WifiIcon className="w-5 h-5" />} trend="stable" color="cyan" index={4} />
        <MetricCard title="CPU Temp" value={58} unit="°C" icon={<ThermometerIcon className="w-5 h-5" />} trend="stable" color="purple" index={5} />
        <MetricCard title="Power Draw" value={145} unit="W" icon={<ZapIcon className="w-5 h-5" />} trend="down" color="green" index={6} />
      </div>

      {/* Active Optimizations */}
      <motion.div initial={{
      opacity: 0,
      y: 20
    }} animate={{
      opacity: 1,
      y: 0
    }} transition={{
      delay: 0.4
    }} className="rounded-2xl bg-space-dark/60 backdrop-blur-xl border border-white/5 p-6">
        <div className="flex items-center gap-3 mb-6">
          <div className="p-2 rounded-lg bg-gradient-to-br from-nebula-purple to-nebula-violet">
            <ActivityIcon className="w-5 h-5 text-white" />
          </div>
          <h2 className="text-xl font-orbitron font-semibold text-star-white">
            Active Optimizations
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {[{
          name: 'CPU Priority Boost',
          status: 'Active',
          impact: '+12% performance'
        }, {
          name: 'Memory Compression',
          status: 'Active',
          impact: '-2.1 GB usage'
        }, {
          name: 'Network Latency Reducer',
          status: 'Active',
          impact: '-8ms ping'
        }, {
          name: 'Background Process Limiter',
          status: 'Active',
          impact: '+15% CPU freed'
        }].map((opt, index) => <motion.div key={opt.name} initial={{
          opacity: 0,
          x: -10
        }} animate={{
          opacity: 1,
          x: 0
        }} transition={{
          delay: 0.5 + index * 0.1
        }} className="flex items-center justify-between p-4 rounded-xl bg-white/5 border border-white/5">
              <div className="flex items-center gap-3">
                <div className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
                <div>
                  <p className="text-star-white font-medium">{opt.name}</p>
                  <p className="text-xs text-star-dim">{opt.status}</p>
                </div>
              </div>
              <span className="text-sm text-emerald-400 font-medium">
                {opt.impact}
              </span>
            </motion.div>)}
        </div>
      </motion.div>
    </motion.div>;
}