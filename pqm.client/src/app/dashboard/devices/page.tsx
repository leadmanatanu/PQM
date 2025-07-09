import * as React from 'react';
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


export const metadata = { title: `Devices | Dashboard | ${config.site.name}` } satisfies Metadata;

export default async function Page(): React.JSX.Element {
    let isVisible = true;
    const page = 0;
    const rowsPerPage = 10;
    const devices = await fetchDevices() satisfies Device[];
    //console.log(devices);
    const totalRows = devices.length;
    console.log(totalRows);

    const paginatedDevices = applyPagination(devices, page, rowsPerPage);

    return (
        <Stack spacing={3}>
            <Stack>
                <Stack direction="row" spacing={3}>
                    <Stack spacing={1} sx={{ flex: '1 1 auto' }}>
                        <Typography variant="h4">Devices</Typography>
                    </Stack>
                    <div>
                        <Button startIcon={<PlusIcon fontSize="var(--icon-fontSize-md)" />} variant="contained">
                            Add
                        </Button>
                    </div>
                </Stack>
                <DevicesFilters show={isVisible} />
                <DevicesTable show={isVisible}
                    count={totalRows}
                    page={page}
                    rows={paginatedDevices}
                    rowsPerPage={rowsPerPage}
                />
            </Stack>
            <Stack>
                <AddDeviceForm show={!isVisible} />
            </Stack>
        </Stack>
    );
}

function applyPagination(rows: Device[], page: number, rowsPerPage: number): Device[] {
    return rows.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
}
