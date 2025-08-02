"use client";

import * as React from 'react';
import { useState, useEffect } from 'react';
import type { Metadata } from 'next';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import { DownloadIcon } from '@phosphor-icons/react/dist/ssr/Download';
import { PlusIcon } from '@phosphor-icons/react/dist/ssr/Plus';
import { UploadIcon } from '@phosphor-icons/react/dist/ssr/Upload';
import dayjs from 'dayjs';
import { config } from '@/config';
import { DevicesFilters } from '@/components/dashboard/device/devices-filters';
import { DevicesTable } from '@/components/dashboard/device/devices-table';
import { AddDeviceForm } from '@/components/dashboard/device/add-device-form';
import type { Device } from '@/components/dashboard/device/devices-table';
import { fetchDevices } from '../../../api/device'
import * as XLSX from 'xlsx';

//export const metadata = { title: `Devices | Dashboard | ${config.site.name}` } satisfies Metadata;

export default function Page(): React.JSX.Element {
    const [isVisible, setIsVisible] = useState(true);
    const [devices, setDevices] = useState<Device[]>([]);
    const [editingDevice, setEditingDevice] = useState<Device | null>(null);
    const page = 0;
    const rowsPerPage = 10;

    useEffect(() => {
        const loadDevices = async () => {
            const fetchedDevices = await fetchDevices();
            setDevices(fetchedDevices);
        };
        loadDevices();
    }, []);

    const totalRows = devices.length;
    const paginatedDevices = applyPagination(devices, page, rowsPerPage);

    // const toggleVisibility = () => {
    //     setIsVisible((prev) => !prev);
    // };
    const toggleVisibility = (device: Device | null = null) => {
        setIsVisible((prev) => !prev);
        // setEditingDevice(device);
    };

    const handleEdit = (deviceId: string) => {
        const device = devices.find((d) => d.id === deviceId) || null;
        toggleVisibility(device);
        setEditingDevice(device);
    };

    const handleExport = () => {
        // Map devices to a format suitable for Excel
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

        // Create a new workbook and worksheet
        const worksheet = XLSX.utils.json_to_sheet(data);
        const workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, worksheet, 'Devices');

        // Generate and download the Excel file
        XLSX.writeFile(workbook, 'devices.xlsx');
    };

    return (
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
                                    onClick={toggleVisibility}
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
    );
}

// export default async function Page(): React.JSX.Element {
//     let isVisible = true;
//     const page = 0;
//     const rowsPerPage = 10;
//     const devices = await fetchDevices() satisfies Device[];
//     //console.log(devices);
//     const totalRows = devices.length;
//     console.log(totalRows);

//     const paginatedDevices = applyPagination(devices, page, rowsPerPage);

//     return (
//         <Stack spacing={3}>
//             <Stack>
//                 <Stack direction="row" spacing={3}>
//                     <Stack spacing={1} sx={{ flex: '1 1 auto' }}>
//                         <Typography variant="h4">Devices</Typography>
//                     </Stack>
//                     <div>
//                         <Button startIcon={<PlusIcon fontSize="var(--icon-fontSize-md)" />} variant="contained">
//                             Add
//                         </Button>
//                     </div>
//                 </Stack>
//                 <DevicesFilters show={isVisible} />
//                 <DevicesTable show={isVisible}
//                     count={totalRows}
//                     page={page}
//                     rows={paginatedDevices}
//                     rowsPerPage={rowsPerPage}
//                 />
//             </Stack>
//             <Stack>
//                 <AddDeviceForm show={!isVisible} />
//             </Stack>
//         </Stack>
//     );
// }

function applyPagination(rows: Device[], page: number, rowsPerPage: number): Device[] {
    return rows.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
}
