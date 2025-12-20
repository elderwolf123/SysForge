import { LucideIcon } from 'lucide-react';
import { ComponentType } from 'react';
export type CategoryColor = 'purple' | 'cyan' | 'pink' | 'green' | 'orange' | 'blue';
export interface CategoryConfig {
  id: string;
  label: string;
  icon: LucideIcon;
  component: ComponentType;
  color: CategoryColor;
  description?: string;
}
export interface CategoryRegistry {
  categories: CategoryConfig[];
  getCategoryById: (id: string) => CategoryConfig | undefined;
  getAllCategories: () => CategoryConfig[];
}