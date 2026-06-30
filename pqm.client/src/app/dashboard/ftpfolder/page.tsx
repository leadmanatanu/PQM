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
import Card from '@mui/material/Card';
import Select from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import Button from '@mui/material/Button';

import { config } from '@/config';
import { FTPDetailsForm } from '@/components/dashboard/ftpfolder/ftp-details';
import type { FTPConfig } from '@/components/dashboard/ftpfolder/ftp-details';

import { fetchFtpDetails, updateFtpDetails, testFtpDetails, fetchDevices, importLocalCsvFiles } from '../../../api/device';

export default function Page(): React.JSX.Element {
  const [loading, setLoading] = React.useState(true);
  const [ftpConfig, setFtpConfig] = useState<FTPConfig>({
    id: 0,
    ftpHost: '',
    userName: '',
    password: '',
    rootFolderName: '',
  });
  const [devices, setDevices] = useState<any[]>([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | number>('');
  const [importing, setImporting] = useState(false);
  const [openSnackbar, setOpenSnackbar] = useState(false);
  const [testResult, setTestResult] = useState<string | null>(null);

  useEffect(() => {
    const loadFTP = async () => {
      try {
        const fetchedFTPConfig = await fetchFtpDetails();
        setFtpConfig(fetchedFTPConfig);
        console.log('Fetched FTP:', fetchedFTPConfig);

        const fetchedDevices = await fetchDevices();
        setDevices(fetchedDevices ?? []);
        if (fetchedDevices && fetchedDevices.length > 0) {
          setSelectedDeviceId(fetchedDevices[0].id);
        }

        setLoading(false);
      } catch (error) {
        console.error('Failed to fetch details:', error);
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

  const handleImportCSV = useCallback(async () => {
    if (!selectedDeviceId) return;
    setImporting(true);
    try {
      const result = await importLocalCsvFiles(selectedDeviceId);
      if (result && result.status) {
        setTestResult(result.data);
      } else {
        setTestResult(result?.errors?.[0] || 'Import failed');
      }
      setOpenSnackbar(true);
    } catch (err) {
      console.error('Failed to import local CSV:', err);
      setTestResult('Import failed due to an error');
      setOpenSnackbar(true);
    } finally {
      setImporting(false);
    }
  }, [selectedDeviceId]);

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
          <FTPDetailsForm
            config={memoizedConfig}
            onUpdate={handleUpdate}
            onTestConnection={handleTestConnection}
          />
          <Card sx={{ p: 3 }}>
            <Stack spacing={2}>
              <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                Local CSV File Import
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Scan and import meter readings and event logs from local CSV files placed inside the server's <code>CSVFiles/</code> folder.
              </Typography>
              <Stack direction="row" spacing={2} alignItems="center">
                <FormControl size="small" sx={{ minWidth: 250 }}>
                  <InputLabel id="select-device-label">Select Device</InputLabel>
                  <Select
                    labelId="select-device-label"
                    value={selectedDeviceId}
                    label="Select Device"
                    onChange={(e) => setSelectedDeviceId(e.target.value)}
                  >
                    {devices.map((dev: any) => (
                      <MenuItem key={dev.id} value={dev.id}>
                        {dev.name} ({dev.ip})
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <Button
                  variant="contained"
                  color="secondary"
                  onClick={handleImportCSV}
                  disabled={!selectedDeviceId || importing}
                  sx={{ height: 40 }}
                >
                  {importing ? <CircularProgress size={20} color="inherit" /> : 'Import CSV Files'}
                </Button>
              </Stack>
            </Stack>
          </Card>
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
