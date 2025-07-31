'use client';

import * as React from 'react';
import { useState, useEffect, useRef } from 'react';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardActions from '@mui/material/CardActions';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Divider from '@mui/material/Divider';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import OutlinedInput from '@mui/material/OutlinedInput';
import Stack from '@mui/material/Stack';
import InputAdornment from '@mui/material/InputAdornment';
import Visibility from '@mui/icons-material/Visibility';
import VisibilityOff from '@mui/icons-material/VisibilityOff';
import IconButton from '@mui/material/IconButton';

interface FTPConfig {
  id: number;
  ftpHost: string;
  userName: string;
  password: string;
  rootFolderName: string;
}

interface FTPConfigProps {
  config?: FTPConfig; // Single object, not array
  onUpdate?: (data: FTPConfig) => void; // Updated to expect single object
  onTestConnection?: (data: FTPConfig) => void; // Updated to expect single object
}

//export function FTPDetailsForm(): React.JSX.Element {
export function FTPDetailsForm({
  config = { id: 0, ftpHost: '', userName: '', password: '', rootFolderName: '' },
  onUpdate, onTestConnection
}: FTPConfigProps): React.JSX.Element {
  const [formData, setFormData] = useState<FTPConfig>(config);
  const [showPassword, setShowPassword] = React.useState(false);

  // Update formData when config prop changes
  // Update formData only if config changes meaningfully
  // useEffect(() => {
  //   if (
  //     config.id !== formData.id ||
  //     config.ftpHost !== formData.ftpHost ||
  //     config.userName !== formData.userName ||
  //     config.password !== formData.password ||
  //     config.rootFolderName !== formData.rootFolderName
  //   ) {
  //     setFormData(config);
  //   }
  // }, [config, formData]); // Include formData to avoid stale state

   const isInitialMount = useRef(true);

  // Initialize formData with config on mount or when config changes
  useEffect(() => {
    if (isInitialMount.current) {
      setFormData(config);
      isInitialMount.current = false;
    } else if (
      config.id !== formData.id ||
      config.ftpHost !== formData.ftpHost ||
      config.userName !== formData.userName ||
      config.password !== formData.password ||
      config.rootFolderName !== formData.rootFolderName
    ) {
      // Only update formData if config changes and user hasn't modified form
      setFormData(config);
    }
  }, [config]);



  const handleClickShowPassword = () => setShowPassword((show) => !show);
  const handleMouseDownPassword = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.preventDefault();
  };

  const handleMouseUpPassword = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.preventDefault();
  };

  // Handle input changes
  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = event.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  // Handle update button click
  const handleUpdate = () => {
    if (onUpdate) {
      onUpdate(formData);
    }
  };

  // Placeholder for Test Connection
  const handleTestConnection = () => {
    console.log('Test Connection clicked'); // Placeholder
    onTestConnection(formData);
  };
  return (
    <Card>
      <Divider />
      <CardContent>
        <Stack spacing={3} sx={{ maxWidth: 'sm' }}>
          <FormControl fullWidth>
            <InputLabel>Enter URL</InputLabel>
            <OutlinedInput
              label="Enter URL"
              name="ftpHost"
              value={formData.ftpHost}
              onChange={handleInputChange}
            />
          </FormControl>
          <FormControl fullWidth>
            <InputLabel>Username</InputLabel>
            <OutlinedInput
              label="Username"
              name="userName"
              value={formData.userName}
              onChange={handleInputChange}
            />
          </FormControl>
          <FormControl fullWidth>
            <InputLabel htmlFor="outlined-adornment-password">Password</InputLabel>
            <OutlinedInput
              id="outlined-adornment-password"
              type={showPassword ? 'text' : 'password'}
              name="password"
              value={formData.password}
              onChange={handleInputChange}
              endAdornment={
                <InputAdornment position="end">
                  <IconButton
                    aria-label={showPassword ? 'hide the password' : 'display the password'}
                    onClick={handleClickShowPassword}
                    onMouseDown={handleMouseDownPassword}
                    onMouseUp={handleMouseUpPassword}
                    edge="end"
                  >
                    {showPassword ? <VisibilityOff /> : <Visibility />}
                  </IconButton>
                </InputAdornment>
              }
              label="Password"
            />
          </FormControl>
          <FormControl fullWidth>
            <InputLabel>Root Folder</InputLabel>
            <OutlinedInput
              label="Root Folder"
              name="rootFolderName"
              value={formData.rootFolderName}
              onChange={handleInputChange}
            />
          </FormControl>
        </Stack>
      </CardContent>
      <Divider />
      <CardActions sx={{ justifyContent: 'flex-end' }}>
        <Button variant="contained" onClick={handleTestConnection}>
          Test Connection
        </Button>
        <Button variant="contained" onClick={handleUpdate}>
          Update
        </Button>
      </CardActions>
    </Card>
  );
}
