"use client";

import * as React from 'react';
import { useState, useEffect } from 'react';
import type { Metadata } from 'next';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import CircularProgress from '@mui/material/CircularProgress';
import Box from '@mui/material/Box';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogTitle from '@mui/material/DialogTitle';
import Snackbar from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import { DownloadIcon } from '@phosphor-icons/react/dist/ssr/Download';
import { PlusIcon } from '@phosphor-icons/react/dist/ssr/Plus';
import { UploadIcon } from '@phosphor-icons/react/dist/ssr/Upload';
import dayjs from 'dayjs';
import { config } from '@/config';
import { DevicesFilters } from '@/components/dashboard/device/devices-filters';
import { DevicesTable } from '@/components/dashboard/device/devices-table';
import { AddDeviceForm } from '@/components/dashboard/device/add-device-form';
import type { Device } from '@/components/dashboard/device/devices-table';
import { fetchDevices, deleteDevice } from '../../../api/device'
import * as XLSX from 'xlsx';

function applyPagination(rows: Device[], page: number, rowsPerPage: number): Device[] {
    return rows.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
}

export default function Page(): React.JSX.Element {
  const [isVisible, setIsVisible] = useState(true);
  const [devices, setDevices] = useState<Device[]>([]);
  const [editingDevice, setEditingDevice] = useState<Device | null>(null);
  const [loading, setLoading] = useState<'fetch' | 'delete' | null>('fetch');
  const [deleteDeviceId, setDeleteDeviceId] = useState<string | null>(null);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');
  const [snackbarSeverity, setSnackbarSeverity] = useState<'success' | 'error'>('success');
  const page = 0;
  const rowsPerPage = 10;

  useEffect(() => {
    const loadDevices = async () => {
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
    };
    loadDevices();
  }, []);

  const totalRows = devices.length;
  const paginatedDevices = applyPagination(devices, page, rowsPerPage);

  const toggleVisibility = (device: Device | null = null) => {
    setIsVisible((prev) => !prev);
    setEditingDevice(device);
  };

  const handleEdit = (deviceId: string) => {
    const device = devices.find((d) => d.id === deviceId) || null;
    toggleVisibility(device);
    setEditingDevice(device);
  };

  const handleDelete = (deviceId: string) => {
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
      'Serial No': device.serialNo,
      'Consumer No': device.consumerNo,
      'FTP Folder': device.ftpFolder,
      Status: device.isActive ? 'Active' : 'Inactive',
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
        <Stack>
          <Stack direction="row" spacing={3}>
            <Stack spacing={1} sx={{ flex: '1 1 auto' }}>
              <Typography variant="h4">Devices</Typography>
            </Stack>
            {isVisible && (
              <>
                <div>
                  <Button
                    startIcon={<PlusIcon fontSize="var(--icon-fontSize-md)" />}
                    variant="contained"
                    onClick={() => toggleVisibility(null)}
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
              </>
            )}
          </Stack>
          <DevicesFilters show={isVisible} />
          <DevicesTable
            show={isVisible}
            count={totalRows}
            page={page}
            rows={paginatedDevices}
            rowsPerPage={rowsPerPage}
            onEdit={handleEdit}
            onDelete={handleDelete}
          />
        </Stack>
        <Stack>
          <AddDeviceForm
            show={!isVisible}
            onToggleVisibility={toggleVisibility}
            editingDevice={editingDevice}
            setEditingDevice={setEditingDevice}
          />
        </Stack>
      </Stack>
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
//export const metadata = { title: `Devices | Dashboard | ${config.site.name}` } satisfies Metadata;

// export default function Page(): React.JSX.Element {
//     const [isVisible, setIsVisible] = useState(true);
//     const [devices, setDevices] = useState<Device[]>([]);
//     const [editingDevice, setEditingDevice] = useState<Device | null>(null);
//     const page = 0;
//     const rowsPerPage = 10;

//     useEffect(() => {
//         const loadDevices = async () => {
//             const fetchedDevices = await fetchDevices();
//             setDevices(fetchedDevices);
//         };
//         loadDevices();
//     }, []);

//     const totalRows = devices.length;
//     const paginatedDevices = applyPagination(devices, page, rowsPerPage);

//     // const toggleVisibility = () => {
//     //     setIsVisible((prev) => !prev);
//     // };
//     const toggleVisibility = (device: Device | null = null) => {
//         setIsVisible((prev) => !prev);
//         // setEditingDevice(device);
//     };

//     const handleEdit = (deviceId: string) => {
//         const device = devices.find((d) => d.id === deviceId) || null;
//         toggleVisibility(device);
//         setEditingDevice(device);
//     };

//     const handleDelete = async(deviceId: string) => {
//         const device = devices.find((d) => d.id === deviceId) || null;
//         // toggleVisibility(device);
//         // setEditingDevice(device);
//         const response = await deleteDevice(device);
//     };

//     const handleExport = () => {
//         // Map devices to a format suitable for Excel
//         const data = devices.map(device => ({
//             ID: device.id,
//             Name: device.name,
//             'Serial No': device.serialNo,
//             'Consumer No': device.consumerNo,
//             'FTP Folder': device.ftpFolder,
//             Status: device.isActive ? 'Active' : 'Inactive',
//             IP: device.ip,
//             Port: device.port,
//             'Created Date': device.createdDate || '',
//         }));

//         // Create a new workbook and worksheet
//         const worksheet = XLSX.utils.json_to_sheet(data);
//         const workbook = XLSX.utils.book_new();
//         XLSX.utils.book_append_sheet(workbook, worksheet, 'Devices');

//         // Generate and download the Excel file
//         XLSX.writeFile(workbook, 'devices.xlsx');
//     };

//     return (
//         <Stack spacing={3}>
//             <Stack>
//                 <Stack direction="row" spacing={3}>
//                     <Stack spacing={1} sx={{ flex: '1 1 auto' }}>
//                         <Typography variant="h4">Devices</Typography>
//                     </Stack>
//                     {isVisible && (
//                         <>
//                             <div>
//                                 <Button
//                                     startIcon={<PlusIcon fontSize="var(--icon-fontSize-md)" />}
//                                     variant="contained"
//                                     onClick={toggleVisibility}
//                                 >
//                                     Add
//                                 </Button>
//                             </div>
//                             <div>
//                                 <Button
//                                     startIcon={<DownloadIcon fontSize="var(--icon-fontSize-md)" />}
//                                     variant="contained"
//                                     onClick={handleExport}
//                                 >
//                                     Export
//                                 </Button>
//                             </div>
//                         </>
//                     )}
//                 </Stack>
//                 <DevicesFilters show={isVisible} />
//                 <DevicesTable
//                     show={isVisible}
//                     count={totalRows}
//                     page={page}
//                     rows={paginatedDevices}
//                     rowsPerPage={rowsPerPage}
//                     onEdit={handleEdit}
//                     onDelete={handleDelete}
//                 />
//             </Stack>
//             <Stack>
//                 <AddDeviceForm
//                     show={!isVisible}
//                     onToggleVisibility={toggleVisibility}
//                     editingDevice={editingDevice}
//                     setEditingDevice={setEditingDevice}
//                 />
//             </Stack>
//         </Stack>
//     );
// }


