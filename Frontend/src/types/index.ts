// User Types
export interface User {
  id: string;
  email: string;
  name: string;
  createdAt: Date;
  preferences?: UserPreferences;
  telegramChatId?: string;
  telegramUsername?: string;
}

export interface UserPreferences {
  dietaryRestrictions: string[];
  allergies: string[];
  favoriteStores: string[];
  budgetLimit?: number;
}

// Fridge Types
export interface FridgeItem {
  id: string;
  userId: string;
  name: string;
  category: string;
  quantity: number;
  unit: string;
  expiryDate: Date;
  addedDate: Date;
  isExpired: boolean;
  daysUntilExpiry: number;
  imageUrl?: string;
  nutritionInfo?: NutritionInfo;
}

export interface NutritionInfo {
  calories: number;
  protein: number;
  carbohydrates: number;
  fat: number;
  fiber?: number;
  sugar?: number;
  salt?: number;
}

// Recipe Types
export interface Recipe {
  id: string;
  name: string;
  description: string;
  ingredients: string[];
  instructions: string[];
  prepTime: number;
  cookTime: number;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  servings: number;
  difficulty: 'Easy' | 'Medium' | 'Hard';
  category: string;
  nutritionInfo?: NutritionInfo;
  nutrition?: NutritionInfo;
  imageUrl?: string;
  availableIngredients: string[];
  missingIngredients: string[];
  aiComment?: string;
  matchPercentage: number;
}

// Recipe Suggestions Response
export interface RecipeSuggestionsResponse {
  recipes: Recipe[];
  expiredItems: ExpiredItemInfo[];
  expiringItems: ExpiringItemInfo[];
  hasExpiredItems: boolean;
  hasExpiringItems: boolean;
  message: string;
}

export interface ExpiredItemInfo {
  name: string;
  expiryDate: string;
  daysExpired: number;
}

export interface ExpiringItemInfo {
  name: string;
  expiryDate: string;
  daysUntilExpiry: number;
}

// Shopping Types
export interface ShoppingList {
  id: string;
  userId: string;
  name: string;
  items: ShoppingItem[];
  createdAt: Date;
  updatedAt: Date;
  isCompleted: boolean;
  totalEstimatedCost?: number;
}

export interface ShoppingItem {
  id?: string;
  name: string;
  quantity: number;
  unit: string;
  isChecked: boolean;
  estimatedPrice?: number;
  actualPrice?: number;
  store?: string;
  category?: string;
  priceComparisons?: PriceComparison[];
}

export interface PriceComparison {
  store: string;
  price: number;
  currency: string;
  isAvailable: boolean;
  lastUpdated: Date;
  unitPrice?: number;
  imageUrl?: string;
  productUrl?: string;
  merchantId?: string;
  quantity?: string;
  unit?: string;
}

// Cimri-specific types
export interface CimriProduct {
  id: string;
  name: string;
  brand: string;
  price: number;
  unitPrice?: number;
  quantity: string;
  unit: string;
  imageUrl: string;
  productUrl: string;
  merchantId: string;
  merchantName: string;
}

export interface CimriSearchResult {
  products: CimriProduct[];
  currentPage: number;
  totalPages: number;
  query: string;
  timestamp: string;
}

export interface PriceHistory {
  date: string;
  price: number;
}

export interface MarketOffer {
  merchantId: string;
  merchantName: string;
  price: number;
  unitPrice?: number;
}

export interface CimriProductDetail {
  id: string;
  name: string;
  description: string;
  specs: any[];
  priceHistory: PriceHistory[];
  offers: MarketOffer[];
}

// AI & Analytics Types
export interface ShoppingAdvice {
  advice: string;
  estimatedSavings: number;
  recommendedStores: string[];
  budgetAnalysis?: BudgetAnalysis;
}

export interface BudgetAnalysis {
  totalBudget: number;
  estimatedCost: number;
  remainingBudget: number;
  isOverBudget: boolean;
  suggestions: string[];
}

// API Response Types
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

// Form Types
export interface LoginForm {
  email: string;
  password: string;
}

export interface RegisterForm {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface FridgeItemForm {
  name: string;
  category: string;
  quantity: number;
  unit: string;
  expiryDate: string;
}

export interface ShoppingListForm {
  name: string;
  items: Omit<ShoppingItem, 'id' | 'isChecked'>[];
}

// Component Props Types
export interface ComponentProps {
  className?: string;
  children?: React.ReactNode;
}

// Utility Types
export type LoadingState = 'idle' | 'loading' | 'success' | 'error';

export interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}