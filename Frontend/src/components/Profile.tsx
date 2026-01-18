import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Alert,
  CircularProgress,
  Chip,
  Link
} from '@mui/material';
// Divider removed - not used
import {
  Telegram,
  Save,
  CheckCircle,
  Person
} from '@mui/icons-material';
import Layout from './Layout';
import { useAuth } from '../contexts/AuthContext';
import { userApi } from '../services/api';

const Profile: React.FC = () => {
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [profile, setProfile] = useState({
    name: '',
    email: '',
    telegramChatId: '',
    telegramUsername: ''
  });

  useEffect(() => {
    loadProfile();
  }, [user]);

  const loadProfile = async () => {
    if (!user?.id) return;

    try {
      setLoading(true);
      const response = await userApi.getProfile(user.id);
      setProfile({
        name: response.data.name || '',
        email: response.data.email || '',
        telegramChatId: response.data.telegramChatId || '',
        telegramUsername: response.data.telegramUsername || ''
      });
    } catch (error) {
      console.error('Profil yüklenirken hata:', error);
      setError('Profil bilgileri yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!user?.id) return;

    try {
      setSaving(true);
      setError('');
      setSuccess('');

      await userApi.updateProfile(user.id, {
        name: profile.name,
        telegramChatId: profile.telegramChatId,
        telegramUsername: profile.telegramUsername
      });

      setSuccess('Profil başarıyla güncellendi!');
      setTimeout(() => setSuccess(''), 3000);
    } catch (error: any) {
      console.error('Profil güncellenirken hata:', error);
      setError(error.response?.data?.message || 'Profil güncellenemedi');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Layout>
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      </Layout>
    );
  }

  return (
    <Layout maxWidth="md">
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" sx={{ fontWeight: 700, color: '#2e7d32', display: 'flex', alignItems: 'center', gap: 1 }}>
          <Person />
          Profil Ayarları
        </Typography>
        <Typography variant="body1" sx={{ color: '#666', mt: 1 }}>
          Hesap bilgilerinizi ve Telegram entegrasyonunuzu yönetin
        </Typography>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError('')}>
          {error}
        </Alert>
      )}

      {success && (
        <Alert severity="success" sx={{ mb: 3 }} onClose={() => setSuccess('')}>
          {success}
        </Alert>
      )}

      {/* Hesap Bilgileri */}
      <Card sx={{ mb: 3, borderRadius: 3 }}>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 3, fontWeight: 600 }}>
            Hesap Bilgileri
          </Typography>

          <TextField
            fullWidth
            label="Ad Soyad"
            value={profile.name}
            onChange={(e) => setProfile({ ...profile, name: e.target.value })}
            sx={{ mb: 2 }}
          />

          <TextField
            fullWidth
            label="E-posta"
            value={profile.email}
            disabled
            helperText="E-posta adresi değiştirilemez"
            sx={{ mb: 2 }}
          />
        </CardContent>
      </Card>

      {/* Telegram Entegrasyonu */}
      <Card sx={{ mb: 3, borderRadius: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
            <Telegram sx={{ color: '#0088cc' }} />
            <Typography variant="h6" sx={{ fontWeight: 600 }}>
              Telegram Entegrasyonu
            </Typography>
            {profile.telegramChatId && (
              <Chip
                icon={<CheckCircle />}
                label="Bağlı"
                color="success"
                size="small"
              />
            )}
          </Box>

          <Alert severity="info" sx={{ mb: 3 }}>
            Telegram bot'umuzu kullanarak alışveriş listelerinizi ve fiyat uyarılarınızı alabilirsiniz.
          </Alert>

          <Box sx={{ mb: 3, p: 2, bgcolor: '#f5f5f5', borderRadius: 2 }}>
            <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1 }}>
              Nasıl Bağlanırım?
            </Typography>
            <Typography variant="body2" sx={{ mb: 1 }}>
              1. Telegram'da <Link href="https://t.me/alisveris_asistan_bot" target="_blank" rel="noopener">@alisveris_asistan_bot</Link> botunu bulun
            </Typography>
            <Typography variant="body2" sx={{ mb: 1 }}>
              2. Bot'a <strong>/start</strong> komutunu gönderin
            </Typography>
            <Typography variant="body2" sx={{ mb: 1 }}>
              3. Bot size Chat ID'nizi gönderecek
            </Typography>
            <Typography variant="body2">
              4. Chat ID'yi aşağıdaki alana yapıştırın
            </Typography>
          </Box>

          <TextField
            fullWidth
            label="Telegram Chat ID"
            value={profile.telegramChatId}
            onChange={(e) => setProfile({ ...profile, telegramChatId: e.target.value })}
            placeholder="Örn: 123456789"
            helperText="Bot'tan aldığınız Chat ID'yi buraya girin"
            sx={{ mb: 2 }}
          />

          <TextField
            fullWidth
            label="Telegram Kullanıcı Adı (Opsiyonel)"
            value={profile.telegramUsername}
            onChange={(e) => setProfile({ ...profile, telegramUsername: e.target.value })}
            placeholder="@kullaniciadi"
            helperText="Telegram kullanıcı adınız (@ ile başlayabilir)"
          />
        </CardContent>
      </Card>

      {/* Kaydet Butonu */}
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 2 }}>
        <Button
          variant="contained"
          size="large"
          startIcon={saving ? <CircularProgress size={20} color="inherit" /> : <Save />}
          onClick={handleSave}
          disabled={saving}
          sx={{
            borderRadius: 2,
            px: 4,
            background: 'linear-gradient(45deg, #2e7d32 30%, #4caf50 90%)'
          }}
        >
          {saving ? 'Kaydediliyor...' : 'Değişiklikleri Kaydet'}
        </Button>
      </Box>
    </Layout>
  );
};

export default Profile;
