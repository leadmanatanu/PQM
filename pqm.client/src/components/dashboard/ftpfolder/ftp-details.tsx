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
import LoadingButton from '@mui/lab/LoadingButton';
import CircularProgress from '@mui/material/CircularProgress';
import FormHelperText from '@mui/material/FormHelperText';
import Box from '@mui/material/Box';

export interface FTPConfig {
  id: number;
  ftpHost: string;
  userName: string;
  password: string;
  rootFolderName: string;
}

interface FTPConfigProps {
  config?: FTPConfig;
  onUpdate?: (data: FTPConfig) => void;
  onTestConnection?: (data: FTPConfig) => void;
}

export function FTPDetailsForm({
  config = { id: 0, ftpHost: '', userName: '', password: '', rootFolderName: '' },
  onUpdate,
  onTestConnection,
}: FTPConfigProps): React.JSX.Element {
  const [formData, setFormData] = useState<FTPConfig>(config);
  const [showPassword, setShowPassword] = useState(false);
  const [errors, setErrors] = useState({
    ftpHost: '',
    userName: '',
    password: '',
    rootFolderName: '',
    general: '',
  });
  const [loading, setLoading] = useState<'update' | 'test' | null>(null);

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
      setFormData(config);
    }
  }, [config]);

  // Validation function
  const validateForm = (): boolean => {
    const newErrors = {
      ftpHost: '',
      userName: '',
      password: '',
      rootFolderName: '',
      general: '',
    };
    let isValid = true;

    if (!formData.ftpHost.trim()) {
      newErrors.ftpHost = 'FTP Host is required';
      isValid = false;
    } else {
      const urlRegex = /^(ftp|sftp):\/\/[^\s/$.?#].[^\s]*$|^([a-zA-Z0-9-]+\.)*[a-zA-Z0-9-]+\.[a-zA-Z]{2,}$/;
      if (!urlRegex.test(formData.ftpHost)) {
        newErrors.ftpHost = 'Enter a valid FTP host (e.g., ftp.example.com or ftp://example.com)';
        isValid = false;
      }
    }

    if (!formData.userName.trim()) {
      newErrors.userName = 'Username is required';
      isValid = false;
    }

    if (!formData.password.trim()) {
      newErrors.password = 'Password is required';
      isValid = false;
    }

    if (!formData.rootFolderName.trim()) {
      newErrors.rootFolderName = 'Root Folder is required';
      isValid = false;
    } else {
      const folderRegex = /^([a-zA-Z0-9-_]+|[a-zA-Z0-9-_][a-zA-Z0-9-_/]*)$/;
      if (!folderRegex.test(formData.rootFolderName)) {
        newErrors.rootFolderName = 'Enter a valid folder name (e.g., /folder or folder)';
        isValid = false;
      }
    }

    setErrors(newErrors);
    return isValid;
  };

  // Validate individual field on blur
  const validateField = (name: keyof FTPConfig, value: string | number): string | undefined => {
    switch (name) {
      case 'ftpHost':
        if (typeof value !== 'string' || !value.trim()) return 'FTP Host is required';
        const urlRegex = /^(ftp|sftp):\/\/[^\s/$.?#].[^\s]*$|^([a-zA-Z0-9-]+\.)*[a-zA-Z0-9-]+\.[a-zA-Z]{2,}$/;
        if (!urlRegex.test(value)) return 'Enter a valid FTP host (e.g., ftp.example.com or ftp://example.com)';
        return undefined;
      case 'userName':
        return typeof value !== 'string' || value.trim() ? undefined : 'Username is required';
      case 'password':
            return typeof value !== 'string' || value.trim() ? undefined : 'Password is required';
      case 'rootFolderName':
        if (typeof value !== 'string' || !value.trim()) return 'Root Folder is required';
        const folderRegex = /^([a-zA-Z0-9-_]+|[a-zA-Z0-9-_][a-zA-Z0-9-_/]*)$/;
        if (!folderRegex.test(value)) return 'Enter a valid folder name (e.g., /folder or folder)';
        return undefined;
      default:
        return undefined;
    }
  };

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
    setErrors((prev) => ({ ...prev, [name]: '', general: '' }));
  };

  // Validate on blur
  const handleBlur = (event: React.FocusEvent<HTMLInputElement>) => {
    const { name, value } = event.target;
    const error = validateField(name as keyof FTPConfig, value);
    setErrors((prev) => ({ ...prev, [name]: error }));
  };

  // Handle update button click
  const handleUpdate = async () => {
    if (!validateForm()) return;
    setLoading('update');
    try {
      if (onUpdate) {
        await onUpdate(formData);
      }
    } catch (err) {
      console.error('Error updating FTP config:', err);
      setErrors((prev) => ({ ...prev, general: 'Failed to update FTP config' }));
    } finally {
      setLoading(null);
    }
  };

  // Handle test connection button click
  const handleTestConnection = async () => {
    console.log('Test Connection clicked');
    if (!validateForm()) return;
    setLoading('test');
    try {
      if (onTestConnection) {
        await onTestConnection(formData);
      }
    } catch (err) {
      console.error('Error testing FTP connection:', err);
      setErrors((prev) => ({ ...prev, general: 'Failed to test FTP connection' }));
    } finally {
      setLoading(null);
    }
  };

  // Check if form is valid for button enabling
  const isFormValid = Object.values(errors).every((error) => !error) &&
    ['ftpHost', 'userName', 'password', 'rootFolderName'].every(
      (field) => !validateField(field as keyof FTPConfig, formData[field as keyof FTPConfig])
    );

  return (
    <Card sx={{ position: 'relative' }} aria-busy={!!loading}>
      <Divider />
      {loading && (
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(255, 255, 255, 0.7)',
            zIndex: 1,
          }}
        >
          <CircularProgress />
        </Box>
      )}
      <CardContent>
        <Stack spacing={3} sx={{ maxWidth: 'sm' }}>
          <FormControl fullWidth error={!!errors.ftpHost}>
            <InputLabel>Enter URL</InputLabel>
            <OutlinedInput
              label="Enter URL"
              name="ftpHost"
              value={formData.ftpHost}
              onChange={handleInputChange}
              onBlur={handleBlur}
            />
            {errors.ftpHost && <FormHelperText>{errors.ftpHost}</FormHelperText>}
          </FormControl>
          <FormControl fullWidth error={!!errors.userName}>
            <InputLabel>Username</InputLabel>
            <OutlinedInput
              label="Username"
              name="userName"
              value={formData.userName}
              onChange={handleInputChange}
              onBlur={handleBlur}
            />
            {errors.userName && <FormHelperText>{errors.userName}</FormHelperText>}
          </FormControl>
          <FormControl fullWidth error={!!errors.password}>
            <InputLabel htmlFor="outlined-adornment-password">Password</InputLabel>
            <OutlinedInput
              id="outlined-adornment-password"
              type={showPassword ? 'text' : 'password'}
              name="password"
              value={formData.password}
              onChange={handleInputChange}
              onBlur={handleBlur}
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
            {errors.password && <FormHelperText>{errors.password}</FormHelperText>}
          </FormControl>
          <FormControl fullWidth error={!!errors.rootFolderName}>
            <InputLabel>Root Folder</InputLabel>
            <OutlinedInput
              label="Root Folder"
              name="rootFolderName"
              value={formData.rootFolderName}
              onChange={handleInputChange}
              onBlur={handleBlur}
            />
            {errors.rootFolderName && <FormHelperText>{errors.rootFolderName}</FormHelperText>}
          </FormControl>
          {errors.general && (
            <FormHelperText error sx={{ textAlign: 'center' }}>
              {errors.general}
            </FormHelperText>
          )}
        </Stack>
      </CardContent>
      <Divider />
      <CardActions sx={{ justifyContent: 'flex-end' }}>
        <LoadingButton
          variant="contained"
          onClick={handleTestConnection}
          disabled={!isFormValid || loading !== null}
          loading={loading === 'test'}
          loadingIndicator={<CircularProgress color="inherit" size={16} />}
        >
          Test Connection
        </LoadingButton>
        <LoadingButton
          variant="contained"
          onClick={handleUpdate}
          disabled={!isFormValid || loading !== null}
          loading={loading === 'update'}
          loadingIndicator={<CircularProgress color="inherit" size={16} />}
        >
          Update
        </LoadingButton>
      </CardActions>
    </Card>
  );
}

// interface FTPConfig {
//   id: number;
//   ftpHost: string;
//   userName: string;
//   password: string;
//   rootFolderName: string;
// }

// interface FTPConfigProps {
//   config?: FTPConfig; // Single object, not array
//   onUpdate?: (data: FTPConfig) => void; // Updated to expect single object
//   onTestConnection?: (data: FTPConfig) => void; // Updated to expect single object
// }

// //export function FTPDetailsForm(): React.JSX.Element {
// export function FTPDetailsForm({
//   config = { id: 0, ftpHost: '', userName: '', password: '', rootFolderName: '' },
//   onUpdate, onTestConnection
// }: FTPConfigProps): React.JSX.Element {

//   const [formData, setFormData] = useState<FTPConfig>(config);
//   const [showPassword, setShowPassword] = React.useState(false);
//   const [errors, setErrors] = React.useState({
//     ftpHost: '',
//     userName: '',
//     password: '',
//     rootFolderName: '',
//     general: '',
//   });

//   const isInitialMount = useRef(true);

//   // Initialize formData with config on mount or when config changes
//   useEffect(() => {
//     if (isInitialMount.current) {
//       setFormData(config);
//       isInitialMount.current = false;
//     } else if (
//       config.id !== formData.id ||
//       config.ftpHost !== formData.ftpHost ||
//       config.userName !== formData.userName ||
//       config.password !== formData.password ||
//       config.rootFolderName !== formData.rootFolderName
//     ) {
//       // Only update formData if config changes and user hasn't modified form
//       setFormData(config);
//     }
//   }, [config]);



//   const handleClickShowPassword = () => setShowPassword((show) => !show);
//   const handleMouseDownPassword = (event: React.MouseEvent<HTMLButtonElement>) => {
//     event.preventDefault();
//   };

//   const handleMouseUpPassword = (event: React.MouseEvent<HTMLButtonElement>) => {
//     event.preventDefault();
//   };

//   // Handle input changes
//   const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
//     const { name, value } = event.target;
//     setFormData((prev) => ({ ...prev, [name]: value }));
//     setErrors((prev) => ({ ...prev, [name]: '', general: ''  }));
//   };

//   const validateForm = (): boolean => {
//     const newErrors = {
//       ftpHost: '',
//       userName: '',
//       password: '',
//       rootFolderName: '',
//        general: '',
//     };
//     let isValid = true;

//     if (!formData.ftpHost.trim()) {
//       newErrors.ftpHost = 'FTP Host is required';
//       isValid = false;
//     }

//     if (!formData.userName.trim()) {
//       newErrors.userName = 'Username is required';
//       isValid = false;
//     }

//     if (!formData.password.trim()) {
//       newErrors.password = 'Consumer number is required';
//       isValid = false;
//     }

//     if (!formData.rootFolderName.trim()) {
//       newErrors.rootFolderName = 'FTP folder is required';
//       isValid = false;
//     }
//     setErrors(newErrors);
//     return isValid;
//   };

//   // Handle update button click
//   const handleUpdate = () => {
//     if (onUpdate) {
//       if (!validateForm()) return;
//       onUpdate(formData);
//     }
//   };

//   // Placeholder for Test Connection
//   const handleTestConnection = () => {
//     console.log('Test Connection clicked'); // Placeholder
//     if (!validateForm()) return;
//     onTestConnection(formData);
//   };
//   return (
//     <Card>
//       <Divider />
//       <CardContent>
//         <Stack spacing={3} sx={{ maxWidth: 'sm' }}>
//           <FormControl fullWidth error={!!errors.ftpHost}>
//             <InputLabel>Enter URL</InputLabel>
//             <OutlinedInput
//               label="Enter URL"
//               name="ftpHost"
//               value={formData.ftpHost}
//               onChange={handleInputChange}
//             />
//              {errors.ftpHost && <FormHelperText>{errors.ftpHost}</FormHelperText>}
//           </FormControl>
//           <FormControl fullWidth>
//             <InputLabel>Username</InputLabel>
//             <OutlinedInput
//               label="Username"
//               name="userName"
//               value={formData.userName}
//               onChange={handleInputChange}
//             />
//           </FormControl>
//            <FormControl fullWidth error={!!errors.password}>
//             <InputLabel htmlFor="outlined-adornment-password">Password</InputLabel>
//             <OutlinedInput
//               id="outlined-adornment-password"
//               type={showPassword ? 'text' : 'password'}
//               name="password"
//               value={formData.password}
//               onChange={handleInputChange}
//               endAdornment={
//                 <InputAdornment position="end">
//                   <IconButton
//                     aria-label={showPassword ? 'hide the password' : 'display the password'}
//                     onClick={handleClickShowPassword}
//                     onMouseDown={handleMouseDownPassword}
//                     onMouseUp={handleMouseUpPassword}
//                     edge="end"
//                   >
//                     {showPassword ? <VisibilityOff /> : <Visibility />}
//                   </IconButton>
//                 </InputAdornment>
//               }
//               label="Password"
//             />
//             {errors.password && <FormHelperText>{errors.password}</FormHelperText>}
//           </FormControl>
//           <FormControl fullWidth error={!!errors.rootFolderName}>
//             <InputLabel>Root Folder</InputLabel>
//             <OutlinedInput
//               label="Root Folder"
//               name="rootFolderName"
//               value={formData.rootFolderName}
//               onChange={handleInputChange}
//             />
//             {errors.rootFolderName && <FormHelperText>{errors.rootFolderName}</FormHelperText>}
//           </FormControl>
//         </Stack>
//       </CardContent>
//       <Divider />
//       <CardActions sx={{ justifyContent: 'flex-end' }}>
//         <Button variant="contained" onClick={handleTestConnection}>
//           Test Connection
//         </Button>
//         <Button variant="contained" onClick={handleUpdate}>
//           Update
//         </Button>
//       </CardActions>
//     </Card>
//   );
// }
