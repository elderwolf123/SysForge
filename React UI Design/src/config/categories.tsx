import React, { memo } from 'react';
import { LayoutDashboardIcon, CpuIcon, MonitorIcon, MemoryStickIcon, HardDriveIcon, WifiIcon } from 'lucide-react';
import { CategoryConfig } from '../types/category';
import { Dashboard } from '../pages/Dashboard';
import { CPUOptimization } from '../pages/CPUOptimization';
import { GPUOptimization } from '../pages/GPUOptimization';
import { MemoryOptimization } from '../pages/MemoryOptimization';
import { StorageOptimization } from '../pages/StorageOptimization';
import { NetworkOptimization } from '../pages/NetworkOptimization';
/**
 * MODULAR CATEGORY REGISTRY
 *
 * To add a new optimization category:
 *
 * 1. Create your page component in pages/ (e.g., pages/AudioOptimization.tsx)
 * 2. Import it at the top of this file
 * 3. Add a new entry to the categories array below
 *
 * Example:
 * {
 *   id: 'audio',
 *   label: 'Audio',
 *   icon: VolumeIcon,
 *   component: AudioOptimization,
 *   color: 'orange',
 *   description: 'Optimize audio latency and quality'
 * }
 *
 * That's it! The sidebar and routing will automatically update.
 */
export const categories: CategoryConfig[] = [{
  id: 'dashboard',
  label: 'Dashboard',
  icon: LayoutDashboardIcon,
  component: Dashboard,
  color: 'purple',
  description: 'System overview and metrics'
}, {
  id: 'cpu',
  label: 'CPU',
  icon: CpuIcon,
  component: CPUOptimization,
  color: 'purple',
  description: 'Processor performance tuning'
}, {
  id: 'gpu',
  label: 'GPU',
  icon: MonitorIcon,
  component: GPUOptimization,
  color: 'cyan',
  description: 'Graphics card optimization'
}, {
  id: 'memory',
  label: 'Memory',
  icon: MemoryStickIcon,
  component: MemoryOptimization,
  color: 'pink',
  description: 'RAM and virtual memory'
}, {
  id: 'storage',
  label: 'Storage',
  icon: HardDriveIcon,
  component: StorageOptimization,
  color: 'green',
  description: 'Disk performance and space'
}, {
  id: 'network',
  label: 'Network',
  icon: WifiIcon,
  component: NetworkOptimization,
  color: 'cyan',
  description: 'Network latency and throughput'
}];
// Helper functions for category management
export const getCategoryById = (id: string): CategoryConfig | undefined => {
  return categories.find(cat => cat.id === id);
};
export const getAllCategories = (): CategoryConfig[] => {
  return categories;
};
export const getNavigationCategories = (): CategoryConfig[] => {
  // Return all categories except dashboard for navigation menu
  return categories.filter(cat => cat.id !== 'dashboard');
};