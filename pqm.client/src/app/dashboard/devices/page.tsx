'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { Plus as PlusIcon } from '@phosphor-icons/react/dist/ssr/Plus';
import { Download as DownloadIcon } from '@phosphor-icons/react/dist/ssr/Download';
import { CircularProgress, Box, Snackbar, Alert, Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions } from '@mui/material';
import * as XLSX from 'xlsx';

import { DevicesFilters } from '@/components/dashboard/device/devices-filters';
import { DevicesTable } from '@/components/dashboard/device/devices-table';
import type { Device } from '@/components/dashboard/device/devices-table';
import { DevicePropertiesDialog } from '@/components/dashboard/device/device-properties-dialog';
import { fetchDevices, deleteDevice, fetchDeviceById, editDevice, connectDevice, disconnectDevice } from '../../../api/device';

function applyPagination(rows: Device[], page: number, rowsPerPage: number): Device[] {
  return rows.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
}

export default function Page(): React.JSX.Element {
  const [devices, setDevices] = useState<Device[]>([]);
  const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
  const [propertiesOpen, setPropertiesOpen] = useState(false);
  const [loading, setLoading] = useState<'fetch' | 'delete' | null>(null);
  const [deleteDeviceId, setDeleteDeviceId] = useState<number | null>(null);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState<'success' | 'error'>('success');
  const page = 0;
  const rowsPerPage = 10;

  const loadDevices = React.useCallback(async () => {
    setLoading('fetch');
    try {
      const fetchedDevices = await fetchDevices();
      setDevices(fetchedDevices);
    } catch (error) {
      console.error('Failed to fetch devices:', error);
      setSnackbarMessage('Failed to fetch devices');
      setSnackbarSeverity('error');
      setSnackbarOpen(true);
    } finally {
      setLoading(null);
    }
  }, []);

  useEffect(() => {
    loadDevices();
  }, [loadDevices]);

  const totalRows = devices.length;
  const paginatedDevices = applyPagination(devices, page, rowsPerPage);

  const handlePropertiesClick = async (device: Device) => {
    setLoading('fetch');
    try {
      const freshDevice = await fetchDeviceById(device.id);
      if (freshDevice) {
        setSelectedDevice(freshDevice);
      } else {
        setSelectedDevice(device);
      }
      setPropertiesOpen(true);
    } catch (error) {
      console.error('Failed to fetch device details:', error);
      setSelectedDevice(device);
      setPropertiesOpen(true);
    } finally {
      setLoading(null);
    }
  };

  const handleSaveProperties = async (updatedDevice: Device) => {
    setLoading('fetch');
    try {
      if (updatedDevice.id > 0) {
        const result = await editDevice(updatedDevice);
        if (result && !result.status) {
          throw new Error(result.errors || 'Failed to update properties');
        }
        setSnackbarMessage('Properties updated successfully');
        setSnackbarSeverity('success');
      } else {
        setSnackbarMessage('Device created successfully');
        setSnackbarSeverity('success');
      }
      await loadDevices();
    } catch (error: any) {
      console.error('Failed to save properties:', error);
      setSnackbarMessage(error.message || 'Failed to save properties');
      setSnackbarSeverity('error');
    } finally {
      setLoading(null);
      setSnackbarOpen(true);
    }
  };

  const handleConnectToggle = async (device: Device) => {
    setLoading('fetch');
    try {
      let result;
      if (device.isConnected) {
        result = await disconnectDevice(device.id);
        if (result && result.status) {
          setSnackbarMessage(`Disconnected from device ${device.name}`);
          setSnackbarSeverity('success');
        } else {
          throw new Error(result?.errors?.join(', ') || 'Failed to disconnect');
        }
      } else {
        result = await connectDevice(device.id);
        if (result && result.status) {
          setSnackbarMessage(`Connected to device ${device.name} successfully`);
          setSnackbarSeverity('success');
        } else {
          throw new Error(result?.errors?.join(', ') || 'Failed to connect');
        }
      }
      await loadDevices();
    } catch (error: any) {
      console.error('Handshake error:', error);
      setSnackbarMessage(error.message || 'Handshake failed.');
      setSnackbarSeverity('error');
    } finally {
      setLoading(null);
      setSnackbarOpen(true);
    }
  };

  const handleDelete = (deviceId: number) => {
    setDeleteDeviceId(deviceId);
  };

  const confirmDelete = async () => {
    if (!deleteDeviceId) return;
    setLoading('delete');
    try {
      const device = devices.find((d) => d.id === deleteDeviceId) || null;
      if (device) {
        await deleteDevice(device);
        setDevices((prev) => prev.filter((d) => d.id !== deleteDeviceId));
        setSnackbarMessage('Device deleted successfully');
        setSnackbarSeverity('success');
      } else {
        throw new Error('Device not found');
      }
    } catch (error) {
      console.error('Failed to delete device:', error);
      setSnackbarMessage('Failed to delete device');
      setSnackbarSeverity('error');
    } finally {
      setLoading(null);
      setDeleteDeviceId(null);
      setSnackbarOpen(true);
    }
  };

  const handleCancelDelete = () => {
    setDeleteDeviceId(null);
  };

  const handleSnackbarClose = () => {
    setSnackbarOpen(false);
    setSnackbarMessage('');
  };

  const handleExport = () => {
    const data = devices.map(device => ({
      ID: device.id,
      Name: device.name,
      'Serial No': device.serialNumber,
      'Account No': device.consumerNumber,
      Status: device.isConnected ? 'Connected' : 'Disconnected',
      IP: device.ip,
      Port: device.port,
      'Created Date': device.createdDate || '',
    }));

    const worksheet = XLSX.utils.json_to_sheet(data);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Devices');
    XLSX.writeFile(workbook, 'devices.xlsx');
  };

  return (
    <div aria-busy={!!loading}>
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
      <Stack spacing={3}>
        <Stack spacing={3}>
          <Stack direction="row" spacing={3}>
            <Stack spacing={1} sx={{ flex: '1 1 auto' }}>
              <Typography variant="h4">Devices</Typography>
            </Stack>
            <div>
              <Button
                startIcon={<PlusIcon fontSize="var(--icon-fontSize-md)" />}
                variant="contained"
                onClick={() => {
                  setSelectedDevice(null);
                  setPropertiesOpen(true);
                }}
              >
                Add
              </Button>
            </div>
            <div>
              <Button
                startIcon={<DownloadIcon fontSize="var(--icon-fontSize-md)" />}
                variant="contained"
                onClick={handleExport}
              >
                Export
              </Button>
            </div>
          </Stack>
          <DevicesFilters show={true} />
          <DevicesTable
            show={true}
            count={totalRows}
            page={page}
            rows={paginatedDevices}
            rowsPerPage={rowsPerPage}
            onPropertiesClick={handlePropertiesClick}
            onDelete={handleDelete}
            onConnectToggle={handleConnectToggle}
          />
        </Stack>
      </Stack>

      <DevicePropertiesDialog
        open={propertiesOpen}
        onClose={() => setPropertiesOpen(false)}
        device={selectedDevice}
        onSave={handleSaveProperties}
      />

      <Dialog
        open={!!deleteDeviceId}
        onClose={handleCancelDelete}
        aria-labelledby="delete-dialog-title"
        aria-describedby="delete-dialog-description"
      >
        <DialogTitle id="delete-dialog-title">Confirm Delete</DialogTitle>
        <DialogContent>
          <DialogContentText id="delete-dialog-description">
            Are you sure you want to delete this device? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCancelDelete}>Cancel</Button>
          <Button onClick={confirmDelete} color="error" variant="contained">
            Confirm
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={snackbarOpen}
        autoHideDuration={6000}
        onClose={handleSnackbarClose}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert
          severity={snackbarSeverity}
          sx={{ width: '100%' }}
          onClose={handleSnackbarClose}
          variant="filled"
        >
          {snackbarMessage}
        </Alert>
      </Snackbar>
    </div>
  );
}
