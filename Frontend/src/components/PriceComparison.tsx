import React, { useState } from 'react';
import {
  Typography,
  TextField,
  Button,
  Box,
  Card,
  CardContent,
  Grid,
  Chip,
  CircularProgress,
  Alert,
  InputAdornment,
  useTheme,
  useMediaQuery,
  Tooltip,
  Divider
} from '@mui/material';
// CardMedia removed - not used
import {
  Search,
  CompareArrows,
  Store,
  TrendingDown,
  LocalOffer,
  ShoppingCart,
  Info
} from '@mui/icons-material';
import type { CimriProduct } from '../types';
import { cimriApi } from '../services/api';
import Layout from './Layout';

const PriceComparisonPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [products, setProducts] = useState<CimriProduct[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  const handleSearch = async (page: number = 1) => {
    if (!searchTerm.trim()) {
      setError('Lütfen bir ürün adı girin');
      return;
    }

    try {
      setLoading(true);
      setError('');
      const response = await cimriApi.searchProducts(searchTerm, page);
      console.log('Cimri search response:', response.data);
      console.log('First product imageUrl:', response.data.products?.[0]?.imageUrl);
      setProducts(response.data.products || []);
      setCurrentPage(response.data.currentPage || 1);
      setTotalPages(response.data.totalPages || 1);
    } catch (error) {
      console.error('Cimri arama hatası:', error);
      setError('Ürün arama yapılırken bir hata oluştu. Lütfen tekrar deneyin.');
      setProducts([]);
    } finally {
      setLoading(false);
    }
  };

  const getBestPrice = () => {
    if (products.length === 0) return null;
    return products.sort((a, b) => a.price - b.price)[0];
  };

  const getWorstPrice = () => {
    if (products.length === 0) return null;
    return products.sort((a, b) => b.price - a.price)[0];
  };

  const getSavings = () => {
    const best = getBestPrice();
    const worst = getWorstPrice();
    return worst && best ? worst.price - best.price : 0;
  };

  return (
    <Layout>
      <Box sx={{ mb: 4 }}>
        <Typography
          variant={isMobile ? "h5" : "h4"}
          component="h1"
          sx={{
            fontWeight: 700,
            color: '#2e7d32',
            display: 'flex',
            alignItems: 'center',
            gap: 1,
            mb: 2
          }}
        >
          <CompareArrows sx={{ fontSize: { xs: 28, sm: 32 } }} />
          Fiyat Karşılaştırma
        </Typography>

        <Typography
          variant="body1"
          sx={{
            color: '#424242',
            maxWidth: 600
          }}
        >
          Ürün adını girerek farklı marketlerdeki fiyatları karşılaştırın ve en uygun seçeneği bulun.
        </Typography>
      </Box>

      {/* Arama Kutusu */}
      <Card sx={{ mb: 4, borderRadius: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', gap: 2, flexDirection: isMobile ? 'column' : 'row' }}>
            <TextField
              fullWidth
              label="Ürün Adı"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Örn: Süt, Ekmek, Domates"
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Search />
                  </InputAdornment>
                ),
              }}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
              sx={{
                '& .MuiOutlinedInput-root': {
                  borderRadius: 2,
                },
              }}
            />
            <Button
              variant="contained"
              onClick={() => handleSearch()}
              disabled={loading}
              size="large"
              sx={{
                minWidth: isMobile ? 'auto' : 150,
                borderRadius: 2,
                background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
                '&:hover': {
                  background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
                }
              }}
            >
              {loading ? <CircularProgress size={24} color="inherit" /> : 'Ara'}
            </Button>
          </Box>
        </CardContent>
      </Card>

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: 2 }}>
          {error}
        </Alert>
      )}

      {/* Özet Bilgiler */}
      {products.length > 0 && (
        <Grid container spacing={3} sx={{ mb: 4 }}>
          <Grid item xs={12} sm={4}>
            <Card sx={{ borderRadius: 3, bgcolor: '#e8f5e8' }}>
              <CardContent sx={{ textAlign: 'center' }}>
                <TrendingDown sx={{ fontSize: 40, color: '#2e7d32', mb: 1 }} />
                <Typography variant="h6" sx={{ fontWeight: 700, color: '#2e7d32' }}>
                  En Düşük Fiyat
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 700, color: '#1b5e20' }}>
                  {getBestPrice()?.price.toFixed(2)} ₺
                </Typography>
                <Typography variant="body2" sx={{ color: '#388e3c' }}>
                  {getBestPrice()?.merchantName}
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} sm={4}>
            <Card sx={{ borderRadius: 3, bgcolor: '#c8e6c9' }}>
              <CardContent sx={{ textAlign: 'center' }}>
                <LocalOffer sx={{ fontSize: 40, color: '#388e3c', mb: 1 }} />
                <Typography variant="h6" sx={{ fontWeight: 700, color: '#388e3c' }}>
                  Fiyat Farkı
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 700, color: '#2e7d32' }}>
                  {getSavings().toFixed(2)} ₺
                </Typography>
                <Typography variant="body2" sx={{ color: '#4caf50' }}>
                  En ucuz - En pahalı
                </Typography>
              </CardContent>
            </Card>
          </Grid>

          <Grid item xs={12} sm={4}>
            <Card sx={{ borderRadius: 3, bgcolor: '#e8f5e9' }}>
              <CardContent sx={{ textAlign: 'center' }}>
                <ShoppingCart sx={{ fontSize: 40, color: '#2e7d32', mb: 1 }} />
                <Typography variant="h6" sx={{ fontWeight: 700, color: '#2e7d32' }}>
                  Ürün Sayısı
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 700, color: '#1b5e20' }}>
                  {products.length}
                </Typography>
                <Typography variant="body2" sx={{ color: '#2e7d32' }}>
                  Bulunan sonuç
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Ürün Listesi */}
      {products.length > 0 && (
        <>
          <Typography variant="h5" sx={{ mb: 3, fontWeight: 700, color: '#212121' }}>
            Bulunan Ürünler ({products.length})
          </Typography>
          <Grid container spacing={3}>
            {products.map((product, index) => (
              <Grid item xs={12} sm={6} md={4} key={`${product.id}-${index}`}>
                <Card
                  sx={{
                    borderRadius: 3,
                    height: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                    transition: 'transform 0.2s, box-shadow 0.2s',
                    '&:hover': {
                      transform: 'translateY(-4px)',
                      boxShadow: 6
                    }
                  }}
                >
                  {/* Ürün Resmi */}
                  <Box
                    sx={{
                      height: 200,
                      bgcolor: '#f5f5f5',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      p: 2,
                      position: 'relative',
                      overflow: 'hidden'
                    }}
                  >
                    {product.imageUrl ? (
                      <Box
                        component="img"
                        src={product.imageUrl}
                        alt={product.name}
                        sx={{
                          maxWidth: '100%',
                          maxHeight: '100%',
                          objectFit: 'contain'
                        }}
                        onError={(e: any) => {
                          // Resim yüklenemezse placeholder göster
                          e.target.style.display = 'none';
                          const parent = e.target.parentElement;
                          if (parent) {
                            const placeholder = document.createElement('div');
                            placeholder.style.cssText = `
                              width: 100%;
                              height: 100%;
                              display: flex;
                              align-items: center;
                              justify-content: center;
                              background-color: hsl(${(product.name.charCodeAt(0) * 137) % 360}, 70%, 90%);
                              border-radius: 8px;
                            `;
                            placeholder.innerHTML = `
                              <span style="
                                font-size: 80px;
                                font-weight: 700;
                                color: hsl(${(product.name.charCodeAt(0) * 137) % 360}, 70%, 40%);
                                opacity: 0.3;
                              ">${product.name.charAt(0).toUpperCase()}</span>
                            `;
                            parent.appendChild(placeholder);
                          }
                        }}
                      />
                    ) : (
                      <Box
                        sx={{
                          width: '100%',
                          height: '100%',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          bgcolor: `hsl(${(product.name.charCodeAt(0) * 137) % 360}, 70%, 90%)`,
                          borderRadius: 2
                        }}
                      >
                        <Typography
                          variant="h1"
                          sx={{
                            fontSize: 80,
                            fontWeight: 700,
                            color: `hsl(${(product.name.charCodeAt(0) * 137) % 360}, 70%, 40%)`,
                            opacity: 0.3
                          }}
                        >
                          {product.name.charAt(0).toUpperCase()}
                        </Typography>
                      </Box>
                    )}
                  </Box>

                  <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
                    {/* Ürün Adı */}
                    <Typography
                      variant="h6"
                      sx={{
                        fontWeight: 600,
                        color: '#212121',
                        mb: 1,
                        minHeight: '3em',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical'
                      }}
                    >
                      {product.name}
                    </Typography>

                    {/* Marka */}
                    {product.brand && (
                      <Typography variant="body2" sx={{ color: '#757575', mb: 1 }}>
                        Marka: {product.brand}
                      </Typography>
                    )}

                    {/* Miktar Bilgisi */}
                    {product.quantity && product.unit && (
                      <Chip
                        label={`${product.quantity} ${product.unit}`}
                        size="small"
                        sx={{ mb: 2, width: 'fit-content' }}
                      />
                    )}

                    <Divider sx={{ my: 2 }} />

                    {/* Market Bilgisi */}
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                      <Store sx={{ fontSize: 20, color: '#2e7d32' }} />
                      <Typography variant="body2" sx={{ fontWeight: 600, color: '#2e7d32' }}>
                        {product.merchantName}
                      </Typography>
                    </Box>

                    {/* Fiyat Bilgisi */}
                    <Box sx={{ mt: 'auto' }}>
                      <Typography
                        variant="h4"
                        sx={{
                          fontWeight: 700,
                          color: '#2e7d32',
                          mb: 1
                        }}
                      >
                        {product.price.toFixed(2)} ₺
                      </Typography>

                      {/* Birim Fiyat */}
                      {product.unitPrice && (
                        <Tooltip title="Birim fiyat karşılaştırması için kullanışlıdır">
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <Info sx={{ fontSize: 16, color: '#757575' }} />
                            <Typography variant="caption" sx={{ color: '#757575' }}>
                              Birim fiyat: {product.unitPrice.toFixed(2)} ₺
                            </Typography>
                          </Box>
                        </Tooltip>
                      )}

                      {/* En İyi Fiyat Badge */}
                      {product === getBestPrice() && (
                        <Chip
                          label="🏆 En İyi Fiyat"
                          color="success"
                          size="small"
                          sx={{ mt: 1, fontWeight: 600 }}
                        />
                      )}
                    </Box>

                    {/* Ürün Linki */}
                    {product.productUrl && (
                      <Button
                        variant="outlined"
                        size="small"
                        href={product.productUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        sx={{ mt: 2 }}
                      >
                        Ürünü Görüntüle
                      </Button>
                    )}
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          {/* Sayfalama */}
          {totalPages > 1 && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4, gap: 2 }}>
              <Button
                variant="outlined"
                disabled={currentPage === 1 || loading}
                onClick={() => handleSearch(currentPage - 1)}
              >
                Önceki
              </Button>
              <Typography sx={{ display: 'flex', alignItems: 'center', px: 2 }}>
                Sayfa {currentPage} / {totalPages}
              </Typography>
              <Button
                variant="outlined"
                disabled={currentPage === totalPages || loading}
                onClick={() => handleSearch(currentPage + 1)}
              >
                Sonraki
              </Button>
            </Box>
          )}
        </>
      )}

      {/* Boş Durum */}
      {!loading && products.length === 0 && searchTerm && (
        <Box sx={{ textAlign: 'center', py: 8 }}>
          <Search sx={{ fontSize: 80, color: '#e0e0e0', mb: 2 }} />
          <Typography variant="h5" gutterBottom sx={{ color: '#424242' }}>
            Sonuç Bulunamadı
          </Typography>
          <Typography variant="body1" sx={{ color: '#757575' }}>
            "{searchTerm}" için ürün bulunamadı. Farklı bir arama terimi deneyin.
          </Typography>
        </Box>
      )}

      {/* İlk Durum */}
      {!loading && products.length === 0 && !searchTerm && (
        <Box sx={{ textAlign: 'center', py: 8 }}>
          <CompareArrows sx={{ fontSize: 80, color: '#e0e0e0', mb: 2 }} />
          <Typography variant="h5" gutterBottom sx={{ color: '#424242' }}>
            Ürün Araması Yapın
          </Typography>
          <Typography variant="body1" sx={{ color: '#757575' }}>
            Cimri.com'dan gerçek zamanlı fiyat karşılaştırması yapmak için yukarıdaki arama kutusunu kullanın.
          </Typography>
        </Box>
      )}
    </Layout>
  );
};

export default PriceComparisonPage;