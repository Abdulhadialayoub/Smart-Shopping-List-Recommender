import React, { useState, useEffect } from 'react';
import {
  Typography,
  Button,
  Grid,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  MenuItem,
  Chip,
  Box,
  Alert,
  useTheme,
  useMediaQuery,
  CircularProgress,
  IconButton,
  Table,
  TableBody,
  TableRow,
  TableCell
} from '@mui/material';
import { Add, Warning, Delete, Kitchen, PhotoCamera, Restaurant, Close } from '@mui/icons-material';
import type { FridgeItem, NutritionInfo } from '../types';
import { fridgeApi, imageApi, translateToEnglish } from '../services/api';
import Layout from './Layout';
import ResponsiveCard from './ResponsiveCard';

const categories = ['Sebze', 'Meyve', 'Et', 'Süt Ürünleri', 'Tahıl', 'Diğer'];
const units = ['adet', 'kg', 'gram', 'litre', 'ml', 'paket'];

const getCategoryColor = (category: string) => {
  const colors: { [key: string]: string } = {
    'Sebze': '#c8e6c9',
    'Meyve': '#ffcdd2',
    'Et': '#ffab91',
    'Süt Ürünleri': '#e1f5fe',
    'Tahıl': '#fff9c4',
    'Diğer': '#f3e5f5'
  };
  return colors[category] || '#e0e0e0';
};

const getCategoryEmoji = (category: string) => {
  const emojis: { [key: string]: string } = {
    'Sebze': '🥬',
    'Meyve': '🍎',
    'Et': '🥩',
    'Süt Ürünleri': '🥛',
    'Tahıl': '🌾',
    'Diğer': '📦'
  };
  return emojis[category] || '📦';
};

interface FridgeManagerProps {
  userId: string;
}

const FridgeManager: React.FC<FridgeManagerProps> = ({ userId }) => {
  const [items, setItems] = useState<FridgeItem[]>([]);
  const [open, setOpen] = useState(false);
  const [newItem, setNewItem] = useState({
    name: '',
    category: '',
    quantity: 1,
    unit: 'adet',
    expiryDate: '',
  });
  const [selectedImage, setSelectedImage] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [uploadingImage, setUploadingImage] = useState(false);
  const [nutritionDialogOpen, setNutritionDialogOpen] = useState(false);
  const [selectedNutrition, setSelectedNutrition] = useState<NutritionInfo | null>(null);
  const [loadingNutrition, setLoadingNutrition] = useState(false);
  const [nutritionError, setNutritionError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  useEffect(() => {
    loadFridgeItems();
  }, [userId]);

  const loadFridgeItems = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fridgeApi.getFridgeItems(userId);
      setItems(response.data);
    } catch (error) {
      console.error('Buzdolabı öğeleri yüklenirken hata:', error);
      setError('Buzdolabı öğeleri yüklenirken bir hata oluştu. Lütfen tekrar deneyin.');
    } finally {
      setLoading(false);
    }
  };

  const handleImageSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        alert('Dosya boyutu 5MB\'dan küçük olmalıdır');
        return;
      }
      setSelectedImage(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setImagePreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleAddItem = async () => {
    try {
      let imageUrl = '';
      
      if (selectedImage) {
        setUploadingImage(true);
        try {
          imageUrl = await imageApi.uploadImage(selectedImage);
        } catch (error) {
          console.error('Resim yüklenirken hata:', error);
          alert('Resim yüklenemedi, ürün resim olmadan eklenecek');
        } finally {
          setUploadingImage(false);
        }
      }

      const itemToAdd = {
        userId,
        name: newItem.name,
        category: newItem.category,
        quantity: newItem.quantity,
        unit: newItem.unit,
        expiryDate: new Date(newItem.expiryDate),
        addedDate: new Date(),
        isExpired: false,
        daysUntilExpiry: 0,
        imageUrl: imageUrl || undefined
      };

      await fridgeApi.addFridgeItem(itemToAdd);
      setOpen(false);
      setNewItem({
        name: '',
        category: '',
        quantity: 1,
        unit: 'adet',
        expiryDate: '',
      });
      setSelectedImage(null);
      setImagePreview(null);
      loadFridgeItems();
    } catch (error) {
      console.error('Öğe eklenirken hata:', error);
    }
  };

  const handleShowNutrition = async (productName: string) => {
    setNutritionDialogOpen(true);
    setLoadingNutrition(true);
    setNutritionError(null);
    setSelectedNutrition(null);

    try {
      const englishName = translateToEnglish(productName);
      const response = await fridgeApi.getNutrition(englishName);
      setSelectedNutrition(response.data);
    } catch (error) {
      console.error('Beslenme bilgisi alınırken hata:', error);
      setNutritionError('Beslenme bilgisi alınamadı. Lütfen tekrar deneyin.');
    } finally {
      setLoadingNutrition(false);
    }
  };

  const handleDeleteItem = async (itemId: string) => {
    try {
      await fridgeApi.deleteFridgeItem(itemId);
      loadFridgeItems();
    } catch (error) {
      console.error('Öğe silinirken hata:', error);
    }
  };

  const getExpiryColor = (daysUntilExpiry: number) => {
    if (daysUntilExpiry < 0) return 'error';
    if (daysUntilExpiry <= 2) return 'warning';
    return 'success';
  };

  const getExpiryText = (daysUntilExpiry: number) => {
    if (daysUntilExpiry < 0) return 'Süresi geçmiş';
    if (daysUntilExpiry === 0) return 'Bugün sona eriyor';
    if (daysUntilExpiry === 1) return '1 gün kaldı';
    return `${daysUntilExpiry} gün kaldı`;
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
          <Kitchen sx={{ fontSize: { xs: 28, sm: 32 } }} />
          Buzdolabım
        </Typography>
        <Button
          variant="contained"
          startIcon={<Add />}
          onClick={() => setOpen(true)}
          fullWidth={isMobile}
          size={isMobile ? "large" : "medium"}
          sx={{
            background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
            '&:hover': {
              background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
            }
          }}
        >
          Yeni Öğe Ekle
        </Button>
      </Box>

      {error && (
        <Alert
          severity="error"
          sx={{
            mb: 3,
            borderRadius: 2
          }}
          action={
            <Button
              color="inherit"
              size="small"
              onClick={loadFridgeItems}
            >
              Tekrar Dene
            </Button>
          }
        >
          <Typography variant="body1" sx={{ fontWeight: 500 }}>
            {error}
          </Typography>
        </Alert>
      )}

      {items.some(item => item.daysUntilExpiry <= 2) && !error && (
        <Alert
          severity="warning"
          sx={{
            mb: 3,
            borderRadius: 2,
            '& .MuiAlert-icon': {
              fontSize: '1.5rem'
            }
          }}
        >
          <Typography variant="body1" sx={{ fontWeight: 500 }}>
            ⚠️ Bazı ürünlerinizin son kullanma tarihi yaklaşıyor!
          </Typography>
          <Typography variant="body2" sx={{ mt: 0.5, opacity: 0.8 }}>
            Lütfen kontrol edin ve gerekirse kullanın.
          </Typography>
        </Alert>
      )}

      {loading ? (
        <Box sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          py: 8,
          gap: 2
        }}>
          <CircularProgress size={50} sx={{ color: '#2e7d32' }} />
          <Typography variant="h6" sx={{ color: '#424242', fontWeight: 600 }}>
            Buzdolabı yükleniyor...
          </Typography>
          <Typography variant="body2" sx={{ color: '#757575' }}>
            Ürünleriniz getiriliyor
          </Typography>
        </Box>
      ) : items.length === 0 ? (
        <Box sx={{
          textAlign: 'center',
          py: 8,
          px: 2
        }}>
          <Kitchen sx={{
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
            Buzdolabınız boş görünüyor
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
            İlk ürününüzü ekleyerek akıllı buzdolabı yönetimine başlayın!
          </Typography>
          <Button
            variant="contained"
            size="large"
            startIcon={<Add />}
            onClick={() => setOpen(true)}
            sx={{
              background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
              py: 1.5,
              px: 4,
              '&:hover': {
                background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
              }
            }}
          >
            İlk Ürünü Ekle
          </Button>
        </Box>
      ) : (
        <Grid
          container
          spacing={{ xs: 2, sm: 3, md: 4 }}
          sx={{ mb: 8 }}
        >
          {items.map((item) => (
            <Grid item xs={12} sm={6} md={4} key={item.id}>
              <ResponsiveCard
                actions={
                  <Box sx={{ display: 'flex', gap: 1, flexDirection: isMobile ? 'column' : 'row' }}>
                    <Button
                      size="small"
                      color="primary"
                      startIcon={<Restaurant />}
                      onClick={() => handleShowNutrition(item.name)}
                      fullWidth={isMobile}
                      variant="outlined"
                    >
                      🍎 Beslenme
                    </Button>
                    <Button
                      size="small"
                      color="error"
                      startIcon={<Delete />}
                      onClick={() => handleDeleteItem(item.id)}
                      fullWidth={isMobile}
                      variant="outlined"
                    >
                      Sil
                    </Button>
                  </Box>
                }
              >
                {item.imageUrl && (
                  <Box sx={{
                    width: '100%',
                    height: 150,
                    borderRadius: 2,
                    overflow: 'hidden',
                    mb: 2,
                    bgcolor: '#f5f5f5'
                  }}>
                    <img
                      src={item.imageUrl}
                      alt={item.name}
                      style={{
                        width: '100%',
                        height: '100%',
                        objectFit: 'cover'
                      }}
                    />
                  </Box>
                )}

                <Box sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1,
                  mb: 2
                }}>
                  <Box sx={{
                    width: 40,
                    height: 40,
                    borderRadius: '50%',
                    bgcolor: getCategoryColor(item.category),
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: '1.2rem'
                  }}>
                    {getCategoryEmoji(item.category)}
                  </Box>
                  <Box>
                    <Typography
                      variant="h6"
                      component="h2"
                      sx={{
                        fontWeight: 700,
                        color: '#212121',
                        lineHeight: 1.2
                      }}
                    >
                      {item.name}
                    </Typography>
                    <Typography
                      variant="body2"
                      sx={{
                        color: '#666',
                        fontWeight: 500
                      }}
                    >
                      {item.category}
                    </Typography>
                  </Box>
                </Box>

                <Box sx={{
                  bgcolor: '#f8f9fa',
                  borderRadius: 2,
                  p: 2,
                  mb: 2
                }}>
                  <Typography
                    variant="body1"
                    sx={{
                      fontWeight: 600,
                      color: '#424242'
                    }}
                  >
                    📦 Miktar: {item.quantity} {item.unit}
                  </Typography>
                </Box>

                <Chip
                  label={getExpiryText(item.daysUntilExpiry)}
                  color={getExpiryColor(item.daysUntilExpiry)}
                  size={isMobile ? "medium" : "small"}
                  icon={item.daysUntilExpiry <= 2 ? <Warning /> : undefined}
                  sx={{
                    fontWeight: 600,
                    fontSize: '0.85rem',
                    ...(isMobile && { width: '100%' })
                  }}
                />
              </ResponsiveCard>
            </Grid>
          ))}
        </Grid>
      )}

      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        maxWidth="sm"
        fullWidth
        fullScreen={isMobile}
        PaperProps={{
          sx: {
            ...(isMobile && {
              margin: 0,
              borderRadius: 0,
              maxHeight: '100vh'
            })
          }
        }}
      >
        <DialogTitle sx={{
          bgcolor: '#2e7d32',
          color: 'white',
          display: 'flex',
          alignItems: 'center',
          gap: 1
        }}>
          <Kitchen />
          Yeni Öğe Ekle
        </DialogTitle>
        <DialogContent sx={{ pt: 3 }}>
          <TextField
            autoFocus
            margin="dense"
            label="Ürün Adı"
            fullWidth
            variant="outlined"
            value={newItem.name}
            onChange={(e) => setNewItem({ ...newItem, name: e.target.value })}
            placeholder="Örn: Domates, Süt, Ekmek"
            sx={{
              '& .MuiOutlinedInput-root': {
                borderRadius: 2,
              },
            }}
          />

          <TextField
            select
            margin="dense"
            label="Kategori"
            fullWidth
            variant="outlined"
            value={newItem.category}
            onChange={(e) => setNewItem({ ...newItem, category: e.target.value })}
            sx={{
              '& .MuiOutlinedInput-root': {
                borderRadius: 2,
              },
            }}
          >
            {categories.map((category) => (
              <MenuItem key={category} value={category}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <span>{getCategoryEmoji(category)}</span>
                  {category}
                </Box>
              </MenuItem>
            ))}
          </TextField>

          <Box display="flex" gap={2}>
            <TextField
              margin="dense"
              label="Miktar"
              type="number"
              variant="outlined"
              value={newItem.quantity}
              onChange={(e) => setNewItem({ ...newItem, quantity: parseInt(e.target.value) })}
              sx={{
                '& .MuiOutlinedInput-root': {
                  borderRadius: 2,
                },
              }}
            />

            <TextField
              select
              margin="dense"
              label="Birim"
              variant="outlined"
              value={newItem.unit}
              onChange={(e) => setNewItem({ ...newItem, unit: e.target.value })}
              sx={{
                '& .MuiOutlinedInput-root': {
                  borderRadius: 2,
                },
              }}
            >
              {units.map((unit) => (
                <MenuItem key={unit} value={unit}>
                  {unit}
                </MenuItem>
              ))}
            </TextField>
          </Box>

          <TextField
            margin="dense"
            label="Son Kullanma Tarihi"
            type="date"
            fullWidth
            variant="outlined"
            InputLabelProps={{ shrink: true }}
            value={newItem.expiryDate || ''}
            onChange={(e) => setNewItem({ ...newItem, expiryDate: e.target.value })}
            helperText="Ürünün son kullanma tarihini seçin"
            sx={{
              '& .MuiOutlinedInput-root': {
                borderRadius: 2,
              },
            }}
          />

          <Box sx={{ mt: 2 }}>
            <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 600 }}>
              Ürün Fotoğrafı (İsteğe Bağlı)
            </Typography>
            <Button
              variant="outlined"
              component="label"
              startIcon={<PhotoCamera />}
              fullWidth
              sx={{ borderRadius: 2 }}
            >
              Fotoğraf Seç
              <input
                type="file"
                hidden
                accept="image/jpeg,image/png,image/webp"
                onChange={handleImageSelect}
              />
            </Button>
            {imagePreview && (
              <Box sx={{ mt: 2, position: 'relative' }}>
                <img
                  src={imagePreview}
                  alt="Önizleme"
                  style={{
                    width: '100%',
                    maxHeight: 200,
                    objectFit: 'cover',
                    borderRadius: 8
                  }}
                />
                <IconButton
                  size="small"
                  sx={{
                    position: 'absolute',
                    top: 8,
                    right: 8,
                    bgcolor: 'rgba(0,0,0,0.5)',
                    color: 'white',
                    '&:hover': { bgcolor: 'rgba(0,0,0,0.7)' }
                  }}
                  onClick={() => {
                    setSelectedImage(null);
                    setImagePreview(null);
                  }}
                >
                  <Close />
                </IconButton>
              </Box>
            )}
          </Box>
        </DialogContent>

        <DialogActions sx={{ p: 3, gap: 2 }}>
          <Button
            onClick={() => {
              setOpen(false);
              setSelectedImage(null);
              setImagePreview(null);
            }}
            fullWidth={isMobile}
            size={isMobile ? "large" : "medium"}
            disabled={uploadingImage}
          >
            İptal
          </Button>
          <Button
            onClick={handleAddItem}
            variant="contained"
            fullWidth={isMobile}
            size={isMobile ? "large" : "medium"}
            disabled={uploadingImage}
            sx={{
              background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
              '&:hover': {
                background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
              }
            }}
          >
            {uploadingImage ? <CircularProgress size={24} /> : 'Buzdolabına Ekle'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Nutrition Info Dialog */}
      <Dialog
        open={nutritionDialogOpen}
        onClose={() => setNutritionDialogOpen(false)}
        maxWidth="sm"
        fullWidth
        fullScreen={isMobile}
      >
        <DialogTitle sx={{
          bgcolor: '#2e7d32',
          color: 'white',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between'
        }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Restaurant />
            Beslenme Bilgisi
          </Box>
          <IconButton
            size="small"
            onClick={() => setNutritionDialogOpen(false)}
            sx={{ color: 'white' }}
          >
            <Close />
          </IconButton>
        </DialogTitle>
        <DialogContent sx={{ pt: 3 }}>
          {loadingNutrition ? (
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', py: 4, gap: 2 }}>
              <CircularProgress size={50} sx={{ color: '#2e7d32' }} />
              <Typography>Beslenme bilgisi yükleniyor...</Typography>
            </Box>
          ) : nutritionError ? (
            <Alert severity="error">{nutritionError}</Alert>
          ) : selectedNutrition ? (
            <Box>
              <Alert severity="info" sx={{ mb: 2 }}>
                100 gram başına besin değerleri
              </Alert>
              <Table>
                <TableBody>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 600 }}>🔥 Kalori</TableCell>
                    <TableCell align="right">{selectedNutrition.calories.toFixed(1)} kcal</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 600 }}>💪 Protein</TableCell>
                    <TableCell align="right">{selectedNutrition.protein.toFixed(1)} g</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 600 }}>🍞 Karbonhidrat</TableCell>
                    <TableCell align="right">{selectedNutrition.carbohydrates.toFixed(1)} g</TableCell>
                  </TableRow>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 600 }}>🥑 Yağ</TableCell>
                    <TableCell align="right">{selectedNutrition.fat.toFixed(1)} g</TableCell>
                  </TableRow>
                  {selectedNutrition.fiber !== undefined && selectedNutrition.fiber > 0 && (
                    <TableRow>
                      <TableCell sx={{ fontWeight: 600 }}>🌾 Lif</TableCell>
                      <TableCell align="right">{selectedNutrition.fiber.toFixed(1)} g</TableCell>
                    </TableRow>
                  )}
                  {selectedNutrition.sugar !== undefined && selectedNutrition.sugar > 0 && (
                    <TableRow>
                      <TableCell sx={{ fontWeight: 600 }}>🍬 Şeker</TableCell>
                      <TableCell align="right">{selectedNutrition.sugar.toFixed(1)} g</TableCell>
                    </TableRow>
                  )}
                  {selectedNutrition.salt !== undefined && selectedNutrition.salt > 0 && (
                    <TableRow>
                      <TableCell sx={{ fontWeight: 600 }}>🧂 Tuz</TableCell>
                      <TableCell align="right">{selectedNutrition.salt.toFixed(1)} g</TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </Box>
          ) : null}
        </DialogContent>
        <DialogActions sx={{ p: 2 }}>
          <Button
            onClick={() => setNutritionDialogOpen(false)}
            variant="contained"
            fullWidth={isMobile}
            sx={{
              background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
            }}
          >
            Kapat
          </Button>
        </DialogActions>
      </Dialog>
    </Layout>
  );
};

export default FridgeManager;