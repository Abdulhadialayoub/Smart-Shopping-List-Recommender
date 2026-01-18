import React, { useState, useEffect } from 'react';
import { Routes, Route, useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Typography,
  BottomNavigation,
  BottomNavigationAction,
  Box,
  IconButton,
  Menu,
  MenuItem,
  Avatar,
  Chip,
  Button
} from '@mui/material';
import {
  Kitchen,
  Restaurant,
  ShoppingCart,
  CompareArrows,
  AccountCircle,
  Logout,
  CheckCircle,
  Error as ErrorIcon,
  Refresh
} from '@mui/icons-material';

import { useAuth } from '../contexts/AuthContext';
import FridgeManager from './FridgeManager';
import RecipeSuggestions from './RecipeSuggestions';
import ShoppingLists from './ShoppingLists';
import PriceComparison from './PriceComparison';
import Profile from './Profile';

const Dashboard: React.FC = () => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [apiStatus, setApiStatus] = useState<'connected' | 'disconnected' | 'checking'>('checking');
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const checkApiConnection = async () => {
    try {
      setApiStatus('checking');
      // Simple API test - try to fetch from test endpoint
      const response = await fetch('http://localhost:7000/api/n8n/test');
      setApiStatus(response.ok ? 'connected' : 'disconnected');
    } catch (error) {
      console.error('API connection failed:', error);
      setApiStatus('disconnected');
    }
  };

  useEffect(() => {
    checkApiConnection();
    // Her 30 saniyede bir API durumunu kontrol et
    const interval = setInterval(checkApiConnection, 30000);
    return () => clearInterval(interval);
  }, []);

  const getCurrentTab = () => {
    const path = location.pathname;
    if (path.includes('/recipes')) return 1;
    if (path.includes('/shopping')) return 2;
    if (path.includes('/price-comparison')) return 3;
    return 0; // default to fridge
  };

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    switch (newValue) {
      case 0:
        navigate('/app');
        break;
      case 1:
        navigate('/app/recipes');
        break;
      case 2:
        navigate('/app/shopping');
        break;
      case 3:
        navigate('/app/price-comparison');
        break;
    }
  };

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/login');
    } catch (error) {
      console.error('Logout error:', error);
    }
    handleMenuClose();
  };

  const userId = user?.id || 'demo-user-123';

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="sticky" elevation={2}>
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ mr: 4, fontWeight: 700 }}>
            🛒 Smart Shopper
          </Typography>

          {/* Desktop Navigation */}
          <Box sx={{ flexGrow: 1, display: { xs: 'none', md: 'flex' }, gap: 1 }}>
            <Button
              color="inherit"
              startIcon={<Kitchen />}
              onClick={() => navigate('/app')}
              sx={{
                borderBottom: getCurrentTab() === 0 ? '3px solid white' : 'none',
                borderRadius: 0,
                px: 2
              }}
            >
              Buzdolabım
            </Button>
            <Button
              color="inherit"
              startIcon={<Restaurant />}
              onClick={() => navigate('/app/recipes')}
              sx={{
                borderBottom: getCurrentTab() === 1 ? '3px solid white' : 'none',
                borderRadius: 0,
                px: 2
              }}
            >
              Tarifler
            </Button>
            <Button
              color="inherit"
              startIcon={<ShoppingCart />}
              onClick={() => navigate('/app/shopping')}
              sx={{
                borderBottom: getCurrentTab() === 2 ? '3px solid white' : 'none',
                borderRadius: 0,
                px: 2
              }}
            >
              Alışveriş Listem
            </Button>
            <Button
              color="inherit"
              startIcon={<CompareArrows />}
              onClick={() => navigate('/app/price-comparison')}
              sx={{
                borderBottom: getCurrentTab() === 3 ? '3px solid white' : 'none',
                borderRadius: 0,
                px: 2
              }}
            >
              Fiyat Karşılaştır
            </Button>
          </Box>

          <Box sx={{ display: 'flex', alignItems: 'center' }}>
            {/* API Status Indicator */}
            <Chip
              icon={
                apiStatus === 'connected' ? <CheckCircle /> :
                  apiStatus === 'disconnected' ? <ErrorIcon /> :
                    <Refresh />
              }
              label={
                apiStatus === 'connected' ? 'Bağlı' :
                  apiStatus === 'disconnected' ? 'Bağlantı Yok' :
                    'Kontrol Ediliyor'
              }
              color={
                apiStatus === 'connected' ? 'success' :
                  apiStatus === 'disconnected' ? 'error' :
                    'default'
              }
              size="small"
              onClick={checkApiConnection}
              sx={{
                mr: 2,
                cursor: 'pointer',
                '& .MuiChip-icon': {
                  animation: apiStatus === 'checking' ? 'spin 1s linear infinite' : 'none'
                }
              }}
            />

            <Typography variant="body2" sx={{ mr: 2, display: { xs: 'none', sm: 'block' } }}>
              Hoş geldin, {user?.email?.split('@')[0] || 'Kullanıcı'}
            </Typography>

            <IconButton
              size="large"
              edge="end"
              aria-label="account of current user"
              aria-controls="menu-appbar"
              aria-haspopup="true"
              onClick={handleMenuOpen}
              color="inherit"
            >
              <Avatar sx={{ width: 32, height: 32, bgcolor: 'secondary.main' }}>
                {user?.name?.charAt(0)?.toUpperCase() || <AccountCircle />}
              </Avatar>
            </IconButton>

            <Menu
              id="menu-appbar"
              anchorEl={anchorEl}
              anchorOrigin={{
                vertical: 'top',
                horizontal: 'right',
              }}
              keepMounted
              transformOrigin={{
                vertical: 'top',
                horizontal: 'right',
              }}
              open={Boolean(anchorEl)}
              onClose={handleMenuClose}
            >
              <MenuItem onClick={() => { navigate('/app/profile'); handleMenuClose(); }}>
                <AccountCircle sx={{ mr: 1 }} />
                Profil Ayarları
              </MenuItem>
              <MenuItem onClick={handleLogout}>
                <Logout sx={{ mr: 1 }} />
                Çıkış Yap
              </MenuItem>
            </Menu>
          </Box>
        </Toolbar>
      </AppBar>

      <Box component="main" sx={{ flexGrow: 1, py: 3, pb: { xs: 8, md: 3 } }}>
        <Routes>
          <Route path="/" element={<FridgeManager userId={userId} />} />
          <Route path="/recipes" element={<RecipeSuggestions userId={userId} />} />
          <Route path="/shopping" element={<ShoppingLists userId={userId} />} />
          <Route path="/price-comparison" element={<PriceComparison />} />
          <Route path="/profile" element={<Profile />} />
        </Routes>
      </Box>

      {/* Mobile Bottom Navigation - Only show on small screens */}
      <BottomNavigation
        value={getCurrentTab()}
        onChange={handleTabChange}
        sx={{
          position: 'fixed',
          bottom: 0,
          left: 0,
          right: 0,
          display: { xs: 'flex', md: 'none' }, // Hide on desktop
          borderTop: '1px solid',
          borderColor: 'divider'
        }}
      >
        <BottomNavigationAction
          label="Buzdolabım"
          icon={<Kitchen />}
        />
        <BottomNavigationAction
          label="Tarifler"
          icon={<Restaurant />}
        />
        <BottomNavigationAction
          label="Alışveriş"
          icon={<ShoppingCart />}
        />
        <BottomNavigationAction
          label="Fiyatlar"
          icon={<CompareArrows />}
        />
      </BottomNavigation>
    </Box>
  );
};

export default Dashboard;