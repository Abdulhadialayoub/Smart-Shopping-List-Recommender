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
  FormControlLabel,
  Checkbox,
  useTheme,
  useMediaQuery,
  Avatar
} from '@mui/material';
import { Email, Lock, Person, PersonAdd } from '@mui/icons-material';
import { useAuth } from '../../contexts/AuthContext';

const Register: React.FC = () => {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    password: '',
    confirmPassword: ''
  });
  const [acceptTerms, setAcceptTerms] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { register } = useAuth();
  const navigate = useNavigate();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const validateForm = () => {
    if (!formData.name || !formData.email || !formData.password || !formData.confirmPassword) {
      setError('Lütfen tüm alanları doldurun');
      return false;
    }

    if (formData.password !== formData.confirmPassword) {
      setError('Şifreler eşleşmiyor');
      return false;
    }

    if (formData.password.length < 6) {
      setError('Şifre en az 6 karakter olmalıdır');
      return false;
    }

    if (!acceptTerms) {
      setError('Kullanım koşullarını kabul etmelisiniz');
      return false;
    }

    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    try {
      setError('');
      setLoading(true);
      await register(formData.name, formData.email, formData.password);
      navigate('/app');
    } catch (error: any) {
      console.error('Register error:', error);
      setError(getErrorMessage(error.message));
    } finally {
      setLoading(false);
    }
  };

  const getErrorMessage = (errorMessage: string) => {
    if (errorMessage.includes('zaten kullanımda')) {
      return 'Bu email adresi zaten kullanımda';
    }
    return errorMessage || 'Hesap oluşturulurken bir hata oluştu';
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
              <PersonAdd sx={{ fontSize: { xs: 28, sm: 32 } }} />
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
              Hesap Oluştur
            </Typography>
            
            <Typography 
              variant="body1" 
              color="textSecondary" 
              align="center" 
              sx={{ mb: 4, maxWidth: 400 }}
            >
              Akıllı alışveriş deneyiminizi başlatın
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
                id="name"
                label="Ad Soyad"
                name="name"
                autoComplete="name"
                autoFocus
                value={formData.name}
                onChange={handleChange}
                InputProps={{
                  startAdornment: <Person sx={{ mr: 1, color: 'action.active' }} />
                }}
              />

              <TextField
                margin="normal"
                required
                fullWidth
                id="email"
                label="Email Adresi"
                name="email"
                autoComplete="email"
                value={formData.email}
                onChange={handleChange}
                InputProps={{
                  startAdornment: <Email sx={{ mr: 1, color: 'action.active' }} />
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
                autoComplete="new-password"
                value={formData.password}
                onChange={handleChange}
                InputProps={{
                  startAdornment: <Lock sx={{ mr: 1, color: 'action.active' }} />
                }}
                helperText="En az 6 karakter olmalıdır"
              />

              <TextField
                margin="normal"
                required
                fullWidth
                name="confirmPassword"
                label="Şifre Tekrar"
                type="password"
                id="confirmPassword"
                autoComplete="new-password"
                value={formData.confirmPassword}
                onChange={handleChange}
                InputProps={{
                  startAdornment: <Lock sx={{ mr: 1, color: 'action.active' }} />
                }}
              />

              <FormControlLabel
                control={
                  <Checkbox
                    checked={acceptTerms}
                    onChange={(e) => setAcceptTerms(e.target.checked)}
                    color="primary"
                  />
                }
                label={
                  <Typography variant="body2">
                    <Link to="/terms" style={{ color: 'inherit' }}>
                      Kullanım Koşulları
                    </Link>
                    {' '}ve{' '}
                    <Link to="/privacy" style={{ color: 'inherit' }}>
                      Gizlilik Politikası
                    </Link>
                    'nı kabul ediyorum
                  </Typography>
                }
                sx={{ mt: 2 }}
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
                {loading ? <CircularProgress size={24} color="inherit" /> : 'Hesap Oluştur'}
              </Button>

              <Box sx={{ textAlign: 'center', mt: 2 }}>
                <Typography variant="body2">
                  Zaten hesabınız var mı?{' '}
                  <Link to="/login" style={{ textDecoration: 'none' }}>
                    <Typography component="span" variant="body2" color="primary">
                      Giriş Yap
                    </Typography>
                  </Link>
                </Typography>
              </Box>
            </Box>
          </Box>
        </Paper>
      </Container>
    </Box>
  );
};

export default Register;