"use client";

import React, { useState, useEffect, useRef } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
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
import { fetchConnectedHeaders, fetchDLMSObjects, fetchProfileGenericEntries, fetchEventStatusMappings } from "../../../api/device";

// Definition of standard event status sections matching Gurux
const DEFAULT_EVENT_STATUS_SECTIONS = [
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
            { code: 51, label: 'R Phase - Current reverse - Occurrence', bitIndex: 4 },
            { code: 52, label: 'R Phase - Current reverse - Restoration', bitIndex: 5 },
            { code: 53, label: 'Y Phase - Current reverse - Occurrence', bitIndex: 8 },
            { code: 54, label: 'Y Phase - Current reverse - Restoration', bitIndex: 9 },
            { code: 55, label: 'B Phase - Current reverse - Occurrence', bitIndex: 10 },
            { code: 56, label: 'B Phase - Current reverse - Restoration', bitIndex: 11 },
            { code: 63, label: 'Current Unbalance - Occurrence', bitIndex: 7 },
            { code: 64, label: 'Current Unbalance - Restoration', bitIndex: 6 },
            { code: 65, label: 'Current bypass - Occurrence', bitIndex: 0 },
            { code: 66, label: 'Current bypass - Restoration', bitIndex: 1 },
            { code: 67, label: 'Over current in any phase - Occurrence', bitIndex: 2 },
            { code: 68, label: 'Over current in any phase - Restoration', bitIndex: 3 },
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

export function EventStatusCheckboxCard({ 
    deviceId, 
    obisCode,
    startDate,
    endDate
}: { 
    deviceId: string | number; 
    obisCode: string;
    startDate?: string;
    endDate?: string;
}) {
    const [loading, setLoading] = useState(true);
    const [entriesLoading, setEntriesLoading] = useState(false);
    const [dlmsObject, setDlmsObject] = useState<any>(null);
    const [value, setValue] = useState<string>("");
    const [profileEntries, setProfileEntries] = useState<any[]>([]);
    const [sections, setSections] = useState<any[]>(DEFAULT_EVENT_STATUS_SECTIONS);
    const [scalers, setScalers] = useState<{[key: string]: number}>({});

    const loadData = async () => {
        setLoading(true);
        setEntriesLoading(true);
        try {
            // Fetch dynamic mappings from the database
            const mappingsRes = await fetchEventStatusMappings();
            let activeSections = DEFAULT_EVENT_STATUS_SECTIONS;
            if (mappingsRes && mappingsRes.status && Array.isArray(mappingsRes.data) && mappingsRes.data.length > 0) {
                const grouped = mappingsRes.data.reduce((acc: any, item: any) => {
                    const obis = item.obisCode;
                    if (!acc[obis]) {
                        acc[obis] = {
                            key: item.category,
                            title: `${item.category.charAt(0).toUpperCase() + item.category.slice(1)} Related Events`,
                            obisCode: obis,
                            items: []
                        };
                    }
                    acc[obis].items.push({
                        code: item.eventCode,
                        label: item.label,
                        bitIndex: item.bitIndex
                    });
                    return acc;
                }, {});
                activeSections = Object.values(grouped);
                setSections(activeSections);
            } else {
                setSections(DEFAULT_EVENT_STATUS_SECTIONS);
            }

            // 1. Fetch DLMS status parameter object value
            const headerRes = await fetchConnectedHeaders(deviceId);
            let foundObj = null;
            let scalerMap: {[key: string]: number} = {};
            if (headerRes && headerRes.status && headerRes.data.length > 0) {
                for (const header of headerRes.data) {
                    const objectsRes = await fetchDLMSObjects(header.id);
                    if (objectsRes && objectsRes.status && Array.isArray(objectsRes.data)) {
                        // Find the scaler profile
                        const sObj = objectsRes.data.find((o: any) => o.obisCode === "1.0.94.91.7.255" || o.name?.toLowerCase().includes("scaler"));
                        if (sObj && sObj.attribute2) {
                            try {
                                const parsed = JSON.parse(sObj.attribute2);
                                const firstRow = Array.isArray(parsed) ? parsed[0] : parsed;
                                if (firstRow) {
                                    Object.keys(firstRow).forEach(key => {
                                        const valStr = String(firstRow[key]);
                                        const parts = valStr.split(",");
                                        const scalerVal = Number(parts[0].trim());
                                        if (!isNaN(scalerVal)) {
                                            scalerMap[key] = scalerVal;
                                        }
                                    });
                                }
                            } catch (e) {
                                console.error("Failed to parse scaler attribute:", e);
                            }
                        }

                        const targetObj = objectsRes.data.find((o: any) => o.obisCode === obisCode);
                        if (targetObj) {
                            foundObj = targetObj;
                            setDlmsObject(targetObj);
                            setValue(targetObj.attribute2 || "");
                        }
                    }
                }
            }
            setScalers(scalerMap);

            // 2. Fetch corresponding Event Profile Generic logged entries
            const profileObis = obisCode.startsWith("0.0.99.98.") ? obisCode : obisCode.replace("0.0.96.11.", "0.0.99.98.");
            const entriesRes = await fetchProfileGenericEntries(deviceId, profileObis, undefined, startDate, endDate);
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

    const loadDataRef = useRef(loadData);
    useEffect(() => {
        loadDataRef.current = loadData;
    });

    useEffect(() => {
        loadData();
    }, [deviceId, obisCode, startDate, endDate]);

    useEffect(() => {
        const connection = new HubConnectionBuilder()
            .withUrl("http://localhost:5135/hubs/meter")
            .withAutomaticReconnect()
            .build();

        connection.start()
            .then(() => {
                console.log("[SignalR] Connected to MeterHub successfully.");
                connection.on("MeterUpdated", (updatedDeviceId: any) => {
                    console.log(`[SignalR] Received MeterUpdated message for device: ${updatedDeviceId}`);
                    if (String(updatedDeviceId) === String(deviceId)) {
                        loadDataRef.current();
                    }
                });
            })
            .catch(err => console.error("[SignalR] Connection to MeterHub failed: ", err));

        return () => {
            connection.stop();
        };
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
                const bit = item.bitIndex !== undefined ? item.bitIndex : index;
                if ((numericValue & (1 << bit)) !== 0) {
                    activeCodes.add(item.code);
                }
            });
            return activeCodes;
        }

        const bitString = normalized.replace(/\s/g, "");
        if (/^[01]+$/.test(bitString)) {
            const reversedBits = bitString.split("").reverse();
            section.items.forEach((item: any, index: number) => {
                const bit = item.bitIndex !== undefined ? item.bitIndex : index;
                const bitIndex = bit % bitString.length;
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

    const queryObis = obisCode.startsWith("0.0.99.98.") ? obisCode.replace("0.0.99.98.", "0.0.96.11.") : obisCode;
    const section = sections.find(s => s.obisCode === queryObis);

    // Group profile entries by timestamp
    const getGroupedEntries = () => {
        const rowsMap: { [time: string]: { [col: string]: string } } = {};
        const columnsSet = new Set<string>();

        profileEntries.forEach(entry => {
            const timeStr = dayjs(entry.entryTime).format('YYYY-MM-DD HH:mm:ss');
            if (!rowsMap[timeStr]) {
                rowsMap[timeStr] = { Timestamp: timeStr };
            }
            
            let rawNum = entry.numericValue;
            let displayVal = "-";

            if (rawNum !== null) {
                const scaler = scalers[entry.columnName];
                if (scaler !== undefined) {
                    const scaled = rawNum * Math.pow(10, scaler);
                    displayVal = String(Number(scaled.toFixed(4)));
                } else {
                    displayVal = String(rawNum);
                }
            } else {
                displayVal = entry.textValue ?? '-';
            }
            
            // Map Event Code to friendly event name
            if (entry.columnName.toLowerCase().includes("code") || entry.columnName.toLowerCase().includes("event")) {
                const codeNum = Number(displayVal);
                if (!isNaN(codeNum) && section) {
                    const item = section.items.find((i: any) => i.code === codeNum);
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

    if (!dlmsObject) {
        return (
            <Card>
                <CardContent>
                    <Alert severity="warning">Event parameters were not found for OBIS Code {obisCode}. Ensure the device is connected and parameter scan has run.</Alert>
                </CardContent>
            </Card>
        );
    }

    return (
        <Stack spacing={4}>
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
