import React from 'react';
import { Grid, type GridProps } from '@mui/material';

interface ResponsiveGridProps extends Omit<GridProps, 'container' | 'item'> {
  children: React.ReactNode;
  columns?: {
    xs?: number;
    sm?: number;
    md?: number;
    lg?: number;
    xl?: number;
  };
  spacing?: number | { xs?: number; sm?: number; md?: number; lg?: number; xl?: number };
}

const ResponsiveGrid: React.FC<ResponsiveGridProps> = ({
  children,
  columns = { xs: 1, sm: 2, md: 3, lg: 4 },
  spacing = { xs: 2, sm: 3, md: 4 },
  ...props
}) => {
  const spacingValue = typeof spacing === 'number' ? spacing : spacing;

  return (
    <Grid
      container
      spacing={spacingValue}
      {...props}
    >
      {React.Children.map(children, (child, index) => (
        <Grid
          item
          xs={12 / (columns.xs || 1)}
          sm={12 / (columns.sm || columns.xs || 1)}
          md={12 / (columns.md || columns.sm || columns.xs || 1)}
          lg={12 / (columns.lg || columns.md || columns.sm || columns.xs || 1)}
          xl={12 / (columns.xl || columns.lg || columns.md || columns.sm || columns.xs || 1)}
          key={index}
        >
          {child}
        </Grid>
      ))}
    </Grid>
  );
};

export default ResponsiveGrid;