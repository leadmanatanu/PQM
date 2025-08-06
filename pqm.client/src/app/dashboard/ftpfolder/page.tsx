'use client';

import * as React from 'react';
import { useState, useEffect, useMemo, useCallback } from 'react';
import type { Metadata } from 'next';
import Stack from '@mui/material/Stack';
import Snackbar, { SnackbarCloseReason } from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import Typography from '@mui/material/Typography';
import CircularProgress from '@mui/material/CircularProgress';
import Box from '@mui/material/Box';

import { config } from '@/config';
//import { DeviceParameter } from '@/components/dashboard/mapping/device-paramter';
import { FTPDetailsForm } from '@/components/dashboard/ftpfolder/ftp-details';
import type { FTPConfig } from '@/components/dashboard/ftpfolder/ftp-details';
//import { DeviceFilters } from '@/components/dashboard/mapping/device-selection';

import { fetchFtpDetails, updateFtpDetails, testFtpDetails } from '../../../api/device';

//export const metadata = { title: `Device Mapping | Dashboard | ${config.site.name}` } satisfies Metadata;

export default function Page(): React.JSX.Element {
  const [loading, setLoading] = React.useState(true);
  const [ftpConfig, setFtpConfig] = useState<FTPConfig>({
    id: 0,
    ftpHost: '',
    userName: '',
    password: '',
    rootFolderName: '',
  });
  const [openSnackbar, setOpenSnackbar] = useState(false);
  const [testResult, setTestResult] = useState<string | null>(null);
  useEffect(() => {
    const loadFTP = async () => {
      try {
        const fetchedFTPConfig = await fetchFtpDetails();
        setFtpConfig(fetchedFTPConfig);
        console.log('Fetched FTP:', fetchedFTPConfig);
        setLoading(false);
      } catch (error) {
        console.error('Failed to fetch devices:', error);
      }
    };
    loadFTP();
  }, []);




  // Memoize config and deviceLogArr to ensure stable references
  const memoizedConfig = useMemo(() => ftpConfig, [ftpConfig]);

  // Memoize onUpdate to avoid re-renders
  // const handleUpdate = useCallback((updatedConfig: FTPConfig) => {
  //   setFtpConfig(updatedConfig);
  //   console.log('Updated config:', updatedConfig);
  // }, []);
  const handleUpdate = useCallback(async (updatedConfig: any) => {
    try {
      const result = await updateFtpDetails(updatedConfig);
      if (result) {
        setFtpConfig(result);
        console.log('Updated config:', result);
        setTestResult('Updated successfully');
        setOpenSnackbar(true);
      } else {
        console.error('Update failed: No result returned');
      }
    } catch (error) {
      console.error('Error updating config:', error);
    }
  }, []);

  const handleTestConnection = useCallback(async (updatedConfig: any) => {
    try {
      const result = await testFtpDetails(updatedConfig);
      if (result) {
        //setFtpConfig(result);
        if (result.status == true) {
          setTestResult('Connection successful');
        } else {
          setTestResult('Connection failed');
        }
        setOpenSnackbar(true);
        console.log('Test :', result);
      } else {
        console.error('Test failed: No result returned');
      }
    } catch (error) {
      console.error('Error test config:', error);
    }
  }, []);

  const handleSnackBarClose = (
    event?: React.SyntheticEvent | Event,
    reason?: SnackbarCloseReason,
  ) => {
    if (reason === 'clickaway') {
      return;
    }

    setOpenSnackbar(false);
  };

  return (
    <div>
      {loading ? (
        <Box sx={{
          display: 'flex',
          justifyContent: 'center', // Center horizontally
          alignItems: 'center', // Center vertically
          minHeight: '100px', // Optional: Ensure the Box has height for visibility
        }}>
          <CircularProgress />
        </Box>
      ) : (
        <Stack spacing={3}>
          <div>
            <Typography variant="h4">FTP Folder</Typography>
          </div>
          {<FTPDetailsForm
            config={memoizedConfig}
            onUpdate={handleUpdate}
            onTestConnection={handleTestConnection}
          />}
          <Snackbar open={openSnackbar} autoHideDuration={6000} onClose={handleSnackBarClose} anchorOrigin={{ vertical: 'top', horizontal: 'center' }}>
            {/*<Alert
          onClose={handleSnackBarClose}
          severity="success"
          variant="filled"
          sx={{ width: '100%' }}
        >
          This is a success Alert inside a Snackbar!
        </Alert> */}

            <Alert
              severity={testResult?.includes('successful') ? 'success' : 'error'}
              sx={{ width: '100%' }}
              onClose={handleSnackBarClose}
              variant="filled"
            >
              {testResult}
            </Alert>
          </Snackbar>
        </Stack>
      )}
    </div>
  );
}
