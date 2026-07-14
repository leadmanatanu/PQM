'use client';

import * as React from 'react';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Divider from '@mui/material/Divider';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import dayjs from 'dayjs';

export interface Device {
    id: number;
    name: string;
    ip: string;
    port: number;
    isActive: string | boolean;
    isDeleted?: string;
    createdDate?: Date;
    createdId?: number;
    modifiedDate?: Date;
    modifiedId?: number;
    serialNumber: string;
    consumerNumber: string;
    lastSync?: Date;
    connectionSettings?: string;
    isConnected?: boolean;
}

interface DevicesTableProps {
    count?: number;
    page?: number;
    rows?: Device[];
    rowsPerPage?: number;
    show?: boolean;
    onPropertiesClick: (device: Device) => void;
    onDelete: (deviceId: number) => void;
    onConnectToggle: (device: Device) => Promise<void>;
}

export function DevicesTable({
    count = 0,
    rows = [],
    page = 0,
    rowsPerPage = 0,
    show = true,
    onPropertiesClick,
    onDelete,
    onConnectToggle,
}: DevicesTableProps): React.JSX.Element | null {
    if (!show) return null;

    const handleChangePage = (
        event: React.MouseEvent<HTMLButtonElement> | null,
        newPage: number
    ) => {
        // Handled by parent if pagination is active
    };

    const handleChangeRowsPerPage = (
        event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
    ) => {
        // Handled by parent if pagination is active
    };

    const handleDeleteClick = (deviceId: number) => {
        onDelete(deviceId);
    };

    return (
        <Card sx={{ borderRadius: '8px' }}>
            <Box sx={{ overflowX: 'auto', maxHeight: '350px', overflowY: 'auto' }}>
                <Table size="small" sx={{ minWidth: '800px' }}>
                    <TableHead sx={{ bgcolor: 'var(--mui-palette-neutral-50)' }}>
                        <TableRow>
                            <TableCell sx={{ fontWeight: 600 }}>Id</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Name</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Serial No</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Account No</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>IP</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>PORT</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Created Date</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Last Sync</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Status</TableCell>
                            <TableCell sx={{ fontWeight: 600 }} align="center">Action</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.map((row) => {
                            return (
                                <TableRow hover key={row.id}>
                                    <TableCell>{row.id}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.name}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.serialNumber}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.consumerNumber}</TableCell>
                                     <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.ip}</TableCell>
                                    <TableCell>{row.port}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{dayjs(row.createdDate).format('MMM D, YYYY')}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.lastSync ? dayjs(row.lastSync).format('MMM D, YYYY') : ''}</TableCell>
                                    <TableCell>
                                        <Chip
                                            label={row.isConnected ? 'Connected' : 'Disconnected'}
                                            color={row.isConnected ? 'success' : 'error'}
                                            size="small"
                                            variant="outlined"
                                        />
                                    </TableCell>
                                    <TableCell align="center" sx={{ whiteSpace: 'nowrap' }}>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            onClick={() => onPropertiesClick(row)}
                                            sx={{ mr: 1, textTransform: 'none' }}
                                        >
                                            Properties
                                        </Button>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            color={row.isConnected ? 'error' : 'success'}
                                            onClick={() => onConnectToggle(row)}
                                            sx={{ mr: 1, textTransform: 'none' }}
                                        >
                                            {row.isConnected ? 'Disconnect' : 'Connect'}
                                        </Button>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            onClick={() => handleDeleteClick(row.id)}
                                            sx={{ textTransform: 'none' }}
                                        >
                                            Delete
                                        </Button>
                                    </TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
            </Box>
            <Divider />
            <TablePagination
                component="div"
                count={count}
                onPageChange={handleChangePage}
                onRowsPerPageChange={handleChangeRowsPerPage}
                page={page}
                rowsPerPage={rowsPerPage}
                rowsPerPageOptions={[5, 10, 25]}
            />
        </Card>
    );
}
