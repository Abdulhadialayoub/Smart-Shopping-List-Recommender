import React, { useState, useEffect } from 'react';
import {
  Typography,
  Grid,
  Chip,
  Box,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  List,
  ListItem,
  ListItemText,
  LinearProgress,
  Divider,
  useTheme,
  useMediaQuery,
  CircularProgress,
  Alert,
  AlertTitle
} from '@mui/material';
import { Restaurant, Timer, People, TrendingUp, MenuBook, Refresh, ShoppingCart, Search, OpenInNew, LocalOffer, Warning, Error as ErrorIcon } from '@mui/icons-material';
import type { Recipe, ExpiredItemInfo, ExpiringItemInfo } from '../types';
import { recipeApi, n8nApi } from '../services/api';
import Layout from './Layout';
import ResponsiveCard from './ResponsiveCard';

interface RecipeSuggestionsProps {
  userId: string;
}

interface ShoppingListResult {
  recipeName: string;
  priceComparisons: Array<{
    ingredient: string;
    cleanName: string;
    cheapestPrice: number;
    cheapestStore: string;
    productUrl?: string;
    isOnSale?: boolean;
    originalPrice?: number;
    discountPercentage?: number;
  }>;
  totalCost: number;
  message: string;
  shoppingListId: string;
}

// n8n'den gelen fiyat sonucu
interface N8nPriceResult {
  recipeName: string;
  missingCount: number;
  products?: Array<{
    product: string;
    minPrice: number;
    maxPrice: number;
    store: string;
    optionCount: number;
  }>;
  estimatedMinCost?: number;
  estimatedMaxCost?: number;
  message: string;
}

const RecipeSuggestions: React.FC<RecipeSuggestionsProps> = ({ userId }) => {
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [selectedRecipe, setSelectedRecipe] = useState<Recipe | null>(null);
  const [loading, setLoading] = useState(false);
  const [servings, setServings] = useState(2);
  const [creatingShoppingList, setCreatingShoppingList] = useState(false);
  const [shoppingListResult, setShoppingListResult] = useState<ShoppingListResult | null>(null);
  const [n8nPriceResult, setN8nPriceResult] = useState<N8nPriceResult | null>(null);
  const [checkingPrices, setCheckingPrices] = useState(false);
  const [expiredItems, setExpiredItems] = useState<ExpiredItemInfo[]>([]);
  const [expiringItems, setExpiringItems] = useState<ExpiringItemInfo[]>([]);
  const [includeBasicIngredients] = useState(false); // Temel baharatlar seçeneği
  // expiryMessage removed - not used in UI
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  useEffect(() => {
    loadRecipeSuggestions();
  }, [userId, servings]);

  const loadRecipeSuggestions = async () => {
    try {
      setLoading(true);
      const response = await recipeApi.getRecipeSuggestions(userId, servings);

      // Yeni response yapısını kullan
      const data = response.data;
      setRecipes(data.recipes || []);
      setExpiredItems(data.expiredItems || []);
      setExpiringItems(data.expiringItems || []);
      // data.message not used in UI

    } catch (error) {
      console.error('Tarif önerileri yüklenirken hata:', error);
    } finally {
      setLoading(false);
    }
  };

  const createShoppingListForRecipe = async () => {
    if (!selectedRecipe || creatingShoppingList) return; // Çift tıklama önleme

    try {
      setCreatingShoppingList(true);
      const response = await recipeApi.createRecipeShoppingList({
        userId,
        recipeName: selectedRecipe.name,
        missingIngredients: selectedRecipe.missingIngredients || [],
        includeBasicIngredients // Temel baharatları dahil et/etme
      });

      // Sonucu state'e kaydet ve dialog'u göster
      setShoppingListResult(response.data);

    } catch (error) {
      console.error('Alışveriş listesi oluşturulurken hata:', error);
      alert('❌ Alışveriş listesi oluşturulamadı. Lütfen tekrar deneyin.');
    } finally {
      setCreatingShoppingList(false);
    }
  };

  // n8n ile fiyat kontrolü (tarif seçildiğinde)
  const checkPricesWithN8n = async (recipe: Recipe) => {
    setCheckingPrices(true);
    try {
      const result = await n8nApi.checkRecipePrices(userId, {
        name: recipe.name,
        ingredients: recipe.ingredients
      });

      if (result) {
        setN8nPriceResult(result);
      }
    } catch (error) {
      console.error('n8n fiyat kontrolü hatası:', error);
    } finally {
      setCheckingPrices(false);
    }
  };

  // Tarif seçildiğinde hem dialog'u aç hem n8n'e istek at
  const handleRecipeSelect = (recipe: Recipe) => {
    setSelectedRecipe(recipe);
    // n8n webhook URL varsa fiyat kontrolü yap
    if (import.meta.env.VITE_N8N_WEBHOOK_URL) {
      checkPricesWithN8n(recipe);
    }
  };

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty.toLowerCase()) {
      case 'kolay': return 'success';
      case 'orta': return 'warning';
      case 'zor': return 'error';
      default: return 'default';
    }
  };

  const getMatchColor = (percentage: number) => {
    if (percentage >= 80) return 'success';
    if (percentage >= 60) return 'warning';
    return 'error';
  };

  return (
    <Layout>
      <Box
        display="flex"
        justifyContent="space-between"
        alignItems={isMobile ? "flex-start" : "center"}
        flexDirection={isMobile ? "column" : "row"}
        gap={2}
        mb={3}
      >
        <Box>
          <Typography
            variant={isMobile ? "h5" : "h4"}
            component="h1"
            sx={{
              fontWeight: 700,
              color: '#2e7d32',
              display: 'flex',
              alignItems: 'center',
              gap: 1
            }}
          >
            <MenuBook sx={{ fontSize: { xs: 28, sm: 32 } }} />
            Tarif Önerileri
          </Typography>

          <Typography
            variant="body1"
            sx={{
              color: '#424242',
              mt: 1,
              maxWidth: 500
            }}
          >
            Buzdolabınızdaki malzemelere göre size özel tarif önerileri
          </Typography>
        </Box>

        <Box display="flex" gap={2} flexDirection={isMobile ? "column" : "row"} width={isMobile ? "100%" : "auto"}>
          <Box display="flex" gap={1} flexWrap="wrap">
            {[1, 2, 4, 6].map((num) => (
              <Chip
                key={num}
                label={`${num} Kişilik`}
                icon={<People />}
                onClick={() => setServings(num)}
                color={servings === num ? "primary" : "default"}
                sx={{
                  backgroundColor: servings === num ? '#2e7d32' : '#f5f5f5',
                  color: servings === num ? 'white' : '#424242',
                  '&:hover': {
                    backgroundColor: servings === num ? '#1b5e20' : '#eeeeee',
                  },
                  fontWeight: servings === num ? 600 : 400,
                }}
              />
            ))}
          </Box>

          <Button
            variant="outlined"
            startIcon={<Refresh />}
            onClick={loadRecipeSuggestions}
            disabled={loading}
            sx={{
              borderColor: '#2e7d32',
              color: '#2e7d32',
              minWidth: isMobile ? '100%' : 'auto',
              '&:hover': {
                borderColor: '#1b5e20',
                backgroundColor: 'rgba(46, 125, 50, 0.04)',
              },
              '&:disabled': {
                borderColor: '#e0e0e0',
                color: '#9e9e9e',
              }
            }}
          >
            {loading ? 'Yenileniyor...' : 'Yenile'}
          </Button>
        </Box>
      </Box>

      {loading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mb: 4 }}>
          <CircularProgress size={40} sx={{ color: '#2e7d32' }} />
        </Box>
      )}

      {/* Süresi Geçmiş/Yaklaşan Ürün Uyarıları */}
      {(expiredItems.length > 0 || expiringItems.length > 0) && !loading && (
        <Box sx={{ mb: 3 }}>
          {expiredItems.length > 0 && (
            <Alert
              severity="error"
              icon={<ErrorIcon />}
              sx={{ mb: 2, borderRadius: 2 }}
            >
              <AlertTitle sx={{ fontWeight: 600 }}>
                ⚠️ {expiredItems.length} Ürünün Süresi Geçmiş!
              </AlertTitle>
              <Typography variant="body2" sx={{ mb: 1 }}>
                Bu ürünler tarif önerilerinde kullanılmadı:
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {expiredItems.map((item) => (
                  <Chip
                    key={item.name}
                    label={`${item.name} (${item.daysExpired} gün önce)`}
                    size="small"
                    color="error"
                    variant="outlined"
                  />
                ))}
              </Box>
            </Alert>
          )}

          {expiringItems.length > 0 && (
            <Alert
              severity="warning"
              icon={<Warning />}
              sx={{ borderRadius: 2 }}
            >
              <AlertTitle sx={{ fontWeight: 600 }}>
                ⏰ {expiringItems.length} Ürünün Süresi Yaklaşıyor!
              </AlertTitle>
              <Typography variant="body2" sx={{ mb: 1 }}>
                Önce bu ürünleri kullanmayı düşünün:
              </Typography>
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {expiringItems.map((item, index) => (
                  <Chip
                    key={index}
                    label={`${item.name} (${item.daysUntilExpiry} gün kaldı)`}
                    size="small"
                    color="warning"
                    variant="outlined"
                  />
                ))}
              </Box>
            </Alert>
          )}
        </Box>
      )}

      {recipes.length === 0 && !loading ? (
        <Box sx={{
          textAlign: 'center',
          py: 8,
          px: 2
        }}>
          <MenuBook sx={{
            fontSize: 80,
            color: '#e0e0e0',
            mb: 2
          }} />
          <Typography
            variant="h5"
            gutterBottom
            sx={{
              color: '#424242',
              fontWeight: 600
            }}
          >
            Henüz tarif önerisi yok
          </Typography>
          <Typography
            variant="body1"
            sx={{
              color: '#757575',
              mb: 3,
              maxWidth: 400,
              mx: 'auto'
            }}
          >
            Buzdolabınıza ürün ekleyerek size özel tarif önerileri alabilirsiniz!
          </Typography>
          <Button
            variant="contained"
            size="large"
            startIcon={<Restaurant />}
            onClick={loadRecipeSuggestions}
            sx={{
              background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
              py: 1.5,
              px: 4,
              '&:hover': {
                background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
              }
            }}
          >
            Tarif Önerilerini Yenile
          </Button>
        </Box>
      ) : (
        <Grid
          container
          spacing={{ xs: 2, sm: 3, md: 4 }}
          sx={{ mb: 8 }}
        >
          {recipes.map((recipe) => (
            <Grid item xs={12} md={6} lg={4} key={recipe.id}>
              <ResponsiveCard
                actions={
                  <Button
                    variant="contained"
                    fullWidth={isMobile}
                    startIcon={<Restaurant />}
                    onClick={() => handleRecipeSelect(recipe)}
                    sx={{
                      background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
                      '&:hover': {
                        background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
                      }
                    }}
                  >
                    Tarifi Görüntüle
                  </Button>
                }
              >
                <Typography
                  variant="h6"
                  component="h2"
                  gutterBottom
                  sx={{
                    fontWeight: 700,
                    color: '#212121'
                  }}
                >
                  {recipe.name}
                </Typography>

                <Typography
                  variant="body2"
                  sx={{
                    color: '#424242',
                    mb: 2,
                    lineHeight: 1.5
                  }}
                >
                  {recipe.description}
                </Typography>

                <Box display="flex" alignItems="center" gap={1} mb={2}>
                  <TrendingUp fontSize="small" sx={{ color: '#2e7d32' }} />
                  <Typography variant="body2" sx={{ fontWeight: 600, color: '#212121' }}>
                    Eşleşme: %{recipe.matchPercentage.toFixed(0)}
                  </Typography>
                  <LinearProgress
                    variant="determinate"
                    value={recipe.matchPercentage}
                    color={getMatchColor(recipe.matchPercentage)}
                    sx={{
                      flexGrow: 1,
                      ml: 1,
                      height: 6,
                      borderRadius: 3
                    }}
                  />
                </Box>

                <Box display="flex" gap={1} mb={2} flexWrap="wrap">
                  <Chip
                    icon={<Timer />}
                    label={`${recipe.prepTimeMinutes + recipe.cookTimeMinutes} dk`}
                    size="small"
                    sx={{
                      bgcolor: '#e8f5e9',
                      color: '#2e7d32',
                      fontWeight: 600
                    }}
                  />
                  <Chip
                    icon={<People />}
                    label={`${recipe.servings} kişi`}
                    size="small"
                    sx={{
                      bgcolor: '#f3e5f5',
                      color: '#7b1fa2',
                      fontWeight: 600
                    }}
                  />
                  <Chip
                    label={recipe.difficulty}
                    size="small"
                    color={getDifficultyColor(recipe.difficulty)}
                    sx={{ fontWeight: 600 }}
                  />
                </Box>

                <Box sx={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  bgcolor: '#f5f5f5',
                  p: 2,
                  borderRadius: 2,
                  mb: 2
                }}>
                  <Typography
                    variant="body2"
                    sx={{
                      color: '#2e7d32',
                      fontWeight: 600
                    }}
                  >
                    ✅ Mevcut: {recipe.availableIngredients.length}
                  </Typography>

                  <Typography
                    variant="body2"
                    sx={{
                      color: '#d32f2f',
                      fontWeight: 600
                    }}
                  >
                    ❌ Eksik: {recipe.missingIngredients.length}
                  </Typography>
                </Box>
              </ResponsiveCard>
            </Grid>
          ))}
        </Grid>
      )}

      <Dialog
        open={!!selectedRecipe}
        onClose={() => {
          setSelectedRecipe(null);
          setN8nPriceResult(null);
        }}
        maxWidth="md"
        fullWidth
      >
        {selectedRecipe && (
          <>
            <DialogTitle>
              <Typography variant="h5" sx={{ color: '#212121', fontWeight: 700 }}>
                {selectedRecipe.name}
              </Typography>
              <Typography variant="body2" sx={{ color: '#666' }}>
                {selectedRecipe.description}
              </Typography>
            </DialogTitle>

            <DialogContent>
              <Box display="flex" gap={2} mb={3} flexWrap="wrap">
                <Chip
                  icon={<Timer />}
                  label={`Hazırlık: ${selectedRecipe.prepTimeMinutes} dk`}
                  variant="outlined"
                  sx={{ borderColor: '#bdbdbd', color: '#424242' }}
                />
                <Chip
                  icon={<Timer />}
                  label={`Pişirme: ${selectedRecipe.cookTimeMinutes} dk`}
                  variant="outlined"
                  sx={{ borderColor: '#bdbdbd', color: '#424242' }}
                />
                <Chip
                  icon={<People />}
                  label={`${selectedRecipe.servings} kişi`}
                  variant="outlined"
                  sx={{ borderColor: '#bdbdbd', color: '#424242' }}
                />
              </Box>

              <Typography variant="h6" gutterBottom sx={{ color: '#212121', fontWeight: 600 }}>
                Malzemeler
              </Typography>

              <List dense>
                {selectedRecipe.ingredients.map((ingredient, index) => (
                  <ListItem key={index}>
                    <ListItemText
                      primary={ingredient}
                      sx={{
                        '& .MuiListItemText-primary': {
                          color: '#212121',
                          fontWeight: 500
                        }
                      }}
                    />
                    <Chip
                      label={
                        selectedRecipe.availableIngredients.includes(ingredient)
                          ? 'Mevcut'
                          : 'Eksik'
                      }
                      size="small"
                      color={
                        selectedRecipe.availableIngredients.includes(ingredient)
                          ? 'success'
                          : 'error'
                      }
                    />
                  </ListItem>
                ))}
              </List>

              <Divider sx={{ my: 2 }} />

              <Typography variant="h6" gutterBottom sx={{ color: '#212121', fontWeight: 600 }}>
                Yapılış
              </Typography>

              <List>
                {selectedRecipe.instructions.map((instruction, index) => (
                  <ListItem key={index}>
                    <ListItemText
                      primary={`${index + 1}. ${instruction}`}
                      sx={{
                        '& .MuiListItemText-primary': {
                          color: '#212121'
                        }
                      }}
                    />
                  </ListItem>
                ))}
              </List>

              {selectedRecipe.nutrition && (
                <>
                  <Divider sx={{ my: 2 }} />
                  <Typography variant="h6" gutterBottom sx={{ color: '#212121' }}>
                    Besin Değerleri (Porsiyon başına)
                  </Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={6}>
                      <Typography variant="body2" sx={{ color: '#424242' }}>
                        Kalori: {selectedRecipe.nutrition.calories} kcal
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" sx={{ color: '#424242' }}>
                        Protein: {selectedRecipe.nutrition.protein}g
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" sx={{ color: '#424242' }}>
                        Karbonhidrat: {selectedRecipe.nutrition.carbohydrates}g
                      </Typography>
                    </Grid>
                    <Grid item xs={6}>
                      <Typography variant="body2" sx={{ color: '#424242' }}>
                        Yağ: {selectedRecipe.nutrition.fat}g
                      </Typography>
                    </Grid>
                  </Grid>
                </>
              )}

              {/* n8n Fiyat Sonuçları */}
              {(checkingPrices || n8nPriceResult) && (
                <>
                  <Divider sx={{ my: 2 }} />
                  <Typography variant="h6" gutterBottom sx={{ color: '#212121', display: 'flex', alignItems: 'center', gap: 1 }}>
                    <ShoppingCart fontSize="small" />
                    Fiyat Analizi (n8n)
                  </Typography>

                  {checkingPrices ? (
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, p: 2, bgcolor: '#f5f5f5', borderRadius: 2 }}>
                      <CircularProgress size={24} sx={{ color: '#2e7d32' }} />
                      <Typography variant="body2" sx={{ color: '#757575' }}>
                        Fiyatlar kontrol ediliyor...
                      </Typography>
                    </Box>
                  ) : n8nPriceResult && (
                    <Box sx={{ bgcolor: '#e8f5e9', borderRadius: 2, p: 2 }}>
                      <Typography variant="body1" sx={{ color: '#2e7d32', fontWeight: 600, mb: 1 }}>
                        {n8nPriceResult.message}
                      </Typography>

                      {n8nPriceResult.estimatedMinCost !== undefined && (
                        <Typography variant="h6" sx={{ color: '#1b5e20', fontWeight: 700 }}>
                          💰 Tahmini Maliyet: {n8nPriceResult.estimatedMinCost?.toFixed(2)} - {n8nPriceResult.estimatedMaxCost?.toFixed(2)} TL
                        </Typography>
                      )}

                      {n8nPriceResult.products && n8nPriceResult.products.length > 0 && (
                        <Box sx={{ mt: 2 }}>
                          {n8nPriceResult.products.map((product, index) => (
                            <Box key={index} sx={{ display: 'flex', justifyContent: 'space-between', py: 0.5 }}>
                              <Typography variant="body2">{product.product}</Typography>
                              <Typography variant="body2" sx={{ fontWeight: 600, color: '#2e7d32' }}>
                                {product.minPrice > 0 ? `${product.minPrice.toFixed(2)} TL` : 'Bulunamadı'}
                              </Typography>
                            </Box>
                          ))}
                        </Box>
                      )}
                    </Box>
                  )}
                </>
              )}
            </DialogContent>

            <DialogActions sx={{ justifyContent: 'space-between', px: 3, pb: 2 }}>
              <Button onClick={() => {
                setSelectedRecipe(null);
                setN8nPriceResult(null);
              }}>
                Kapat
              </Button>
              {selectedRecipe && selectedRecipe.missingIngredients && selectedRecipe.missingIngredients.length > 0 && (
                <Button
                  variant="contained"
                  onClick={createShoppingListForRecipe}
                  disabled={creatingShoppingList}
                  sx={{
                    background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
                    '&:hover': {
                      background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
                    }
                  }}
                >
                  {creatingShoppingList ? 'Oluşturuluyor...' : `Eksik ${selectedRecipe.missingIngredients.length} Malzeme için Alışveriş Listesi`}
                </Button>
              )}
            </DialogActions>
          </>
        )}
      </Dialog>

      {/* Loading Dialog - Web Scraping Progress */}
      <Dialog
        open={creatingShoppingList}
        maxWidth="sm"
        fullWidth
        PaperProps={{
          sx: {
            borderRadius: 3,
            p: 2
          }
        }}
      >
        <DialogContent>
          <Box
            display="flex"
            flexDirection="column"
            alignItems="center"
            gap={3}
            py={2}
          >
            <Box position="relative" display="inline-flex">
              <CircularProgress
                size={80}
                thickness={4}
                sx={{
                  color: '#2e7d32',
                  animationDuration: '1s'
                }}
              />
              <Box
                sx={{
                  top: 0,
                  left: 0,
                  bottom: 0,
                  right: 0,
                  position: 'absolute',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <ShoppingCart sx={{ fontSize: 32, color: '#2e7d32' }} />
              </Box>
            </Box>

            <Box textAlign="center">
              <Typography
                variant="h6"
                gutterBottom
                sx={{
                  fontWeight: 700,
                  color: '#212121'
                }}
              >
                Alışveriş Listesi Oluşturuluyor
              </Typography>

              <Typography
                variant="body2"
                sx={{
                  color: '#757575',
                  mb: 2
                }}
              >
                Eksik malzemeler için fiyat karşılaştırması yapılıyor...
              </Typography>

              <Box
                sx={{
                  bgcolor: '#e8f5e9',
                  borderRadius: 2,
                  p: 2,
                  mt: 2
                }}
              >
                <Box display="flex" alignItems="center" gap={1} mb={1}>
                  <Search sx={{ fontSize: 20, color: '#2e7d32' }} />
                  <Typography
                    variant="body2"
                    sx={{
                      color: '#424242',
                      fontWeight: 600
                    }}
                  >
                    Web Scraping Yapılıyor
                  </Typography>
                </Box>
                <Typography
                  variant="caption"
                  sx={{
                    color: '#616161',
                    display: 'block'
                  }}
                >
                  Cimri.com'dan en uygun fiyatlar aranıyor. Bu işlem 10-30 saniye sürebilir.
                </Typography>
              </Box>
            </Box>

            <LinearProgress
              sx={{
                width: '100%',
                height: 6,
                borderRadius: 3,
                bgcolor: '#c8e6c9',
                '& .MuiLinearProgress-bar': {
                  bgcolor: '#2e7d32'
                }
              }}
            />
          </Box>
        </DialogContent>
      </Dialog>

      {/* Result Dialog - Shopping List Created */}
      <Dialog
        open={!!shoppingListResult}
        onClose={() => setShoppingListResult(null)}
        maxWidth="md"
        fullWidth
        PaperProps={{
          sx: {
            borderRadius: 3
          }
        }}
      >
        {shoppingListResult && (
          <>
            <DialogTitle sx={{ bgcolor: '#f5f5f5', borderBottom: '2px solid #2e7d32' }}>
              <Box display="flex" alignItems="center" gap={2}>
                <Box
                  sx={{
                    bgcolor: '#2e7d32',
                    borderRadius: '50%',
                    p: 1.5,
                    display: 'flex'
                  }}
                >
                  <ShoppingCart sx={{ color: 'white', fontSize: 28 }} />
                </Box>
                <Box>
                  <Typography variant="h5" sx={{ fontWeight: 700, color: '#212121' }}>
                    Alışveriş Listesi Oluşturuldu!
                  </Typography>
                  <Typography variant="body2" sx={{ color: '#757575' }}>
                    {shoppingListResult.recipeName}
                  </Typography>
                </Box>
              </Box>
            </DialogTitle>

            <DialogContent sx={{ pt: 3 }}>
              <Box
                sx={{
                  bgcolor: '#e8f5e9',
                  borderRadius: 2,
                  p: 2,
                  mb: 3,
                  border: '1px solid #4caf50'
                }}
              >
                <Typography variant="body1" sx={{ color: '#2e7d32', fontWeight: 600 }}>
                  ✅ {shoppingListResult.message}
                </Typography>
              </Box>

              <Box
                sx={{
                  bgcolor: '#e8f5e9',
                  borderRadius: 2,
                  p: 2,
                  mb: 3
                }}
              >
                <Typography variant="h6" sx={{ color: '#1b5e20', fontWeight: 700, mb: 1 }}>
                  💰 Toplam Tahmini Maliyet
                </Typography>
                <Typography variant="h4" sx={{ color: '#2e7d32', fontWeight: 700 }}>
                  {shoppingListResult.totalCost.toFixed(2)} TL
                </Typography>
                <Typography variant="caption" sx={{ color: '#757575' }}>
                  En uygun fiyatlarla hesaplandı
                </Typography>
              </Box>

              <Typography variant="h6" sx={{ mb: 2, fontWeight: 600 }}>
                📋 Malzemeler ve Fiyatlar
              </Typography>

              <List>
                {shoppingListResult.priceComparisons.map((item, index) => (
                  <ListItem
                    key={index}
                    sx={{
                      bgcolor: index % 2 === 0 ? '#fafafa' : 'white',
                      borderRadius: 1,
                      mb: 1,
                      border: item.isOnSale ? '2px solid #ff9800' : 'none'
                    }}
                  >
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="body1" sx={{ fontWeight: 600 }}>
                            {item.cleanName}
                          </Typography>
                          {item.isOnSale && (
                            <Chip
                              icon={<LocalOffer sx={{ fontSize: 14 }} />}
                              label={item.discountPercentage ? `%${item.discountPercentage} İndirim` : 'İndirimde'}
                              size="small"
                              sx={{
                                bgcolor: '#ff9800',
                                color: 'white',
                                fontWeight: 600,
                                fontSize: '0.7rem'
                              }}
                            />
                          )}
                          {item.productUrl && (
                            <Button
                              size="small"
                              href={item.productUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                              sx={{
                                minWidth: 'auto',
                                p: 0.5,
                                color: '#1976d2'
                              }}
                            >
                              <OpenInNew fontSize="small" />
                            </Button>
                          )}
                        </Box>
                      }
                      secondary={
                        <Box display="flex" alignItems="center" gap={1} mt={0.5}>
                          <Chip
                            label={item.cheapestStore}
                            size="small"
                            sx={{
                              bgcolor: '#e8f5e9',
                              color: '#2e7d32',
                              fontWeight: 600
                            }}
                          />
                          <Typography variant="body2" sx={{ color: '#757575' }}>
                            • En uygun fiyat
                          </Typography>
                        </Box>
                      }
                    />
                    <Box textAlign="right">
                      {item.isOnSale && item.originalPrice && (
                        <Typography
                          variant="body2"
                          sx={{
                            color: '#9e9e9e',
                            textDecoration: 'line-through',
                            fontSize: '0.8rem'
                          }}
                        >
                          {item.originalPrice.toFixed(2)} TL
                        </Typography>
                      )}
                      <Typography
                        variant="h6"
                        sx={{
                          color: item.isOnSale ? '#ff9800' : '#2e7d32',
                          fontWeight: 700
                        }}
                      >
                        {item.cheapestPrice.toFixed(2)} TL
                      </Typography>
                    </Box>
                  </ListItem>
                ))}
              </List>

              <Box
                sx={{
                  bgcolor: '#f5f5f5',
                  borderRadius: 2,
                  p: 2,
                  mt: 3
                }}
              >
                <Typography variant="body2" sx={{ color: '#616161', textAlign: 'center' }}>
                  💡 Bu liste "Alışveriş Listeleri" sayfanızda kaydedildi. İstediğiniz zaman görüntüleyebilirsiniz.
                </Typography>
              </Box>
            </DialogContent>

            <DialogActions sx={{ p: 3, bgcolor: '#fafafa' }}>
              <Button
                onClick={() => setShoppingListResult(null)}
                sx={{ color: '#757575' }}
              >
                Kapat
              </Button>
              <Button
                variant="contained"
                onClick={() => {
                  setShoppingListResult(null);
                  // React Router navigation
                  window.location.href = '/app/shopping';
                }}
                sx={{
                  background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
                  '&:hover': {
                    background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
                  }
                }}
              >
                Alışveriş Listelerime Git
              </Button>
            </DialogActions>
          </>
        )}
      </Dialog>
    </Layout>
  );
};

export default RecipeSuggestions;