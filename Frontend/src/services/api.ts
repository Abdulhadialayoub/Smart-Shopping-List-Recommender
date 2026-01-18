import axios, { AxiosError } from 'axios';
import type {
  FridgeItem,
  Recipe,
  ShoppingList,
  PriceComparison,
  User,
  LoginForm,
  RegisterForm,
  ShoppingAdvice,
  CimriSearchResult,
  CimriProductDetail,
  RecipeSuggestionsResponse
} from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:7000/api';
const N8N_WEBHOOK_URL = import.meta.env.VITE_N8N_WEBHOOK_URL || '';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 0, // Timeout yok - cevap gelene kadar bekle
});

// Request interceptor
api.interceptors.request.use(
  (config) => {
    console.log(`API Request: ${config.method?.toUpperCase()} ${config.url}`);
    return config;
  },
  (error) => {
    console.error('API Request Error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor
api.interceptors.response.use(
  (response) => {
    console.log(`API Response: ${response.status} ${response.config.url}`);
    return response;
  },
  (error: AxiosError) => {
    console.error('API Response Error:', error.response?.status, error.message);

    // Handle specific error cases
    if (error.code === 'ECONNREFUSED') {
      console.error('Backend server is not running. Please start the API server.');
    }

    return Promise.reject(error);
  }
);

// Retry function for failed requests
const retryRequest = async <T>(requestFn: () => Promise<T>, retries = 2): Promise<T> => {
  try {
    return await requestFn();
  } catch (error) {
    if (retries > 0 && axios.isAxiosError(error)) {
      console.log(`Retrying request... (${retries} attempts left)`);
      await new Promise(resolve => setTimeout(resolve, 1000)); // Wait 1 second
      return retryRequest(requestFn, retries - 1);
    }
    throw error;
  }
};

// Fridge API
export const fridgeApi = {
  getFridgeItems: (userId: string) =>
    retryRequest(() => api.get<FridgeItem[]>(`/fridge/${userId}`)),

  addFridgeItem: (item: Omit<FridgeItem, 'id'>) =>
    retryRequest(() => api.post<FridgeItem>('/fridge', item)),

  updateFridgeItem: (itemId: string, item: FridgeItem) =>
    retryRequest(() => api.put<FridgeItem>(`/fridge/${itemId}`, item)),

  deleteFridgeItem: (itemId: string) =>
    retryRequest(() => api.delete(`/fridge/${itemId}`)),

  getExpiringItems: (userId: string, days: number = 3) =>
    retryRequest(() => api.get<FridgeItem[]>(`/fridge/${userId}/expiring?days=${days}`)),

  getNutrition: (productName: string) =>
    retryRequest(() => api.get(`/fridge/nutrition/${encodeURIComponent(productName)}`)),
};

// Image API
export const imageApi = {
  uploadImage: async (file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await retryRequest(() =>
      api.post<{ imageUrl: string }>('/image/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      })
    );

    return response.data.imageUrl;
  },
};

// Turkish to English translation helper for nutrition API
const turkishToEnglish: { [key: string]: string } = {
  'domates': 'tomato',
  'salatalık': 'cucumber',
  'biber': 'pepper',
  'patlıcan': 'eggplant',
  'kabak': 'zucchini',
  'havuç': 'carrot',
  'soğan': 'onion',
  'sarımsak': 'garlic',
  'patates': 'potato',
  'elma': 'apple',
  'muz': 'banana',
  'portakal': 'orange',
  'üzüm': 'grape',
  'çilek': 'strawberry',
  'karpuz': 'watermelon',
  'kavun': 'melon',
  'şeftali': 'peach',
  'kiraz': 'cherry',
  'tavuk': 'chicken',
  'et': 'beef',
  'balık': 'fish',
  'süt': 'milk',
  'yoğurt': 'yogurt',
  'peynir': 'cheese',
  'yumurta': 'egg',
  'ekmek': 'bread',
  'pirinç': 'rice',
  'makarna': 'pasta',
  'un': 'flour',
  'şeker': 'sugar',
  'tuz': 'salt',
  'yağ': 'oil',
  'tereyağı': 'butter',
  'zeytin': 'olive',
  'zeytinyağı': 'olive oil',
};

export const translateToEnglish = (turkishWord: string): string => {
  const normalized = turkishWord.toLowerCase().trim();
  return turkishToEnglish[normalized] || turkishWord;
};

// Recipe API
export const recipeApi = {
  getRecipeSuggestions: (userId: string, servings: number = 2) =>
    retryRequest(() => api.get<RecipeSuggestionsResponse>(`/recipe/suggestions/${userId}?servings=${servings}`)),

  createRecipeShoppingList: (data: { userId: string; recipeName: string; missingIngredients: string[]; includeBasicIngredients?: boolean }) =>
    retryRequest(() => api.post('/recipe/shopping-list', data, { timeout: 0 })), // Timeout yok - cevap gelene kadar bekle

  generateRecipe: (ingredients: string[], dietaryRestrictions?: string) =>
    retryRequest(() => api.post<Recipe>('/recipe/generate', { ingredients, dietaryRestrictions })),

  getNutritionInfo: (ingredients: string[]) =>
    retryRequest(() => api.get('/recipe/nutrition', { params: { ingredients } })),
};

// Auth API
export const authApi = {
  login: (credentials: LoginForm) =>
    retryRequest(() => api.post<{ success: boolean; user: User; token: string; message: string }>('/auth/login', credentials)),

  register: (userData: RegisterForm) =>
    retryRequest(() => api.post<{ success: boolean; user: User; token: string; message: string }>('/auth/register', userData)),

  logout: () =>
    retryRequest(() => api.post('/auth/logout')),

  forgotPassword: (email: string) =>
    retryRequest(() => api.post('/auth/forgot-password', { email })),

  resetPassword: (token: string, newPassword: string) =>
    retryRequest(() => api.post('/auth/reset-password', { token, newPassword })),
};

// User API
export const userApi = {
  getProfile: (userId: string) =>
    retryRequest(() => api.get<User>(`/user/${userId}/profile`)),

  updateProfile: (userId: string, userData: Partial<User>) =>
    retryRequest(() => api.put<User>(`/user/${userId}/profile`, userData)),

  deleteAccount: (userId: string) =>
    retryRequest(() => api.delete(`/user/${userId}`)),
};

// Shopping API
export const shoppingApi = {
  getShoppingLists: (userId: string) =>
    retryRequest(() => api.get<ShoppingList[]>(`/shopping/${userId}`)),

  createShoppingList: (shoppingList: Omit<ShoppingList, 'id'>) =>
    retryRequest(() => api.post<ShoppingList>('/shopping', shoppingList)),

  updateShoppingList: (listId: string, shoppingList: ShoppingList) =>
    retryRequest(() => api.put<ShoppingList>(`/shopping/${listId}`, shoppingList)),

  deleteShoppingList: (listId: string) =>
    retryRequest(() => api.delete(`/shopping/${listId}`)),

  comparePrices: (productName: string) =>
    retryRequest(() => api.get<{ comparisons: PriceComparison[]; aiAdvice: string; generatedAt: string }>(`/shopping/compare-prices?productName=${productName}`)),

  getShoppingAdvice: (userId: string, products: string[]) =>
    retryRequest(() => api.post<ShoppingAdvice>('/shopping/advice', { userId, products })),

  createSmartShoppingList: (userId: string) =>
    retryRequest(() => api.post<ShoppingList>('/shopping/smart-list', { userId })),
};

// Webhook API (for n8n integration)
export const webhookApi = {
  getFridgeSummary: (userId: string) =>
    retryRequest(() => api.get(`/webhook/fridge-summary/${userId}`)),

  createSmartShoppingList: (userId: string) =>
    retryRequest(() => api.post('/webhook/smart-shopping-list', { userId })),

  batchOperation: (operations: any[]) =>
    retryRequest(() => api.post('/webhook/batch-operation', { operations })),
};

// Cimri API (for price comparison from Cimri.com)
export const cimriApi = {
  searchProducts: (query: string, page: number = 1, sort: string = '') =>
    retryRequest(() => api.get<CimriSearchResult>(`/cimri/search`, {
      params: { query, page, sort }
    })),

  getProductDetails: (productId: string) =>
    retryRequest(() => api.get<CimriProductDetail>(`/cimri/product/${productId}`)),
};

// n8n Webhook API (for recipe price checking via n8n Cloud)
export const n8nApi = {
  // Tarif seçildiğinde eksik malzemelerin fiyatlarını kontrol et
  checkRecipePrices: async (userId: string, recipe: { name: string; ingredients: string[] }) => {
    if (!N8N_WEBHOOK_URL) {
      console.warn('n8n webhook URL not configured');
      return null;
    }

    try {
      const response = await axios.post(`${N8N_WEBHOOK_URL}/recipe-selected`, {
        userId,
        recipe
      }, {
        timeout: 60000, // 60 saniye timeout (web scraping uzun sürebilir)
        headers: {
          'Content-Type': 'application/json'
        }
      });
      return response.data;
    } catch (error) {
      console.error('n8n webhook error:', error);
      return null;
    }
  },

  // n8n bağlantı testi
  testConnection: async () => {
    if (!N8N_WEBHOOK_URL) {
      return { connected: false, message: 'n8n webhook URL not configured' };
    }

    try {
      const response = await axios.get(`${N8N_WEBHOOK_URL.replace('/webhook/', '/webhook-test/')}/recipe-selected`, {
        timeout: 5000
      });
      return { connected: true, message: 'n8n connected', data: response.data };
    } catch {
      return { connected: false, message: 'n8n connection failed' };
    }
  }
};