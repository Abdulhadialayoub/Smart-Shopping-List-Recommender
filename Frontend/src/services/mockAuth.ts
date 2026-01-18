// Demo authentication service
export interface MockUser {
  uid: string;
  email: string;
  displayName?: string;
  photoURL?: string;
}

class MockAuthService {
  private currentUser: MockUser | null = null;
  private listeners: ((user: MockUser | null) => void)[] = [];

  // Demo kullanıcılar
  private demoUsers = [
    {
      uid: 'demo-user-123',
      email: 'demo@smartshopper.com',
      displayName: 'Demo Kullanıcı'
    },
    {
      uid: 'test-user-456',
      email: 'test@smartshopper.com',
      displayName: 'Test Kullanıcı'
    }
  ];

  constructor() {
    // LocalStorage'dan kullanıcıyı yükle
    const savedUser = localStorage.getItem('mockUser');
    if (savedUser) {
      this.currentUser = JSON.parse(savedUser);
    }
  }

  async signInWithEmailAndPassword(email: string, password: string): Promise<MockUser> {
    // Demo authentication logic
    if (email === 'demo@smartshopper.com' && password === 'demo123456') {
      const user = this.demoUsers[0];
      this.currentUser = user;
      localStorage.setItem('mockUser', JSON.stringify(user));
      this.notifyListeners();
      return user;
    }
    
    if (email === 'test@smartshopper.com' && password === 'test123456') {
      const user = this.demoUsers[1];
      this.currentUser = user;
      localStorage.setItem('mockUser', JSON.stringify(user));
      this.notifyListeners();
      return user;
    }

    throw new Error('auth/wrong-password');
  }

  async createUserWithEmailAndPassword(email: string, _password: string): Promise<MockUser> {
    // Demo registration logic
    const user: MockUser = {
      uid: `user-${Date.now()}`,
      email,
      displayName: email.split('@')[0]
    };
    
    this.currentUser = user;
    localStorage.setItem('mockUser', JSON.stringify(user));
    this.notifyListeners();
    return user;
  }

  async signOut(): Promise<void> {
    this.currentUser = null;
    localStorage.removeItem('mockUser');
    this.notifyListeners();
  }

  async sendPasswordResetEmail(email: string): Promise<void> {
    // Mock password reset
    console.log(`Password reset email sent to: ${email}`);
    // Gerçek uygulamada email gönderilir
  }

  onAuthStateChanged(callback: (user: MockUser | null) => void): () => void {
    this.listeners.push(callback);
    // İlk çağrıda mevcut kullanıcıyı gönder
    callback(this.currentUser);
    
    // Unsubscribe function döndür
    return () => {
      const index = this.listeners.indexOf(callback);
      if (index > -1) {
        this.listeners.splice(index, 1);
      }
    };
  }

  getCurrentUser(): MockUser | null {
    return this.currentUser;
  }

  private notifyListeners(): void {
    this.listeners.forEach(listener => listener(this.currentUser));
  }
}

export const mockAuth = new MockAuthService();