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
    eventType?: string | null;
}

export function EventRTable({
    rows = [],
    totalCount,
    page,
    rowsPerPage,
    onPageChange = () => { },
    eventType,
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

    const eventTypeColumns: Record<
        string,
        { label: string; field: string, type?: "date" | "number" | "text" }[]
    > = {
        dip: [
            { label: "Phase", field: "phase" },
            { label: "Start Time", field: "start_Time", type: "date" },
            { label: "End Time", field: "end_Time", type: "date" },
            { label: "Duration", field: "duration" },
            { label: "Min Voltage", field: "min_Voltage" },
        ],
        swell: [
            { label: "Phase", field: "phase" },
            { label: "Start Time", field: "start_Time", type: "date" },
            { label: "End Time", field: "end_Time", type: "date" },
            { label: "Duration", field: "duration" },
            { label: "Max Voltage", field: "max_Voltage" },
        ],
        rvc: [
            { label: "Phase", field: "phase" },
            { label: "Start Time", field: "start_Time", type: "date" },
            { label: "End Time", field: "end_Time", type: "date" },
            { label: "Duration", field: "duration" },
            { label: "UMAX", field: "umax" },
            { label: "USS", field: "uss" },
        ],
        interrupt: [
            { label: "Phase", field: "phase" },
            { label: "Start Time", field: "start_Time", type: "date" },
            { label: "End Time", field: "end_Time", type: "date" },
            { label: "Duration", field: "duration" },
        ],
        shortflicker: [
            { label: "Date", field: "date", type: "date" },
            { label: "A", field: "a" },
            { label: "B", field: "b" },
            { label: "C", field: "c" },
        ],
        longflicker: [
            { label: "Date", field: "date", type: "date" },
            { label: "A", field: "a" },
            { label: "B", field: "b" },
            { label: "C", field: "c" },
        ],
    };

    return (
        <Card>
            <Box sx={{ overflowX: 'auto' }}>
                <Table sx={{ minWidth: '800px' }}>
                    <TableHead>
                        <TableRow>
                            {eventType &&
                                eventTypeColumns[eventType]?.map((col) => (
                                    <TableCell key={col.field}>{col.label}</TableCell>
                                ))}
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rows.length > 0 ? (
                            rows.map((row) => (
                                <TableRow hover key={row.id}>
                                    {eventType &&
                                        eventTypeColumns[eventType]?.map((col) => (
                                            <TableCell key={col.field}>
                                                {col.type === "date"
                                                    ? row[col.field]
                                                        ? dayjs(row[col.field]).format("MMM D, YYYY HH:mm")
                                                        : "-"
                                                    : row[col.field] ?? "N/A"}
                                            </TableCell>
                                        ))}
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
