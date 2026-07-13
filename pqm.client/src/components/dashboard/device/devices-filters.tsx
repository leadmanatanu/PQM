import * as React from 'react';
import Card from '@mui/material/Card';
import InputAdornment from '@mui/material/InputAdornment';
import OutlinedInput from '@mui/material/OutlinedInput';
import { MagnifyingGlassIcon } from '@phosphor-icons/react/dist/ssr/MagnifyingGlass';

interface DevicesFiltersProps {
    show?: boolean;
}

export function DevicesFilters({ show = true }: DevicesFiltersProps): React.JSX.Element | null{
    //console.log("isVisible " + isVisible);
    if (!show) return null;
    return (
      <OutlinedInput
        defaultValue=""
        fullWidth
        size="small"
        placeholder="Search device"
        startAdornment={
          <InputAdornment position="start">
            <MagnifyingGlassIcon fontSize="var(--icon-fontSize-md)" />
          </InputAdornment>
        }
        sx={{ 
          maxWidth: '300px', 
          width: '100%', 
          borderRadius: '8px',
          bgcolor: 'var(--mui-palette-background-paper)',
          '& .MuiOutlinedInput-notchedOutline': {
            borderColor: 'var(--mui-palette-divider)',
          }
        }}
      />
  );
}
