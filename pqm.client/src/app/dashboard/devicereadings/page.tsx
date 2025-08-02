'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import type { Metadata } from 'next';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { config } from '@/config';
import { DeviceParameter } from '@/components/dashboard/devicereadings/device-paramter';
import { UpdatePasswordForm } from '@/components/dashboard/devicereadings/update-password-form';
import { DeviceFilters } from '@/components/dashboard/devicereadings/device-selection';

import { fetchDevices, fetchDeviceParameter } from '../../../api/device';

//export const metadata = { title: `Device Mapping | Dashboard | ${config.site.name}` } satisfies Metadata;

export default function Page(): React.JSX.Element {
  const [devices, setDevices] = useState<Device[]>([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);
  const [devParamArr,setDevParamArr] = useState([]);

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
    console.log('Updated device parameters:', updatedDevice);
  };

  return (
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">Device Readings</Typography>
      </div>
      <DeviceFilters
        rows={devices}
        onDeviceSelect={handleDeviceSelection}
      />
      {/*<DeviceParameter device={devParamArr} onDeviceUpdate={handleDeviceUpdate} /> */}
      {/*<UpdatePasswordForm />*/ }
    </Stack>
  );
}
