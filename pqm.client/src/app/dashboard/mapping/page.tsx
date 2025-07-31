'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import type { Metadata } from 'next';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { config } from '@/config';
import { DeviceParameter } from '@/components/dashboard/mapping/device-paramter';
import { UpdatePasswordForm } from '@/components/dashboard/mapping/update-password-form';
import { DeviceFilters } from '@/components/dashboard/mapping/device-selection';
import Snackbar, { SnackbarCloseReason } from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import { fetchDevices, fetchDeviceParameter, updateDeviceParamMapping } from '../../../api/device';

//export const metadata = { title: `Device Mapping | Dashboard | ${config.site.name}` } satisfies Metadata;

export default function Page(): React.JSX.Element {
  const [devices, setDevices] = useState<Device[]>([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);
  const [devParamArr, setDevParamArr] = useState([]);
const [openSnackbar, setOpenSnackbar] = useState(false);
const [displayMsg, setDisplayMsg] = useState<string | null>(null);
  useEffect(() => {
    const loadDevices = async () => {
      try {
        const fetchedDevices = await fetchDevices();
        setDevices(fetchedDevices);
        console.log('Fetched devices:', fetchedDevices[0]);
      } catch (error) {
        console.error('Failed to fetch devices:', error);
      }
    };
    loadDevices();
  }, []);

  // Log devParamArr when it updates
  useEffect(() => {
    if (devParamArr.length > 0) {
      console.log('devParamArr updated:', devParamArr);
    }
  }, [devParamArr]);

  const handleDeviceSelection = (id: string | number) => {
    setSelectedDeviceId(id);
    console.log('Device selected:', id);
    fetchDeviceParameter(id)
      .then((fetchedDeviceParameter) => {
        setDevParamArr(fetchedDeviceParameter.data);
        console.log('Fetched devices param:', fetchedDeviceParameter);
      })
      .catch((error) => {
        console.error('Failed to fetch devices:', error);
      });
  };

  const handleDeviceUpdate = (updatedDevice: unknown[]) => {
    setDevParamArr(updatedDevice);
    // need to create json 
    const selParamsTmp = updatedDevice
      .filter((t) => t.isSelected)
      .map((t) => ({
        DeviceId: selectedDeviceId,
        ParameterId: t.id,
      }));
    console.log('Updated device parameters:', selParamsTmp);
    updateDeviceParamMapping(selParamsTmp)
      .then((data) => {
        console.log('handleDeviceUpdate:', data);
        setDisplayMsg('Update successful');
        setOpenSnackbar(true);
      })
      .catch((error) => {
        setDisplayMsg('Update failed');
        console.error('Failed to update devices:', error);
      });
  };

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
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">Device Mapping</Typography>
      </div>
      <DeviceFilters
        rows={devices}
        onDeviceSelect={handleDeviceSelection}
      />
      {<DeviceParameter device={devParamArr} onDeviceUpdate={handleDeviceUpdate} />}
      {/*<UpdatePasswordForm />*/}
      <Snackbar open={openSnackbar} autoHideDuration={6000} onClose={handleSnackBarClose} anchorOrigin={{ vertical: 'top', horizontal: 'center' }}>
              <Alert
                severity={displayMsg?.includes('successful') ? 'success' : 'error'}
                sx={{ width: '100%' }}
                onClose={handleSnackBarClose}
                variant="filled"
              >
                {displayMsg}
              </Alert>
            </Snackbar>
    </Stack>
  );
}
