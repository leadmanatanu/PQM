'use client';

import * as React from 'react';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Checkbox from '@mui/material/Checkbox';
import Divider from '@mui/material/Divider';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import dayjs from 'dayjs';

import { useSelection } from '@/hooks/use-selection';
import { fetchDevices } from '../../../api/device'

function noop(): void {
    // do nothing
}

export interface Device {
    id: number;
    name: string;
    ip: string;
    port: number;
    isActive: string;
    isDeleted?: string;
    createdDate?: Date;
    createdId?: number;
    modifiedDate?: Date;
    modifiedId?: number;
    serialNumber: string;
    consumerNumber: string;
    ftpFolder: string;
    lastSync?: Date;
    clientAddress?: number;
    serverAddress?: number;
    authentication?: string;
    password?: string;
    timeout?: number;
    status?: string;
    lastConnectionAttempt?: Date;
    lastError?: string;
    lastEventType?: string;
    lastEventMessage?: string;
    typeName?: string;
}

interface DevicesTableProps {
    count?: number;
    page?: number;
    rows?: Device[];
    rowsPerPage?: number;
    show?: boolean;
    onEdit?: (deviceId: number) => void;
    onDelete?: (deviceId: number) => void;
}

export function DevicesTable({
    count = 0,
    rows = [],
    page = 0,
    rowsPerPage = 0,
    show = true,
    onEdit = () => { },
    onDelete = () => { },
}: DevicesTableProps): React.JSX.Element | null {
    if (!show) return null;
    const rowIds = React.useMemo(() => {
        return rows.map((device) => device.id);
    }, [rows]);


    const { selectAll, deselectAll, selectOne, deselectOne, selected } = useSelection(rowIds);
    const selectedSome = (selected?.size ?? 0) > 0 && (selected?.size ?? 0) < rows.length;
    const selectedAll = rows.length > 0 && selected?.size === rows.length;

    //const [page, setPage] = React.useState(0);
    //const [rowsPerPage, setRowsPerPage] = React.useState(10);


    const handleChangePage = (
        event: React.MouseEvent<HTMLButtonElement> | null,
        newPage: number
    ) => {
        //setPage(newPage);
    };

    const handleChangeRowsPerPage = (
        event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
    ) => {
        //setRowsPerPage(parseInt(event.target.value, 10));
        //setPage(0);
        rowsPerPage = parseInt(event.target.value, 10);
        page = 0;
    };

    const handleEditClick = (deviceId: number) => {
        console.log(`Edit device with ID: ${deviceId}`);
        // Placeholder for edit logic (e.g., open edit form)
        onEdit(deviceId);
    };

    const handleDeleteClick = (deviceId: number) => {
        console.log(`Delete device with ID: ${deviceId}`);
        // Placeholder for edit logic (e.g., open edit form)
        onDelete(deviceId);
    };

    const handleSyncClick = (deviceId: number) => {
        console.log(`Sync device with ID: ${deviceId}`);
        // Placeholder for sync logic (e.g., trigger sync API call)
    };

    React.useEffect(() => {
        //const devices = await fetchDevices() satisfies Device[];
    }, [page, rowsPerPage]);

    return (
        <Card sx={{ borderRadius: '8px' }}>
            <Box sx={{ overflowX: 'auto', maxHeight: '350px', overflowY: 'auto' }}>
                <Table size="small" sx={{ minWidth: '800px' }}>
                    <TableHead sx={{ bgcolor: 'var(--mui-palette-neutral-50)' }}>
                        <TableRow>
                            <TableCell sx={{ fontWeight: 600 }}>Id</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Name</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Serial No</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Consumer No</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>FTP Folder</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Active</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Meter Type</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Connection</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>IP</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>PORT</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Last Sync</TableCell>
                            <TableCell sx={{ fontWeight: 600 }}>Last Event</TableCell>
                            <TableCell sx={{ fontWeight: 600 }} align="center">Action</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.map((row) => {
                            const isSelected = selected?.has(row.id);
                            const connectionStatus = row.status ?? 'Offline';

                            const statusColor = (() => {
                                switch (connectionStatus) {
                                    case 'Online':    return 'success';
                                    case 'Connecting': return 'warning';
                                    case 'Error':     return 'error';
                                    case 'Disabled':  return 'default';
                                    default:          return 'default'; // Offline
                                }
                            })() as 'success' | 'warning' | 'error' | 'default';

                            const statusVariant = connectionStatus === 'Online' ? 'filled' : 'outlined';

                            return (
                                <TableRow hover key={row.id} selected={isSelected}>
                                    <TableCell>{row.id}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.name}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.serialNumber}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.consumerNumber}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.ftpFolder}</TableCell>
                                    <TableCell>
                                        <Chip
                                            label={row.isActive ? 'Active' : 'Inactive'}
                                            color={row.isActive ? 'success' : 'default'}
                                            size="small"
                                            variant="outlined"
                                        />
                                    </TableCell>
                                    <TableCell>{row.typeName || 'ABT'}</TableCell>
                                    <TableCell>
                                        <Chip
                                            label={connectionStatus}
                                            color={statusColor}
                                            size="small"
                                            variant={statusVariant}
                                            sx={{ fontWeight: 600, minWidth: 80, textAlign: 'center' }}
                                        />
                                    </TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>{row.ip}</TableCell>
                                    <TableCell>{row.port}</TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap' }}>
                                        {row.lastSync
                                            ? dayjs(row.lastSync).format('MMM D, YYYY HH:mm')
                                            : <Typography variant="caption" color="text.disabled">Never</Typography>}
                                    </TableCell>
                                    <TableCell sx={{ whiteSpace: 'nowrap', maxWidth: 200 }}>
                                        {row.lastEventType ? (
                                            <Stack spacing={0}>
                                                <Typography variant="caption" fontWeight={600} color="text.primary">
                                                    {row.lastEventType}
                                                </Typography>
                                                {row.lastEventMessage && (
                                                    <Typography variant="caption" color="text.secondary" sx={{
                                                        overflow: 'hidden',
                                                        textOverflow: 'ellipsis',
                                                        whiteSpace: 'nowrap',
                                                        maxWidth: 180,
                                                        display: 'block'
                                                    }}>
                                                        {row.lastEventMessage}
                                                    </Typography>
                                                )}
                                            </Stack>
                                        ) : (
                                            <Typography variant="caption" color="text.disabled">—</Typography>
                                        )}
                                    </TableCell>
                                    <TableCell align="center" sx={{ whiteSpace: 'nowrap' }}>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            onClick={() => handleEditClick(row.id)}
                                            sx={{ mr: 1, textTransform: 'none' }}
                                        >
                                            Edit
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
