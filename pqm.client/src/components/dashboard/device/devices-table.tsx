'use client';

import React, { useState } from 'react';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Divider from '@mui/material/Divider';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import Chip from '@mui/material/Chip';
import dayjs from 'dayjs';
import MoreVertIcon from "@mui/icons-material/MoreVert";
import {
    IconButton,
    Menu,
    MenuItem,
} from "@mui/material";
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
    deviceType?: string;
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
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const [selectedRow, setSelectedRow] = useState<Device | null>(null);
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
    const handleMenuOpen = (
        event: React.MouseEvent<HTMLElement>,
        row: any
    ) => {
        setAnchorEl(event.currentTarget);
        setSelectedRow(row);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
        setSelectedRow(null);
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
                            <TableCell sx={{ fontWeight: 600 }}>Type</TableCell>
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
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.deviceType || '—'}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.lastSync ? dayjs(row.lastSync).format('MMM D, YYYY') : ''}</TableCell>
                                    <TableCell>
                                        <Chip
                                            label={row.isConnected ? 'Connected' : 'Disconnected'}
                                            color={row.isConnected ? 'success' : 'error'}
                                            size="small"
                                            variant="outlined"
                                        />
                                    </TableCell>
                                    <TableCell align="center">
                                        <IconButton
                                            onClick={(e) => handleMenuOpen(e, row)}
                                            size="small"
                                        >
                                            <MoreVertIcon />
                                        </IconButton>

                                        <Menu
                                            anchorEl={anchorEl}
                                            open={Boolean(anchorEl)}
                                            onClose={handleMenuClose}
                                        >
                                            <MenuItem
                                                onClick={() => {
                                                    if (selectedRow) {
                                                        onPropertiesClick(selectedRow);
                                                    }
                                                    handleMenuClose();
                                                }}
                                            >
                                                Properties
                                            </MenuItem>

                                            <MenuItem
                                                onClick={() => {
                                                    if (selectedRow) {
                                                        onConnectToggle(selectedRow);
                                                    }
                                                    handleMenuClose();
                                                }}
                                            >
                                                {selectedRow?.isConnected ? "Disconnect" : "Connect"}
                                            </MenuItem>

                                            <MenuItem
                                                onClick={() => {
                                                    if (selectedRow) {
                                                        handleDeleteClick(selectedRow.id);
                                                    }
                                                    handleMenuClose();                                                    
                                                }}
                                                sx={{ color: "error.main" }}
                                            >
                                                Delete
                                            </MenuItem>
                                        </Menu>
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
