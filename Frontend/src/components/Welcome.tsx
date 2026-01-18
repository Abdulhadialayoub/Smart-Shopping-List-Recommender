import React from 'react';
import { Link } from 'react-router-dom';
import {
  Container,
  Typography,
  Button,
  Box,
  Grid,
  Card,
  CardContent,
  AppBar,
  Toolbar,
  Stack,
  keyframes
} from '@mui/material';
import {
  Kitchen,
  Restaurant,
  ShoppingCart,
  TrendingDown,
  Notifications,
  Speed,
  ArrowForward
} from '@mui/icons-material';

// Animasyonlar
const wiggle = keyframes`
  0%, 100% { transform: rotate(0deg); }
  25% { transform: rotate(-10deg); }
  75% { transform: rotate(10deg); }
`;

const float = keyframes`
  0%, 100% { transform: translateY(0) rotate(0deg); }
  25% { transform: translateY(-10px) rotate(5deg); }
  50% { transform: translateY(-20px) rotate(0deg); }
  75% { transform: translateY(-10px) rotate(-5deg); }
`;

const pulse = keyframes`
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.1); opacity: 0.8; }
`;

const slideIn = keyframes`
  0% { transform: translateX(-100px); opacity: 0; }
  100% { transform: translateX(0); opacity: 1; }
`;

const cartMove = keyframes`
  0% { transform: translateX(-30px) rotate(-5deg); }
  25% { transform: translateX(0) rotate(0deg); }
  50% { transform: translateX(30px) rotate(5deg); }
  75% { transform: translateX(0) rotate(0deg); }
  100% { transform: translateX(-30px) rotate(-5deg); }
`;

const itemFall = keyframes`
  0% { transform: translateY(-50px) rotate(0deg); opacity: 0; }
  50% { opacity: 1; }
  100% { transform: translateY(0) rotate(360deg); opacity: 1; }
`;

const Welcome: React.FC = () => {
  const features = [
    {
      icon: <Kitchen sx={{ fontSize: 40 }} />,
      title: 'Akıllı Buzdolabı',
      description: 'Ürünlerinizi takip edin, son kullanma tarihlerini kontrol edin ve israfı önleyin.'
    },
    {
      icon: <Restaurant sx={{ fontSize: 40 }} />,
      title: 'AI Tarif Önerileri',
      description: 'Buzdolabınızdaki malzemelere göre yapay zeka destekli tarif önerileri alın.'
    },
    {
      icon: <ShoppingCart sx={{ fontSize: 40 }} />,
      title: 'Akıllı Alışveriş Listesi',
      description: 'Eksik malzemeler için otomatik alışveriş listesi oluşturun.'
    },
    {
      icon: <TrendingDown sx={{ fontSize: 40 }} />,
      title: 'Fiyat Karşılaştırma',
      description: 'Cimri.com entegrasyonu ile en uygun fiyatları anında bulun.'
    },
    {
      icon: <Notifications sx={{ fontSize: 40 }} />,
      title: 'Telegram Bildirimleri',
      description: 'Son kullanma tarihi yaklaşan ürünler için anlık bildirimler alın.'
    },
    {
      icon: <Speed sx={{ fontSize: 40 }} />,
      title: 'Hızlı ve Kolay',
      description: 'Kullanıcı dostu arayüz ile alışveriş deneyiminizi kolaylaştırın.'
    }
  ];

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: '#f8faf8' }}>
      {/* Navbar */}
      <AppBar position="static" elevation={0} sx={{ bgcolor: 'transparent', py: 1 }}>
        <Container maxWidth="lg">
          <Toolbar disableGutters sx={{ justifyContent: 'space-between' }}>
            <Typography 
              variant="h5" 
              sx={{ 
                fontWeight: 800, 
                color: '#2e7d32',
                display: 'flex',
                alignItems: 'center',
                gap: 1
              }}
            >
              🛒 Smart Shopper
            </Typography>
            <Stack direction="row" spacing={2}>
              <Button 
                component={Link} 
                to="/login" 
                sx={{ 
                  color: '#2e7d32', 
                  fontWeight: 600,
                  '&:hover': { bgcolor: 'rgba(46, 125, 50, 0.08)' }
                }}
              >
                Giriş Yap
              </Button>
              <Button 
                component={Link} 
                to="/register" 
                variant="contained"
                sx={{ 
                  bgcolor: '#2e7d32',
                  fontWeight: 600,
                  px: 3,
                  '&:hover': { bgcolor: '#1b5e20' }
                }}
              >
                Ücretsiz Başla
              </Button>
            </Stack>
          </Toolbar>
        </Container>
      </AppBar>

      {/* Hero Section */}
      <Box 
        sx={{ 
          background: 'linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 50%, #a5d6a7 100%)',
          py: { xs: 8, md: 12 },
          position: 'relative',
          overflow: 'hidden'
        }}
      >
        <Container maxWidth="lg">
          <Grid container spacing={4} alignItems="center">
            <Grid item xs={12} md={6}>
              <Typography 
                variant="h2" 
                sx={{ 
                  fontWeight: 800,
                  color: '#1b5e20',
                  fontSize: { xs: '2.5rem', md: '3.5rem' },
                  lineHeight: 1.2,
                  mb: 3,
                  animation: `${slideIn} 0.8s ease-out`
                }}
              >
                Akıllı Alışveriş
                <br />
                <Box 
                  component="span" 
                  sx={{ 
                    color: '#2e7d32',
                    display: 'inline-block',
                    animation: `${pulse} 2s ease-in-out infinite`
                  }}
                >
                  Asistanınız
                </Box>
              </Typography>
              <Typography 
                variant="h6" 
                sx={{ 
                  color: '#424242',
                  mb: 4,
                  fontWeight: 400,
                  lineHeight: 1.6,
                  animation: `${slideIn} 0.8s ease-out`,
                  animationDelay: '0.2s',
                  animationFillMode: 'backwards'
                }}
              >
                Buzdolabınızı yönetin, AI ile tarif keşfedin, 
                en uygun fiyatları bulun ve akıllı alışveriş yapın.
              </Typography>
              <Stack 
                direction={{ xs: 'column', sm: 'row' }} 
                spacing={2}
                sx={{
                  animation: `${slideIn} 0.8s ease-out`,
                  animationDelay: '0.4s',
                  animationFillMode: 'backwards'
                }}
              >
                <Button
                  component={Link}
                  to="/register"
                  variant="contained"
                  size="large"
                  endIcon={<ArrowForward />}
                  sx={{ 
                    bgcolor: '#2e7d32',
                    py: 1.5,
                    px: 4,
                    fontSize: '1.1rem',
                    fontWeight: 600,
                    transition: 'all 0.3s ease',
                    '&:hover': { 
                      bgcolor: '#1b5e20',
                      transform: 'scale(1.05)',
                      boxShadow: '0 8px 25px rgba(46, 125, 50, 0.3)'
                    }
                  }}
                >
                  Hemen Başla
                </Button>
                <Button
                  component={Link}
                  to="/login"
                  variant="outlined"
                  size="large"
                  sx={{ 
                    borderColor: '#2e7d32',
                    color: '#2e7d32',
                    py: 1.5,
                    px: 4,
                    fontSize: '1.1rem',
                    fontWeight: 600,
                    borderWidth: 2,
                    transition: 'all 0.3s ease',
                    '&:hover': { 
                      borderWidth: 2,
                      borderColor: '#1b5e20',
                      bgcolor: 'rgba(46, 125, 50, 0.08)',
                      transform: 'scale(1.05)'
                    }
                  }}
                >
                  Giriş Yap
                </Button>
              </Stack>
            </Grid>
            <Grid item xs={12} md={6}>
              <Box 
                sx={{ 
                  display: 'flex',
                  justifyContent: 'center',
                  alignItems: 'center',
                  position: 'relative',
                  height: { xs: 250, md: 350 }
                }}
              >
                {/* Ana Alışveriş Arabası */}
                <Box
                  sx={{
                    fontSize: { xs: '120px', md: '180px' },
                    animation: `${cartMove} 4s ease-in-out infinite`,
                    position: 'relative',
                    zIndex: 2
                  }}
                >
                  🛒
                </Box>
                
                {/* Düşen Ürünler */}
                <Box
                  sx={{
                    position: 'absolute',
                    top: { xs: '10%', md: '5%' },
                    left: { xs: '30%', md: '35%' },
                    fontSize: { xs: '30px', md: '40px' },
                    animation: `${itemFall} 2s ease-in-out infinite`,
                    animationDelay: '0s'
                  }}
                >
                  🍎
                </Box>
                <Box
                  sx={{
                    position: 'absolute',
                    top: { xs: '5%', md: '0%' },
                    right: { xs: '30%', md: '35%' },
                    fontSize: { xs: '30px', md: '40px' },
                    animation: `${itemFall} 2s ease-in-out infinite`,
                    animationDelay: '0.5s'
                  }}
                >
                  🥛
                </Box>
                <Box
                  sx={{
                    position: 'absolute',
                    top: { xs: '15%', md: '10%' },
                    left: { xs: '45%', md: '48%' },
                    fontSize: { xs: '25px', md: '35px' },
                    animation: `${itemFall} 2s ease-in-out infinite`,
                    animationDelay: '1s'
                  }}
                >
                  🥖
                </Box>
                <Box
                  sx={{
                    position: 'absolute',
                    top: { xs: '0%', md: '-5%' },
                    left: { xs: '55%', md: '55%' },
                    fontSize: { xs: '28px', md: '38px' },
                    animation: `${itemFall} 2s ease-in-out infinite`,
                    animationDelay: '1.5s'
                  }}
                >
                  🧀
                </Box>
                
                {/* Arka Plan Dekoratif Elementler */}
                <Box
                  sx={{
                    position: 'absolute',
                    bottom: '10%',
                    left: '10%',
                    fontSize: '50px',
                    animation: `${float} 3s ease-in-out infinite`,
                    opacity: 0.6
                  }}
                >
                  🥬
                </Box>
                <Box
                  sx={{
                    position: 'absolute',
                    bottom: '20%',
                    right: '10%',
                    fontSize: '45px',
                    animation: `${float} 3s ease-in-out infinite`,
                    animationDelay: '1s',
                    opacity: 0.6
                  }}
                >
                  🍊
                </Box>
                <Box
                  sx={{
                    position: 'absolute',
                    top: '30%',
                    left: '5%',
                    fontSize: '35px',
                    animation: `${pulse} 2s ease-in-out infinite`,
                    opacity: 0.5
                  }}
                >
                  ✨
                </Box>
                <Box
                  sx={{
                    position: 'absolute',
                    top: '20%',
                    right: '5%',
                    fontSize: '35px',
                    animation: `${pulse} 2s ease-in-out infinite`,
                    animationDelay: '0.5s',
                    opacity: 0.5
                  }}
                >
                  ✨
                </Box>
              </Box>
            </Grid>
          </Grid>
        </Container>
      </Box>

      {/* Features Section */}
      <Container maxWidth="lg" sx={{ py: { xs: 6, md: 10 } }}>
        <Typography 
          variant="h3" 
          align="center"
          sx={{ 
            fontWeight: 700,
            color: '#1b5e20',
            mb: 2
          }}
        >
          Özellikler
        </Typography>
        <Typography 
          variant="h6" 
          align="center"
          sx={{ 
            color: '#666',
            mb: 6,
            maxWidth: 600,
            mx: 'auto'
          }}
        >
          Akıllı alışveriş deneyimi için ihtiyacınız olan her şey
        </Typography>
        
        <Grid container spacing={4}>
          {features.map((feature, index) => (
            <Grid item xs={12} sm={6} md={4} key={index}>
              <Card 
                elevation={0}
                sx={{ 
                  height: '100%',
                  bgcolor: 'white',
                  border: '1px solid #e0e0e0',
                  borderRadius: 3,
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    transform: 'translateY(-8px)',
                    boxShadow: '0 12px 40px rgba(46, 125, 50, 0.15)',
                    borderColor: '#4caf50',
                    '& .feature-icon': {
                      animation: `${wiggle} 0.5s ease-in-out`,
                      bgcolor: '#4caf50',
                      color: 'white'
                    }
                  }
                }}
              >
                <CardContent sx={{ p: 4 }}>
                  <Box 
                    className="feature-icon"
                    sx={{ 
                      width: 64,
                      height: 64,
                      borderRadius: 2,
                      bgcolor: '#e8f5e9',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: '#2e7d32',
                      mb: 2,
                      transition: 'all 0.3s ease'
                    }}
                  >
                    {feature.icon}
                  </Box>
                  <Typography 
                    variant="h6" 
                    sx={{ 
                      fontWeight: 700,
                      color: '#212121',
                      mb: 1
                    }}
                  >
                    {feature.title}
                  </Typography>
                  <Typography 
                    variant="body2" 
                    sx={{ 
                      color: '#666',
                      lineHeight: 1.6
                    }}
                  >
                    {feature.description}
                  </Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </Container>

      {/* Demo Section */}
      <Box sx={{ bgcolor: '#2e7d32', py: { xs: 6, md: 8 } }}>
        <Container maxWidth="md">
          <Box textAlign="center">
            <Typography 
              variant="h4" 
              sx={{ 
                fontWeight: 700,
                color: 'white',
                mb: 2
              }}
            >
              Hemen Deneyin!
            </Typography>
            <Typography 
              variant="body1" 
              sx={{ 
                color: 'rgba(255,255,255,0.9)',
                mb: 4
              }}
            >
              Demo hesabı ile tüm özellikleri ücretsiz keşfedin
            </Typography>
            
            <Card 
              sx={{ 
                maxWidth: 400, 
                mx: 'auto', 
                borderRadius: 3,
                mb: 4
              }}
            >
              <CardContent sx={{ p: 3 }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
                  Demo Hesap Bilgileri
                </Typography>
                <Box sx={{ bgcolor: '#f5f5f5', p: 1.5, borderRadius: 1, mb: 1 }}>
                  <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                    📧 demo@smartshopper.com
                  </Typography>
                </Box>
                <Box sx={{ bgcolor: '#f5f5f5', p: 1.5, borderRadius: 1 }}>
                  <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                    🔒 demo123456
                  </Typography>
                </Box>
              </CardContent>
            </Card>

            <Button
              component={Link}
              to="/login"
              variant="contained"
              size="large"
              sx={{
                bgcolor: 'white',
                color: '#2e7d32',
                py: 1.5,
                px: 4,
                fontSize: '1rem',
                fontWeight: 600,
                '&:hover': {
                  bgcolor: '#f5f5f5'
                }
              }}
            >
              🚀 Demo ile Giriş Yap
            </Button>
          </Box>
        </Container>
      </Box>

      {/* Footer */}
      <Box sx={{ bgcolor: '#1b5e20', py: 4 }}>
        <Container maxWidth="lg">
          <Typography 
            variant="body2" 
            align="center"
            sx={{ color: 'rgba(255,255,255,0.8)' }}
          >
            © 2024 Smart Shopper. Tüm hakları saklıdır.
          </Typography>
        </Container>
      </Box>
    </Box>
  );
};

export default Welcome;
