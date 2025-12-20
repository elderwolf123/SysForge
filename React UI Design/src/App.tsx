import React, { useState, Component } from 'react';
import { AnimatePresence } from 'framer-motion';
import { StarField } from './components/StarField';
import { Sidebar } from './components/Sidebar';
import { categories, getCategoryById } from './config/categories';
export function App() {
  const [activeCategory, setActiveCategory] = useState('dashboard');
  // Dynamic page rendering based on category registry
  const renderPage = () => {
    const category = getCategoryById(activeCategory);
    if (!category) {
      // Fallback to dashboard if category not found
      const dashboard = getCategoryById('dashboard');
      const DashboardComponent = dashboard?.component;
      return DashboardComponent ? <DashboardComponent key="dashboard" /> : null;
    }
    const PageComponent = category.component;
    return <PageComponent key={category.id} />;
  };
  return <div className="h-screen w-full flex overflow-hidden bg-space-dark">
      {/* Animated star field background */}
      <StarField />

      {/* Sidebar navigation */}
      <Sidebar activeCategory={activeCategory} onCategoryChange={setActiveCategory} />

      {/* Main content area with dynamic routing */}
      <main className="flex-1 overflow-y-auto relative z-10">
        <AnimatePresence mode="wait">{renderPage()}</AnimatePresence>
      </main>
    </div>;
}