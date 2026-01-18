import React from 'react';
import { Card, CardContent, CardActions } from '@mui/material';

interface ResponsiveCardProps {
  children: React.ReactNode;
  actions?: React.ReactNode;
  elevation?: number;
}

const ResponsiveCard: React.FC<ResponsiveCardProps> = ({ 
  children, 
  actions, 
  elevation = 2 
}) => {

  return (
    <Card
      elevation={elevation}
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        borderRadius: 3,
        transition: 'all 0.3s ease-in-out',
        '&:hover': {
          elevation: elevation + 2,
          transform: 'translateY(-2px)',
        },
      }}
    >
      <CardContent sx={{ flexGrow: 1, p: { xs: 2, sm: 3 } }}>
        {children}
      </CardContent>
      {actions && (
        <CardActions sx={{ p: { xs: 2, sm: 3 }, pt: 0 }}>
          {actions}
        </CardActions>
      )}
    </Card>
  );
};

export default ResponsiveCard;