import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Container,
  Paper,
  TextField,
  Button,
  Typography,
  Box,
  Alert,
  CircularProgress,

  useTheme,
  useMediaQuery,
  Avatar
} from '@mui/material';
import { Email, Lock, ShoppingCart } from '@mui/icons-material';
import { useAuth } from '../../contexts/AuthContext';

const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email || !password) {
      setError('Lütfen tüm alanları doldurun');
      return;
    }

    try {
      setError('');
      setLoading(true);
      await login(email, password);
      navigate('/app');
    } catch (error: any) {
      console.error('Login error:', error);
      setError(getErrorMessage(error.message));
    } finally {
      setLoading(false);
    }
  };

  const getErrorMessage = (errorMessage: string) => {
    if (errorMessage.includes('Kullanıcı bulunamadı')) {
      return 'Bu email adresi ile kayıtlı kullanıcı bulunamadı. Lütfen önce kayıt olun.';
    }
    return errorMessage || 'Giriş yapılırken bir hata oluştu';
  };



  return (
    <Box
      sx={{
        minHeight: '100vh',
        background: 'linear-gradient(135deg, #2e7d32 0%, #4caf50 100%)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        py: 3,
        px: { xs: 2, sm: 3 }
      }}
    >
      <Container component="main" maxWidth="sm">
        <Paper 
          elevation={isMobile ? 0 : 8} 
          sx={{ 
            padding: { xs: 3, sm: 4, md: 5 }, 
            width: '100%',
            borderRadius: { xs: 0, sm: 3 },
            background: 'rgba(255, 255, 255, 0.95)',
            backdropFilter: 'blur(10px)',
            ...(isMobile && {
              minHeight: '100vh',
              display: 'flex',
              flexDirection: 'column',
              justifyContent: 'center'
            })
          }}
        >
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <Avatar
              sx={{
                m: 1,
                bgcolor: 'primary.main',
                width: { xs: 56, sm: 64 },
                height: { xs: 56, sm: 64 }
              }}
            >
              <ShoppingCart sx={{ fontSize: { xs: 28, sm: 32 } }} />
            </Avatar>
            
            <Typography 
              component="h1" 
              variant={isMobile ? "h5" : "h4"} 
              gutterBottom
              sx={{ 
                fontWeight: 700,
                color: 'primary.main',
                textAlign: 'center'
              }}
            >
              Giriş Yap
            </Typography>
            
            <Typography 
              variant="body1" 
              color="textSecondary" 
              align="center" 
              sx={{ mb: 4, maxWidth: 400 }}
            >
              Akıllı Alışveriş Asistanınıza hoş geldiniz
            </Typography>

            {error && (
              <Alert severity="error" sx={{ width: '100%', mb: 2 }}>
                {error}
              </Alert>
            )}

            <Box component="form" onSubmit={handleSubmit} sx={{ mt: 1, width: '100%' }}>
              <TextField
                margin="normal"
                required
                fullWidth
                id="email"
                label="Email Adresi"
                name="email"
                autoComplete="email"
                autoFocus
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                InputProps={{
                  startAdornment: <Email sx={{ mr: 1, color: 'action.active' }} />
                }}
                sx={{
                  '& .MuiOutlinedInput-root': {
                    borderRadius: 2,
                    '&:hover fieldset': {
                      borderColor: 'primary.main',
                    },
                  },
                }}
              />
              
              <TextField
                margin="normal"
                required
                fullWidth
                name="password"
                label="Şifre"
                type="password"
                id="password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                InputProps={{
                  startAdornment: <Lock sx={{ mr: 1, color: 'action.active' }} />
                }}
                sx={{
                  '& .MuiOutlinedInput-root': {
                    borderRadius: 2,
                    '&:hover fieldset': {
                      borderColor: 'primary.main',
                    },
                  },
                }}
              />

              <Button
                type="submit"
                fullWidth
                variant="contained"
                size="large"
                sx={{ 
                  mt: 3, 
                  mb: 2, 
                  py: { xs: 1.5, sm: 2 },
                  borderRadius: 2,
                  fontWeight: 600,
                  fontSize: { xs: '1rem', sm: '1.1rem' },
                  background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
                  boxShadow: '0 3px 5px 2px rgba(76, 175, 80, .3)',
                  '&:hover': {
                    background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
                    transform: 'translateY(-1px)',
                    boxShadow: '0 6px 10px 2px rgba(76, 175, 80, .3)',
                  }
                }}
                disabled={loading}
              >
                {loading ? <CircularProgress size={24} color="inherit" /> : 'Giriş Yap'}
              </Button>



              <Box sx={{ 
                display: 'flex', 
                justifyContent: 'space-between',
                flexDirection: { xs: 'column', sm: 'row' },
                gap: { xs: 2, sm: 0 },
                mt: 2 
              }}>
                <Link to="/forgot-password" style={{ textDecoration: 'none' }}>
                  <Typography 
                    variant="body2" 
                    color="primary"
                    sx={{ 
                      fontWeight: 500,
                      '&:hover': { textDecoration: 'underline' }
                    }}
                  >
                    Şifremi Unuttum
                  </Typography>
                </Link>
                
                <Link to="/register" style={{ textDecoration: 'none' }}>
                  <Typography 
                    variant="body2" 
                    color="primary"
                    sx={{ 
                      fontWeight: 500,
                      '&:hover': { textDecoration: 'underline' }
                    }}
                  >
                    Hesap Oluştur
                  </Typography>
                </Link>
              </Box>
            </Box>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
};

export default Login;