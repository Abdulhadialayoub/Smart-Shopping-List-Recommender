import React from 'react';
import { Container, Box } from '@mui/material';

interface LayoutProps {
  children: React.ReactNode;
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
}

const Layout: React.FC<LayoutProps> = ({ children, maxWidth = 'lg' }) => {
  return (
    <Container maxWidth={maxWidth}>
      <Box sx={{ py: { xs: 2, sm: 3 } }}>
        {children}
      </Box>
    </Container>
  );
};

export default Layout;