import React, { useState } from 'react';
import { Link } from 'react-router-dom';
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
import { Email, ArrowBack, LockReset } from '@mui/icons-material';
import { useAuth } from '../../contexts/AuthContext';

const ForgotPassword: React.FC = () => {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { resetPassword } = useAuth();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email) {
      setError('Lütfen email adresinizi girin');
      return;
    }

    try {
      setMessage('');
      setError('');
      setLoading(true);
      await resetPassword(email);
      setMessage('Şifre sıfırlama bağlantısı email adresinize gönderildi');
    } catch (error: any) {
      console.error('Reset password error:', error);
      setError(getErrorMessage(error.code));
    } finally {
      setLoading(false);
    }
  };

  const getErrorMessage = (errorCode: string) => {
    switch (errorCode) {
      case 'auth/user-not-found':
        return 'Bu email adresi ile kayıtlı kullanıcı bulunamadı';
      case 'auth/invalid-email':
        return 'Geçersiz email adresi';
      case 'auth/too-many-requests':
        return 'Çok fazla istek gönderildi. Lütfen daha sonra tekrar deneyin';
      default:
        return 'Şifre sıfırlama işlemi sırasında bir hata oluştu';
    }
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
              <LockReset sx={{ fontSize: { xs: 28, sm: 32 } }} />
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
              Şifremi Unuttum
            </Typography>
            
            <Typography 
              variant="body1" 
              color="textSecondary" 
              align="center" 
              sx={{ mb: 4, maxWidth: 400 }}
            >
              Email adresinizi girin, size şifre sıfırlama bağlantısı gönderelim
            </Typography>

            {error && (
              <Alert severity="error" sx={{ width: '100%', mb: 2 }}>
                {error}
              </Alert>
            )}

            {message && (
              <Alert severity="success" sx={{ width: '100%', mb: 2 }}>
                {message}
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
                  fontSize: { xs: '0.9rem', sm: '1rem' },
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
                {loading ? <CircularProgress size={24} color="inherit" /> : 'Şifre Sıfırlama Bağlantısı Gönder'}
              </Button>

              <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
                <Link to="/login" style={{ textDecoration: 'none' }}>
                  <Button startIcon={<ArrowBack />} color="primary">
                    Giriş Sayfasına Dön
                  </Button>
                </Link>
              </Box>

              {message && (
                <Box sx={{ mt: 3, textAlign: 'center' }}>
                  <Typography variant="body2" color="textSecondary">
                    Email gelmedi mi?{' '}
                    <Button 
                      variant="text" 
                      size="small" 
                      onClick={handleSubmit}
                      disabled={loading}
                    >
                      Tekrar Gönder
                    </Button>
                  </Typography>
                </Box>
              )}
            </Box>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
};

export default ForgotPassword;