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
import dayjs from 'dayjs';

interface DeviceRTableProps {
    rows?: any[];
    totalCount: number;
    page: number;
    rowsPerPage: number;
    onPageChange?: (page: number, rowsPerPage: number) => void;
}

export function EventRTable({
    rows = [],
    totalCount,
    page,
    rowsPerPage,
    onPageChange = () => { },
}: DeviceRTableProps): React.JSX.Element | null {
    const handleChangePage = (
        event: React.MouseEvent<HTMLButtonElement> | null,
        newPage: number
    ) => {
        onPageChange(newPage, rowsPerPage);
    };

    const handleChangeRowsPerPage = (
        event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
    ) => {
        const newRowsPerPage = parseInt(event.target.value, 10);
        if (newRowsPerPage === rowsPerPage) return;
        onPageChange(0, newRowsPerPage);
    };

    return (
        <Card>
            <Box sx={{ overflowX: 'auto' }}>
                <Table sx={{ minWidth: '800px' }}>
                    <TableHead>
                        <TableRow>
                            <TableCell>Phase</TableCell>
                            <TableCell>Duration</TableCell>
                            <TableCell>Start Time</TableCell>
                            <TableCell>End Time</TableCell>
                            <TableCell>Min Voltage</TableCell>
                            <TableCell>Max Voltage</TableCell>
                            <TableCell>UMAX</TableCell>
                            <TableCell>USS</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.length > 0 ? (
                            rows.map((row) => (
                                <TableRow hover key={row.id}>
                                    <TableCell>{row.phase}</TableCell>
                                    <TableCell>{row.duration}</TableCell>
                                    <TableCell>
                                        {row.start_Time
                                            ? dayjs(row.start_Time).format('MMM D, YYYY HH:mm')
                                            : '-'}
                                    </TableCell>
                                    <TableCell>
                                        {row.end_Time
                                            ? dayjs(row.end_Time).format('MMM D, YYYY HH:mm')
                                            : '-'}
                                    </TableCell>
                                    <TableCell>{row.min_Voltage ?? 'N/A'}</TableCell>
                                    <TableCell>{row.max_Voltage ?? 'N/A'}</TableCell>
                                    <TableCell>{row.umax ?? 'N/A'}</TableCell>
                                    <TableCell>{row.uss ?? 'N/A'}</TableCell>
                                </TableRow>
                            ))
                        ) : (
                            <TableRow>
                                <TableCell colSpan={8} align="center">
                                    No data available
                                </TableCell>
                            </TableRow>
                        )}
                    </TableBody>
                </Table>
            </Box>

            <Divider />

            <TablePagination
                component="div"
                count={totalCount}
                page={page}
                onPageChange={handleChangePage}
                rowsPerPage={rowsPerPage}
                onRowsPerPageChange={handleChangeRowsPerPage}
                rowsPerPageOptions={[10, 25, 50, 100]}
                SelectProps={{
                    MenuProps: {
                        keepMounted: true,
                        disableScrollLock: true,
                        disablePortal: false, // ✅ default: render at document.body
                        container: typeof document !== "undefined" ? document.body : undefined,
                    },
                }}
            />
        </Card>
    );
}
