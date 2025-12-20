import React from 'react';
import { motion } from 'framer-motion';
import { SettingsIcon, ZapIcon } from 'lucide-react';
import { categories } from '../config/categories';
type SidebarProps = {
  activeCategory: string;
  onCategoryChange: (category: string) => void;
};
export function Sidebar({
  activeCategory,
  onCategoryChange
}: SidebarProps) {
  return <aside className="w-72 h-full flex flex-col bg-space-darker/80 backdrop-blur-xl border-r border-white/5">
      {/* Logo */}
      <div className="p-6 border-b border-white/5">
        <div className="flex items-center gap-3">
          <div className="relative">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-nebula-purple to-nebula-cyan flex items-center justify-center">
              <ZapIcon className="w-5 h-5 text-white" />
            </div>
            <div className="absolute inset-0 rounded-xl bg-gradient-to-br from-nebula-purple to-nebula-cyan blur-lg opacity-50" />
          </div>
          <div>
            <h1 className="font-orbitron font-bold text-lg text-star-white tracking-wide">
              NOVA
            </h1>
            <p className="text-xs text-star-dim uppercase tracking-widest">
              Optimizer
            </p>
          </div>
        </div>
      </div>

      {/* Navigation - Dynamically generated from categories config */}
      <nav className="flex-1 p-4 space-y-1">
        <p className="text-xs text-star-dim uppercase tracking-widest px-3 mb-3">
          Categories
        </p>

        {categories.map(category => {
        const isActive = activeCategory === category.id;
        const Icon = category.icon;
        return <motion.button key={category.id} onClick={() => onCategoryChange(category.id)} whileHover={{
          x: 4
        }} whileTap={{
          scale: 0.98
        }} className={`
                relative w-full flex items-center gap-3 px-4 py-3 rounded-xl
                transition-colors duration-200
                ${isActive ? 'text-star-white' : 'text-star-dim hover:text-star-white'}
              `}>
              {/* Active background */}
              {isActive && <motion.div layoutId="activeCategory" className="absolute inset-0 rounded-xl bg-gradient-to-r from-nebula-purple/20 to-nebula-cyan/10 border border-nebula-purple/30" transition={{
            type: 'spring',
            stiffness: 300,
            damping: 30
          }} />}

              {/* Icon with glow */}
              <div className="relative z-10">
                <Icon className={`w-5 h-5 ${isActive ? 'text-nebula-purple' : ''}`} />
                {isActive && <div className="absolute inset-0 blur-md bg-nebula-purple opacity-50" />}
              </div>

              <span className="relative z-10 font-medium">
                {category.label}
              </span>

              {/* Active indicator */}
              {isActive && <motion.div initial={{
            scale: 0
          }} animate={{
            scale: 1
          }} className="absolute right-3 w-1.5 h-1.5 rounded-full bg-nebula-cyan" />}
            </motion.button>;
      })}
      </nav>

      {/* Settings */}
      <div className="p-4 border-t border-white/5">
        <button className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-star-dim hover:text-star-white transition-colors">
          <SettingsIcon className="w-5 h-5" />
          <span className="font-medium">Settings</span>
        </button>
      </div>

      {/* System status */}
      <div className="p-4 mx-4 mb-4 rounded-xl bg-gradient-to-br from-nebula-purple/10 to-nebula-cyan/5 border border-white/5">
        <div className="flex items-center gap-2 mb-2">
          <div className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          <span className="text-sm text-star-white font-medium">
            System Healthy
          </span>
        </div>
        <p className="text-xs text-star-dim">
          All optimizations running smoothly
        </p>
      </div>
    </aside>;
}