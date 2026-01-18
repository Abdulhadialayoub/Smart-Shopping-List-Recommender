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
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Checkbox,
  IconButton,
  Box,
  Chip,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  useTheme,
  useMediaQuery,
  CircularProgress
} from '@mui/material';
import {
  Add,
  Delete,
  ShoppingCart,
  ExpandMore,
  CompareArrows,
  ShoppingBasket
} from '@mui/icons-material';
import type { ShoppingList, PriceComparison } from '../types';
import { shoppingApi } from '../services/api';
import Layout from './Layout';
import ResponsiveCard from './ResponsiveCard';

interface ShoppingListsProps {
  userId: string;
}

const ShoppingLists: React.FC<ShoppingListsProps> = ({ userId }) => {
  const [lists, setLists] = useState<ShoppingList[]>([]);
  const [open, setOpen] = useState(false);
  const [newListName, setNewListName] = useState('');
  const [newItems, setNewItems] = useState([{ name: '', quantity: 1, unit: 'adet' }]);
  const [priceComparisons, setPriceComparisons] = useState<{[key: string]: PriceComparison[]}>({});
  const [loading, setLoading] = useState(false);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  useEffect(() => {
    loadShoppingLists();
  }, [userId]);

  const loadShoppingLists = async () => {
    try {
      setLoading(true);
      const response = await shoppingApi.getShoppingLists(userId);
      setLists(response.data);
    } catch (error) {
      console.error('Alışveriş listeleri yüklenirken hata:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateList = async () => {
    try {
      const validItems = newItems.filter(item => item.name.trim() !== '');
      
      const newList: Omit<ShoppingList, 'id'> = {
        userId,
        name: newListName,
        items: validItems.map(item => ({
          name: item.name,
          quantity: item.quantity,
          unit: item.unit,
          isChecked: false
        })),
        createdAt: new Date(),
        updatedAt: new Date(),
        isCompleted: false,
        totalEstimatedCost: 0
      };

      await shoppingApi.createShoppingList(newList);
      setOpen(false);
      setNewListName('');
      setNewItems([{ name: '', quantity: 1, unit: 'adet' }]);
      loadShoppingLists();
    } catch (error) {
      console.error('Liste oluşturulurken hata:', error);
    }
  };

  const addNewItem = () => {
    setNewItems([...newItems, { name: '', quantity: 1, unit: 'adet' }]);
  };

  const removeItem = (index: number) => {
    if (newItems.length > 1) {
      setNewItems(newItems.filter((_, i) => i !== index));
    }
  };

  const updateItem = (index: number, field: string, value: any) => {
    const updatedItems = newItems.map((item, i) => 
      i === index ? { ...item, [field]: value } : item
    );
    setNewItems(updatedItems);
  };

  const handleCreateSmartList = async () => {
    try {
      setLoading(true);
      await shoppingApi.createSmartShoppingList(userId);
      loadShoppingLists();
    } catch (error) {
      console.error('Akıllı liste oluşturulurken hata:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteList = async (listId: string) => {
    try {
      await shoppingApi.deleteShoppingList(listId);
      loadShoppingLists();
    } catch (error) {
      console.error('Liste silinirken hata:', error);
    }
  };

  const handleToggleItem = async (listId: string, itemId: string) => {
    const list = lists.find(l => l.id === listId);
    if (!list) return;

    const updatedItems = list.items.map(item =>
      item.id === itemId ? { ...item, isChecked: !item.isChecked } : item
    );

    const updatedList = { ...list, items: updatedItems };

    try {
      await shoppingApi.updateShoppingList(listId, updatedList);
      loadShoppingLists();
    } catch (error) {
      console.error('Öğe güncellenirken hata:', error);
    }
  };

  const handleComparePrices = async (productName: string) => {
    try {
      const response = await shoppingApi.comparePrices(productName);
      setPriceComparisons(prev => ({
        ...prev,
        [productName]: response.data.comparisons || []
      }));
    } catch (error) {
      console.error('Fiyat karşılaştırması yapılırken hata:', error);
    }
  };

  const getCompletionPercentage = (list: ShoppingList) => {
    if (list.items.length === 0) return 0;
    const checkedItems = list.items.filter(item => item.isChecked).length;
    return Math.round((checkedItems / list.items.length) * 100);
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
          <ShoppingBasket sx={{ fontSize: { xs: 28, sm: 32 } }} />
          Alışveriş Listelerim
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, flexDirection: isMobile ? 'column' : 'row' }}>
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
            Yeni Liste Oluştur
          </Button>
          <Button
            variant="outlined"
            startIcon={<ShoppingCart />}
            onClick={handleCreateSmartList}
            fullWidth={isMobile}
            size={isMobile ? "large" : "medium"}
            sx={{
              borderColor: '#2e7d32',
              color: '#2e7d32',
              '&:hover': {
                borderColor: '#1b5e20',
                color: '#1b5e20',
                bgcolor: 'rgba(46, 125, 50, 0.04)'
              }
            }}
          >
            🤖 Akıllı Liste
          </Button>
        </Box>
      </Box>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress size={40} sx={{ color: '#2e7d32' }} />
        </Box>
      ) : lists.length === 0 ? (
        <Box sx={{ 
          textAlign: 'center', 
          py: 8,
          px: 2
        }}>
          <ShoppingBasket sx={{ 
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
            Henüz alışveriş listeniz yok
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
            İlk alışveriş listenizi oluşturarak akıllı alışveriş deneyimine başlayın!
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
            İlk Listenizi Oluşturun
          </Button>
        </Box>
      ) : (
        <Grid 
          container 
          spacing={{ xs: 2, sm: 3, md: 4 }}
          sx={{ mb: 8 }}
        >
          {lists.map((list) => (
            <Grid item xs={12} md={6} key={list.id}>
              <ResponsiveCard
                actions={
                  <Button
                    size="small"
                    color="error"
                    startIcon={<Delete />}
                    onClick={() => handleDeleteList(list.id)}
                    fullWidth={isMobile}
                    variant="outlined"
                  >
                    Listeyi Sil
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
                  {list.name}
                </Typography>

                <Box display="flex" alignItems="center" gap={2} mb={3}>
                  <Chip
                    icon={<ShoppingCart />}
                    label={`${list.items.length} öğe`}
                    size="small"
                    sx={{ 
                      bgcolor: '#e8f5e9',
                      color: '#2e7d32',
                      fontWeight: 600
                    }}
                  />
                  <Chip
                    label={`%${getCompletionPercentage(list)} tamamlandı`}
                    size="small"
                    color={getCompletionPercentage(list) === 100 ? 'success' : 'default'}
                    sx={{ fontWeight: 600 }}
                  />
                </Box>

                <Accordion 
                  defaultExpanded={list.items.length <= 5}
                  sx={{ 
                    boxShadow: 'none',
                    border: '1px solid #e0e0e0',
                    borderRadius: '8px !important',
                    mb: 2,
                    '&:before': { display: 'none' }
                  }}
                >
                  <AccordionSummary
                    expandIcon={<ExpandMore />}
                    sx={{
                      bgcolor: '#f8f9fa',
                      borderRadius: '8px',
                      minHeight: 48,
                      '&.Mui-expanded': {
                        minHeight: 48,
                        borderBottomLeftRadius: 0,
                        borderBottomRightRadius: 0
                      }
                    }}
                  >
                    <Typography sx={{ fontWeight: 600, color: '#424242' }}>
                      📋 Ürünler ({list.items.filter(i => i.isChecked).length}/{list.items.length})
                    </Typography>
                  </AccordionSummary>
                  <AccordionDetails sx={{ p: 0 }}>
                    <List dense sx={{ py: 0 }}>
                      {list.items.map((item, index) => (
                        <ListItem 
                          key={item.id} 
                          dense 
                          sx={{ 
                            px: 2,
                            py: 1,
                            borderBottom: index < list.items.length - 1 ? '1px solid #f0f0f0' : 'none',
                            '&:hover': {
                              bgcolor: '#fafafa'
                            }
                          }}
                        >
                          <Checkbox
                            edge="start"
                            checked={item.isChecked}
                            onChange={() => handleToggleItem(list.id, item.id || '')}
                            size="small"
                            sx={{ mr: 1 }}
                          />
                          <ListItemText
                            primary={
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <Typography
                                  sx={{
                                    textDecoration: item.isChecked ? 'line-through' : 'none',
                                    opacity: item.isChecked ? 0.6 : 1,
                                    fontSize: '0.95rem',
                                    fontWeight: item.isChecked ? 400 : 500,
                                    color: '#212121'
                                  }}
                                >
                                  {item.name}
                                </Typography>
                                {item.category && (
                                  <Chip 
                                    label={item.category} 
                                    size="small"
                                    sx={{ 
                                      height: 20,
                                      fontSize: '0.7rem',
                                      bgcolor: '#e8f5e9',
                                      color: '#2e7d32'
                                    }}
                                  />
                                )}
                              </Box>
                            }
                            secondary={`${item.quantity} ${item.unit}`}
                            sx={{
                              '& .MuiListItemText-secondary': {
                                fontSize: '0.8rem',
                                color: '#666'
                              }
                            }}
                          />
                          <ListItemSecondaryAction>
                            <Button
                              size="small"
                              variant="outlined"
                              startIcon={<CompareArrows />}
                              onClick={() => handleComparePrices(item.name)}
                              sx={{ 
                                fontSize: '0.75rem',
                                py: 0.5,
                                px: 1.5,
                                borderColor: '#2e7d32',
                                color: '#2e7d32',
                                '&:hover': {
                                  bgcolor: '#2e7d32',
                                  color: '#fff',
                                  borderColor: '#2e7d32'
                                }
                              }}
                            >
                              Fiyat Karşılaştır
                            </Button>
                          </ListItemSecondaryAction>
                        </ListItem>
                      ))}
                    </List>
                  </AccordionDetails>
                </Accordion>

                {/* Fiyat Karşılaştırmaları */}
                {Object.entries(priceComparisons).map(([productName, comparisons]) => (
                  <Box 
                    key={productName} 
                    sx={{ 
                      mt: 2,
                      border: '2px solid #2e7d32',
                      borderRadius: 2,
                      overflow: 'hidden'
                    }}
                  >
                    <Box 
                      sx={{ 
                        bgcolor: '#2e7d32',
                        color: 'white',
                        p: 2,
                        display: 'flex',
                        alignItems: 'center',
                        gap: 1
                      }}
                    >
                      <CompareArrows />
                      <Typography variant="h6" sx={{ fontWeight: 700 }}>
                        {productName}
                      </Typography>
                    </Box>
                    
                    {comparisons.length === 0 ? (
                      <Box sx={{ p: 3, textAlign: 'center' }}>
                        <Typography variant="body2" sx={{ color: '#757575' }}>
                          Bu ürün için fiyat bulunamadı
                        </Typography>
                      </Box>
                    ) : (
                      <List sx={{ p: 0 }}>
                        {comparisons
                          .sort((a, b) => a.price - b.price)
                          .map((comparison, index) => {
                            const isBest = index === 0;
                            return (
                              <ListItem 
                                key={index}
                                sx={{
                                  borderBottom: index < comparisons.length - 1 ? '1px solid #e0e0e0' : 'none',
                                  bgcolor: isBest ? '#e8f5e9' : 'white',
                                  py: 2
                                }}
                              >
                                <Box sx={{ display: 'flex', alignItems: 'center', width: '100%', gap: 2 }}>
                                  {isBest && (
                                    <Box 
                                      sx={{ 
                                        bgcolor: '#4caf50',
                                        color: 'white',
                                        borderRadius: '50%',
                                        width: 32,
                                        height: 32,
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        fontSize: '1.2rem'
                                      }}
                                    >
                                      🏆
                                    </Box>
                                  )}
                                  <Box sx={{ flex: 1 }}>
                                    <Typography 
                                      variant="body1" 
                                      sx={{ 
                                        fontWeight: isBest ? 700 : 600,
                                        color: '#212121'
                                      }}
                                    >
                                      {comparison.store}
                                    </Typography>
                                    {comparison.isAvailable ? (
                                      <Chip 
                                        label="Stokta" 
                                        size="small"
                                        sx={{ 
                                          mt: 0.5,
                                          height: 20,
                                          fontSize: '0.7rem',
                                          bgcolor: '#e8f5e9',
                                          color: '#2e7d32'
                                        }}
                                      />
                                    ) : (
                                      <Chip 
                                        label="Stokta Yok" 
                                        size="small"
                                        sx={{ 
                                          mt: 0.5,
                                          height: 20,
                                          fontSize: '0.7rem',
                                          bgcolor: '#ffebee',
                                          color: '#c62828'
                                        }}
                                      />
                                    )}
                                  </Box>
                                  <Box sx={{ textAlign: 'right' }}>
                                    <Typography 
                                      variant="h6" 
                                      sx={{ 
                                        fontWeight: 700,
                                        color: isBest ? '#2e7d32' : '#424242'
                                      }}
                                    >
                                      {comparison.price.toFixed(2)} TL
                                    </Typography>
                                    {isBest && (
                                      <Typography 
                                        variant="caption" 
                                        sx={{ 
                                          color: '#2e7d32',
                                          fontWeight: 600
                                        }}
                                      >
                                        En Uygun Fiyat
                                      </Typography>
                                    )}
                                  </Box>
                                </Box>
                              </ListItem>
                            );
                          })}
                      </List>
                    )}
                  </Box>
                ))}
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
          <ShoppingBasket />
          Yeni Alışveriş Listesi
        </DialogTitle>
        <DialogContent sx={{ pt: 3 }}>
          <TextField
            autoFocus
            margin="dense"
            label="Liste Adı"
            fullWidth
            variant="outlined"
            value={newListName}
            onChange={(e) => setNewListName(e.target.value)}
            placeholder="Örn: Haftalık Alışveriş"
            sx={{
              '& .MuiOutlinedInput-root': {
                borderRadius: 2,
              },
              mb: 3
            }}
          />

          <Typography variant="h6" gutterBottom sx={{ mt: 2, mb: 2 }}>
            Ürünler
          </Typography>

          {newItems.map((item, index) => (
            <Box key={index} sx={{ display: 'flex', gap: 1, mb: 2, alignItems: 'center' }}>
              <TextField
                label="Ürün Adı"
                value={item.name}
                onChange={(e) => updateItem(index, 'name', e.target.value)}
                placeholder="Örn: Ekmek, Süt"
                sx={{ flex: 2 }}
                size="small"
              />
              <TextField
                label="Miktar"
                type="number"
                value={item.quantity}
                onChange={(e) => updateItem(index, 'quantity', parseInt(e.target.value) || 1)}
                sx={{ width: 80 }}
                size="small"
                inputProps={{ min: 1 }}
              />
              <TextField
                select
                label="Birim"
                value={item.unit}
                onChange={(e) => updateItem(index, 'unit', e.target.value)}
                sx={{ width: 100 }}
                size="small"
                SelectProps={{ native: true }}
              >
                <option value="adet">adet</option>
                <option value="kg">kg</option>
                <option value="gram">gram</option>
                <option value="litre">litre</option>
                <option value="ml">ml</option>
                <option value="paket">paket</option>
              </TextField>
              {newItems.length > 1 && (
                <IconButton 
                  onClick={() => removeItem(index)}
                  color="error"
                  size="small"
                >
                  <Delete />
                </IconButton>
              )}
            </Box>
          ))}

          <Button
            startIcon={<Add />}
            onClick={addNewItem}
            variant="outlined"
            size="small"
            sx={{ mt: 1 }}
          >
            Ürün Ekle
          </Button>
        </DialogContent>
        <DialogActions sx={{ p: 3, gap: 2 }}>
          <Button 
            onClick={() => setOpen(false)}
            fullWidth={isMobile}
            size={isMobile ? "large" : "medium"}
          >
            İptal
          </Button>
          <Button 
            onClick={handleCreateList} 
            variant="contained"
            fullWidth={isMobile}
            size={isMobile ? "large" : "medium"}
            sx={{
              background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)',
              '&:hover': {
                background: 'linear-gradient(45deg, #1b5e20 30%, #388e3c 90%)',
              }
            }}
          >
            Oluştur
          </Button>
        </DialogActions>
      </Dialog>
    </Layout>
  );
};

export default ShoppingLists;