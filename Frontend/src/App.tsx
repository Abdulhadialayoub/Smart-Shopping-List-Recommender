// React import removed as it's not needed in React 18+ with new JSX transform
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material';
import { AuthProvider } from './contexts/AuthContext';
import ProtectedRoute from './components/auth/ProtectedRoute';
import Login from './components/auth/Login';
import Register from './components/auth/Register';
import ForgotPassword from './components/auth/ForgotPassword';
import Dashboard from './components/Dashboard';
import Welcome from './components/Welcome';

const theme = createTheme({
  palette: {
    primary: {
      main: '#2e7d32',
      light: '#4caf50',
      dark: '#1b5e20',
      contrastText: '#ffffff',
    },
    secondary: {
      main: '#66bb6a',
      light: '#81c784',
      dark: '#388e3c',
      contrastText: '#ffffff',
    },
    success: {
      main: '#4caf50',
      light: '#81c784',
      dark: '#388e3c',
    },
    background: {
      default: '#f1f8e9',
      paper: '#ffffff',
    },
    text: {
      primary: '#212121',
      secondary: '#424242',
    },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
        containedPrimary: {
          '&:hover': {
            backgroundColor: '#1b5e20',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: '0 2px 8px rgba(46, 125, 50, 0.1)',
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: '#2e7d32',
        },
      },
    },
    MuiDialogTitle: {
      styleOverrides: {
        root: {
          color: '#212121',
          fontWeight: 700,
        },
      },
    },
    MuiDialogContent: {
      styleOverrides: {
        root: {
          color: '#212121',
        },
      },
    },
    MuiTypography: {
      styleOverrides: {
        root: {
          color: '#212121',
        },
        h1: { color: '#212121' },
        h2: { color: '#212121' },
        h3: { color: '#212121' },
        h4: { color: '#212121' },
        h5: { color: '#212121' },
        h6: { color: '#212121' },
        body1: { color: '#212121' },
        body2: { color: '#424242' },
      },
    },
    MuiListItemText: {
      styleOverrides: {
        primary: {
          color: '#212121',
        },
        secondary: {
          color: '#666666',
        },
      },
    },
  },
});

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AuthProvider>
        <Router>
          <Routes>
            <Route path="/" element={<Welcome />} />
            <Route path="/welcome" element={<Welcome />} />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/forgot-password" element={<ForgotPassword />} />
            <Route 
              path="/app/*" 
              element={
                <ProtectedRoute>
                  <Dashboard />
                </ProtectedRoute>
              } 
            />
          </Routes>
        </Router>
      </AuthProvider>
    </ThemeProvider>
  );
}

export default App;
