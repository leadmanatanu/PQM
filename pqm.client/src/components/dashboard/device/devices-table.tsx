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
    isDeleted: string;
    createdDate: Date;
    createdId: number;
    modifiedDate: Date;
    modifiedId: number;
}

interface DevicesTableProps {
    count?: number;
    page?: number;
    rows?: Device[];
    rowsPerPage?: number;
}

export function DevicesTable({
    count = 0,
    rows = [],
    page = 0,
    rowsPerPage = 0,
}: DevicesTableProps): React.JSX.Element {
    const rowIds = React.useMemo(() => {
        return rows.map((device) => device.id);
    }, [rows]);

    const { selectAll, deselectAll, selectOne, deselectOne, selected } = useSelection(rowIds);
    const selectedSome = (selected?.size ?? 0) > 0 && (selected?.size ?? 0) < rows.length;
    const selectedAll = rows.length > 0 && selected?.size === rows.length;

    //const [page, setPage] = React.useState(0);
    //const [rowsPerPage, setRowsPerPage] = React.useState(10);

    const handleChangePage = (event, newPage) => {
        setPage(newPage);
        //page = newPage;
    };

    const handleChangeRowsPerPage = (event) => {
        //setRowsPerPage(parseInt(event.target.value, 10));
        //setPage(0);
        rowsPerPage = parseInt(event.target.value, 10);
        page = 0;
    };

    React.useEffect(() => {
        //const devices = await fetchDevices() satisfies Device[];
    }, [page, rowsPerPage]);

    return (
        <Card>
            <Box sx={{ overflowX: 'auto' }}>
                <Table sx={{ minWidth: '800px' }}>
                    <TableHead>
                        <TableRow>
                            <TableCell>Id</TableCell>
                            <TableCell>Name</TableCell>
                            <TableCell>Serial No</TableCell>
                            <TableCell>Consumer No</TableCell>
                            <TableCell>FTP Folder</TableCell>
                            <TableCell>Status</TableCell>
                            <TableCell>IP</TableCell>
                            <TableCell>PORT</TableCell>
                            <TableCell>Created Date</TableCell>
                            <TableCell>Action</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.map((row) => {
                            const isSelected = selected?.has(row.id);

                            return (
                                <TableRow hover key={row.id} selected={isSelected}>
                                    <TableCell>{row.id}</TableCell>
                                    <TableCell>{row.name}</TableCell>
                                    <TableCell>SER1025767</TableCell>
                                    <TableCell>CON123465</TableCell>
                                    <TableCell>{row.ftpFolder}</TableCell>
                                    <TableCell>{row.isActive ? <p>Active</p> : <p>Inactive</p>}</TableCell>
                                    <TableCell>{row.ip}</TableCell>
                                    <TableCell>{row.port}</TableCell>
                                    <TableCell>{dayjs(row.createdDate).format('MMM D, YYYY')}</TableCell>
                                    <TableCell></TableCell>
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
