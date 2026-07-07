"use client";

import React, { useState, useEffect } from "react";
import { 
    Card, 
    CardHeader, 
    CardContent, 
    Divider, 
    Stack, 
    FormControlLabel, 
    Checkbox, 
    Box, 
    Typography, 
    Alert, 
    CircularProgress,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Paper
} from "@mui/material";
import dayjs from "dayjs";
import { fetchConnectedHeaders, fetchDLMSObjects, fetchProfileGenericEntries } from "../../../api/device";

// Definition of standard event status sections matching Gurux
const EVENT_STATUS_SECTIONS = [
    {
        key: 'voltage',
        title: 'Voltage Related Events',
        obisCode: '0.0.96.11.0.255',
        items: [
            { code: 1, label: 'R-Phase - Voltage Missing - Occurrence' },
            { code: 2, label: 'R-Phase - Voltage Missing - Restoration' },
            { code: 3, label: 'Y-Phase - Voltage Missing - Occurrence' },
            { code: 4, label: 'Y-Phase - Voltage Missing - Restoration' },
            { code: 5, label: 'B-Phase - Voltage Missing - Occurrence' },
            { code: 6, label: 'B-Phase - Voltage Missing - Restoration' },
            { code: 7, label: 'Over Voltage in any Phase - Occurrence' },
            { code: 8, label: 'Over Voltage in any Phase - Restoration' },
            { code: 9, label: 'Low Voltage in any Phase - Occurrence' },
            { code: 10, label: 'Low Voltage in any Phase - Restoration' },
            { code: 11, label: 'Voltage Unbalance - Occurrence' },
            { code: 12, label: 'Voltage Unbalance - Restoration' },
        ],
    },
    {
        key: 'current',
        title: 'Current Related Events',
        obisCode: '0.0.96.11.1.255',
        items: [
            { code: 51, label: 'R Phase - Current reverse - Occurrence' },
            { code: 52, label: 'R Phase - Current reverse - Restoration' },
            { code: 53, label: 'Y Phase - Current reverse - Occurrence' },
            { code: 54, label: 'Y Phase - Current reverse - Restoration' },
            { code: 55, label: 'B Phase - Current reverse - Occurrence' },
            { code: 56, label: 'B Phase - Current reverse - Restoration' },
            { code: 63, label: 'Current Unbalance - Occurrence' },
            { code: 64, label: 'Current Unbalance - Restoration' },
            { code: 65, label: 'Current bypass - Occurrence' },
            { code: 66, label: 'Current bypass - Restoration' },
            { code: 67, label: 'Over current in any phase - Occurrence' },
            { code: 68, label: 'Over current in any phase - Restoration' },
        ],
    },
    {
        key: 'power',
        title: 'Power Related Events',
        obisCode: '0.0.96.11.2.255',
        items: [
            { code: 101, label: 'Power failure - Occurrence' },
            { code: 102, label: 'Power failure - Restoration' },
        ],
    },
    {
        key: 'transaction',
        title: 'Transaction Related Events',
        obisCode: '0.0.96.11.3.255',
        items: [
            { code: 151, label: 'Real Time Clock - Date and Time' },
            { code: 152, label: 'Demand Integration Period' },
            { code: 153, label: 'Profile Capture Period' },
            { code: 154, label: 'Single-action Schedule for Billing Dates' },
            { code: 155, label: 'Activity Calendar Time Zones' },
            { code: 157, label: 'New Firmware Activated' },
            { code: 158, label: 'Load limit (kW) set' },
            { code: 159, label: 'Enabled - load limit function' },
            { code: 160, label: 'Disabled - load limit function' },
            { code: 161, label: 'LLS secret (MR) change' },
            { code: 162, label: 'HLS key (US) change' },
            { code: 163, label: 'HLS key (FW) change' },
            { code: 164, label: 'Global key change(encryption and authentication)' },
            { code: 165, label: 'ESWF change' },
            { code: 166, label: 'MD reset' },
            { code: 169, label: 'Single Action Schedule for Image Activation' },
            { code: 182, label: 'Passive Relay time.' },
        ],
    },
    {
        key: 'others',
        title: 'Others Events',
        obisCode: '0.0.96.11.4.255',
        items: [
            { code: 201, label: 'Influence of permanent magnet - Occurrence' },
            { code: 202, label: 'Influence of permanent magnet - Restoration' },
            { code: 203, label: 'Neutral disturbance - Occurrence' },
            { code: 204, label: 'Neutral disturbance - Restoration' },
            { code: 205, label: 'Meter cover opened' },
            { code: 206, label: 'Terminal cover opened' },
        ],
    },
];

export function EventStatusCheckboxCard({ deviceId, obisCode }: { deviceId: string | number, obisCode: string }) {
    const [loading, setLoading] = useState(true);
    const [entriesLoading, setEntriesLoading] = useState(false);
    const [dlmsObject, setDlmsObject] = useState<any>(null);
    const [value, setValue] = useState<string>("");
    const [profileEntries, setProfileEntries] = useState<any[]>([]);

    const loadData = async () => {
        setLoading(true);
        setEntriesLoading(true);
        try {
            // 1. Fetch DLMS status parameter object value
            const headerRes = await fetchConnectedHeaders(deviceId);
            let foundObj = null;
            if (headerRes && headerRes.status && headerRes.data.length > 0) {
                for (const header of headerRes.data) {
                    const objectsRes = await fetchDLMSObjects(header.id);
                    if (objectsRes && objectsRes.status && Array.isArray(objectsRes.data)) {
                        foundObj = objectsRes.data.find((o: any) => o.obisCode === obisCode);
                        if (foundObj) {
                            setDlmsObject(foundObj);
                            setValue(foundObj.attribute2 || "");
                            break;
                        }
                    }
                }
            }

            // 2. Fetch corresponding Event Profile Generic logged entries
            const profileObis = obisCode.replace("0.0.96.11.", "0.0.99.98.");
            const entriesRes = await fetchProfileGenericEntries(deviceId, profileObis);
            if (entriesRes && entriesRes.status) {
                setProfileEntries(entriesRes.data || []);
            } else {
                setProfileEntries([]);
            }
        } catch (error) {
            console.error("Error loading event status or log entries:", error);
        } finally {
            setLoading(false);
            setEntriesLoading(false);
        }
    };

    useEffect(() => {
        loadData();
    }, [deviceId, obisCode]);

    const parseEventStatusValue = (val: string, section: any): Set<number> => {
        const activeCodes = new Set<number>();
        if (!val || val === "Waiting..." || val === "Scanning..." || val.startsWith("Error")) {
            return activeCodes;
        }

        const normalized = val.trim();
        const numericValue = Number(normalized);
        if (!Number.isNaN(numericValue)) {
            section.items.forEach((item: any, index: number) => {
                if ((numericValue & (1 << index)) !== 0) {
                    activeCodes.add(item.code);
                }
            });
            return activeCodes;
        }

        const bitString = normalized.replace(/\s/g, "");
        if (/^[01]+$/.test(bitString)) {
            const reversedBits = bitString.split("").reverse();
            section.items.forEach((item: any, index: number) => {
                const bitIndex = index % bitString.length;
                if (reversedBits[bitIndex] === "1") {
                    activeCodes.add(item.code);
                }
            });
            return activeCodes;
        }

        section.items.forEach((item: any) => {
            if (normalized.toLowerCase().includes(item.label.toLowerCase()) || normalized.includes(String(item.code))) {
                activeCodes.add(item.code);
            }
        });

        return activeCodes;
    };

    const section = EVENT_STATUS_SECTIONS.find(s => s.obisCode === obisCode);

    // Group profile entries by timestamp
    const getGroupedEntries = () => {
        const rowsMap: { [time: string]: { [col: string]: string } } = {};
        const columnsSet = new Set<string>();

        profileEntries.forEach(entry => {
            const timeStr = dayjs(entry.entryTime).format('YYYY-MM-DD HH:mm:ss');
            if (!rowsMap[timeStr]) {
                rowsMap[timeStr] = { Timestamp: timeStr };
            }
            
            let displayVal = entry.numericValue !== null ? String(entry.numericValue) : (entry.textValue ?? '-');
            
            // Map Event Code to friendly event name
            if (entry.columnName.toLowerCase().includes("code") || entry.columnName.toLowerCase() === "event") {
                const codeNum = Number(displayVal);
                if (!isNaN(codeNum) && section) {
                    const item = section.items.find(i => i.code === codeNum);
                    if (item) {
                        displayVal = `${item.label} (${codeNum})`;
                    }
                }
            }
            
            rowsMap[timeStr][entry.columnName] = displayVal;
            columnsSet.add(entry.columnName);
        });

        const columns = ['Timestamp', ...Array.from(columnsSet)];
        const rows = Object.values(rowsMap).sort((a, b) => b.Timestamp.localeCompare(a.Timestamp));
        return { columns, rows };
    };

    const { columns, rows } = getGroupedEntries();

    if (loading) {
        return (
            <Card>
                <CardContent sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
                    <CircularProgress />
                </CardContent>
            </Card>
        );
    }

    if (!dlmsObject || !section) {
        return (
            <Card>
                <CardContent>
                    <Alert severity="warning">Event status parameters were not found for OBIS Code {obisCode}. Ensure the device is connected and parameter scan has run.</Alert>
                </CardContent>
            </Card>
        );
    }

    const activeCodes = parseEventStatusValue(value, section);

    return (
        <Stack spacing={4}>
            {/* Status Checkbox Card */}
            <Card sx={{ maxWidth: 'sm' }}>
                <CardHeader 
                    title={dlmsObject.name} 
                    subheader={`OBIS Code: ${obisCode} | Value: ${value || '0'}`}
                    titleTypographyProps={{ variant: 'h6', fontWeight: 'bold' }}
                />
                <Divider />
                <CardContent>
                    <Stack spacing={1}>
                        {section.items.map((item) => (
                            <FormControlLabel
                                key={item.code}
                                control={
                                    <Checkbox 
                                        checked={activeCodes.has(item.code)} 
                                        disabled 
                                        size="small" 
                                    />
                                }
                                label={`${item.label} (${item.code})`}
                                sx={{
                                    m: 0,
                                    '& .MuiFormControlLabel-label': {
                                        fontSize: '0.9rem',
                                        color: activeCodes.has(item.code) ? 'text.primary' : 'text.secondary'
                                    }
                                }}
                            />
                        ))}
                    </Stack>
                </CardContent>
            </Card>

            {/* Historical Table */}
            <Card>
                <CardHeader 
                    title="Logged Occurrences" 
                    subheader="List of events recorded on the meter with exact timestamps"
                    titleTypographyProps={{ variant: 'h6', fontWeight: 'medium' }}
                />
                <Divider />
                <CardContent>
                    {entriesLoading ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
                            <CircularProgress />
                        </Box>
                    ) : rows.length > 0 ? (
                        <TableContainer component={Paper} sx={{ maxHeight: 500 }}>
                            <Table stickyHeader size="small">
                                <TableHead>
                                    <TableRow>
                                        {columns.map((col) => (
                                            <TableCell key={col} sx={{ fontWeight: 'bold', bgcolor: 'background.neutral' }}>{col}</TableCell>
                                        ))}
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {rows.map((row, idx) => (
                                        <TableRow key={idx} hover>
                                            {columns.map((col) => (
                                                <TableCell key={col}>{row[col] ?? '-'}</TableCell>
                                            ))}
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    ) : (
                        <Alert severity="info">
                            No event occurrences have been logged for this event category.
                        </Alert>
                    )}
                </CardContent>
            </Card>
        </Stack>
    );
}
