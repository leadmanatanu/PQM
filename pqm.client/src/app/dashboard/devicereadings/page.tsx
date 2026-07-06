'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import CircularProgress from '@mui/material/CircularProgress';
import Box from '@mui/material/Box';
import Tabs from '@mui/material/Tabs';
import Tab from '@mui/material/Tab';
import Grid from '@mui/material/Grid';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Paper from '@mui/material/Paper';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import Divider from '@mui/material/Divider';

import { DeviceRTable } from '@/components/dashboard/devicereadings/devices-table';
import { DeviceFilters } from '@/components/dashboard/devicereadings/device-selection';

import { 
    fetchDevices, 
    fetchDeviceParameter, 
    fetchDeviceReading,
    fetchConnectedHeaders,
    fetchDLMSObjects,
    fetchClockLatest,
    fetchActivityCalendarLatest,
    fetchProfileGenericEntries,
    fetchDeviceConfiguration
} from '../../../api/device';
import dayjs, { Dayjs } from 'dayjs';
import { Device } from '../../../components/dashboard/device/devices-table';

interface DeviceParameter {
    id: string | number;
    name: string;
    [key: string]: any;
}

interface DeviceLog {
    [key: string]: any;
}

export default function Page(): React.JSX.Element {
    const [loading, setLoading] = useState<'devices' | 'parameters' | 'search' | 'data' | null>('devices');
    const [devices, setDevices] = useState<Device[]>([]);
    const [selectedDeviceId, setSelectedDeviceId] = useState<string | number>(0);
    const [devParamArr, setDevParamArr] = useState<DeviceParameter[]>([]);
    const [deviceLogArr, setDeviceLogArr] = useState<DeviceLog[]>([]);
    const [selParamName, setSelParamName] = useState<string | undefined>(undefined);
    
    // Tabbed dashboard states
    const [activeTab, setActiveTab] = useState(0);
    const [liveObjects, setLiveObjects] = useState<any[]>([]);
    const [latestClock, setLatestClock] = useState<any>(null);
    const [latestCalendar, setLatestCalendar] = useState<any>(null);
    const [profileObis, setProfileObis] = useState<string>('1.0.99.1.0.255'); // default load profile
    const [profileEntries, setProfileEntries] = useState<any[]>([]);
    const [configData, setConfigData] = useState<any>(null);

    const [isVisible, setIsVisible] = useState(false);

    useEffect(() => {
        const loadDevices = async () => {
            setLoading('devices');
            try {
                const fetchedDevices = await fetchDevices();
                setDevices(fetchedDevices ?? []);
            } catch (error) {
                console.error('Failed to fetch devices:', error);
            } finally {
                setLoading(null);
            }
        };
        loadDevices();
    }, []);

    const handleDeviceSelection = async (id: string | number) => {
        setSelectedDeviceId(id);
        setLoading('parameters');
        try {
            // Load parameters for historical query dropdown
            const fetchedDeviceParameter = await fetchDeviceParameter(id);
            const seenNames = new Set<string>();
            const uniqueParams: DeviceParameter[] = fetchedDeviceParameter.data.filter(
                (param: DeviceParameter) => {
                    if (seenNames.has(param.name)) return false;
                    seenNames.add(param.name);
                    return true;
                }
            );
            setDevParamArr(uniqueParams);

            // Fetch live/overview dashboard data
            await refreshDeviceTabDetails(id, activeTab);
        } catch (error) {
            console.error('Failed to fetch device parameters:', error);
            setDevParamArr([]);
        } finally {
            setLoading(null);
        }
    };

    const refreshDeviceTabDetails = async (deviceId: string | number, tabIndex: number) => {
        if (!deviceId) return;
        setLoading('data');
        try {
            if (tabIndex === 0) {
                // Fetch Overview: live parameter values & latest clock
                const headerRes = await fetchConnectedHeaders(deviceId);
                if (headerRes && headerRes.status && headerRes.data.length > 0) {
                    const headerId = headerRes.data[0].id;
                    const objectsRes = await fetchDLMSObjects(headerId);
                    if (objectsRes && objectsRes.status) {
                        setLiveObjects(objectsRes.data ?? []);
                    }
                } else {
                    setLiveObjects([]);
                }

                const clockRes = await fetchClockLatest(deviceId);
                if (clockRes && clockRes.status) {
                    setLatestClock(clockRes.data);
                } else {
                    setLatestClock(null);
                }
            } else if (tabIndex === 2) {
                // Fetch Load & Billing Profiles
                const entriesRes = await fetchProfileGenericEntries(deviceId, profileObis);
                if (entriesRes && entriesRes.status) {
                    setProfileEntries(entriesRes.data ?? []);
                } else {
                    setProfileEntries([]);
                }
            } else if (tabIndex === 3) {
                // Fetch Configuration Settings
                const configRes = await fetchDeviceConfiguration(deviceId);
                if (configRes && configRes.status) {
                    setConfigData(configRes.data);
                } else {
                    setConfigData(null);
                }

                const calRes = await fetchActivityCalendarLatest(deviceId);
                if (calRes && calRes.status) {
                    setLatestCalendar(calRes.data);
                } else {
                    setLatestCalendar(null);
                }
            }
        } catch (error) {
            console.error('Error refreshing device tab data:', error);
        } finally {
            setLoading(null);
        }
    };

    const handleTabChange = async (event: React.SyntheticEvent, newValue: number) => {
        setActiveTab(newValue);
        if (selectedDeviceId) {
            await refreshDeviceTabDetails(selectedDeviceId, newValue);
        }
    };

    const handleProfileObisChange = async (obis: string) => {
        setProfileObis(obis);
        if (selectedDeviceId) {
            setLoading('data');
            try {
                const entriesRes = await fetchProfileGenericEntries(selectedDeviceId, obis);
                setProfileEntries(entriesRes?.data ?? []);
            } catch (error) {
                console.error('Failed to query profile entries:', error);
            } finally {
                setLoading(null);
            }
        }
    };

    const handleSearch = async ({
        deviceId,
        startTime,
        endTime,
        paramId,
    }: {
        deviceId: string | number | null;
        startTime: Dayjs | null;
        endTime: Dayjs | null;
        paramId: string | number | null;
    }) => {
        setIsVisible(true);
        if (!deviceId || !paramId || !startTime || !endTime || endTime.isBefore(startTime)) {
            console.error('Search failed: Invalid parameters');
            return;
        }

        const startDate = startTime.format('MM/DD/YYYY');
        const endDate = endTime.format('MM/DD/YYYY');

        setLoading('search');
        try {
            const data = await fetchDeviceReading(deviceId, paramId, 1, 1000000, startDate, endDate);
            if (data) {
                const matchedRow = devParamArr.find((d) => d.id === paramId);
                if (matchedRow) {
                    setSelParamName(matchedRow.name);
                }
                setDeviceLogArr(data.data.deviceLogSearch ?? []);
            }
        } catch (error) {
            console.error('Search failed:', error);
        } finally {
            setLoading(null);
        }
    };

    const parseUnit = (attr3: string) => {
        if (!attr3) return '';
        const match = attr3.match(/,\s*([A-Za-z0-9/]+)/);
        return match ? match[1] : '';
    };

    const getLiveValueObj = (nameKeyword: string) => {
        const match = liveObjects.find(o => o.name.toLowerCase().includes(nameKeyword.toLowerCase()));
        if (!match) return { value: '-', unit: '' };
        return {
            value: match.attribute2,
            unit: parseUnit(match.attribute3)
        };
    };

    // Group profile entries by timestamp for displaying in grid
    const getGroupedProfileEntries = () => {
        const rowsMap: { [time: string]: { [col: string]: string } } = {};
        const columnsSet = new Set<string>();

        profileEntries.forEach(entry => {
            const timeStr = dayjs(entry.entryTime).format('YYYY-MM-DD HH:mm:ss');
            if (!rowsMap[timeStr]) {
                rowsMap[timeStr] = { Timestamp: timeStr };
            }
            const displayVal = entry.numericValue !== null ? `${entry.numericValue}${entry.unit ? ' ' + entry.unit : ''}` : entry.textValue;
            rowsMap[timeStr][entry.columnName] = displayVal ?? '-';
            columnsSet.add(entry.columnName);
        });

        const columns = ['Timestamp', ...Array.from(columnsSet)];
        const rows = Object.values(rowsMap).sort((a, b) => b.Timestamp.localeCompare(a.Timestamp));
        return { columns, rows };
    };

    const { columns: profileColumns, rows: profileRows } = getGroupedProfileEntries();

    return (
        <div>
            {loading && (
                <Box
                    sx={{
                        display: 'flex',
                        justifyContent: 'center',
                        alignItems: 'center',
                        position: 'absolute',
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                        backgroundColor: 'rgba(255, 255, 255, 0.7)',
                        zIndex: 9999,
                    }}
                >
                    <CircularProgress />
                </Box>
            )}
            
            <Stack spacing={3}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="h4">Device Readings & Analytics</Typography>
                </Box>

                {/* Device selection filter is always visible at the top */}
                <DeviceFilters
                    rows={devices}
                    onDeviceSelect={handleDeviceSelection}
                    paramArray={devParamArr}
                    onSearch={handleSearch}
                />

                {Number(selectedDeviceId) > 0 && (
                    <Box sx={{ width: '100%', mt: 2 }}>
                        <Tabs
                            value={activeTab}
                            onChange={handleTabChange}
                            indicatorColor="primary"
                            textColor="primary"
                            sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
                        >
                            <Tab label="Dashboard Overview" />
                            <Tab label="Historical Logs" />
                            <Tab label="Load & Billing Profiles" />
                            <Tab label="Device Settings" />
                        </Tabs>

                        {/* TAB 0: DASHBOARD OVERVIEW */}
                        {activeTab === 0 && (
                            <Stack spacing={3}>
                                <Card sx={{ p: 1, bgcolor: '#f4f6f8' }}>
                                    <CardContent>
                                        <Grid container spacing={2} alignItems="center">
                                            <Grid size={{ xs: 12, md: 4 }}>
                                                <Typography variant="subtitle2" color="text.secondary">METER CLOCK</Typography>
                                                <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                                                    {latestClock?.value ? latestClock.value : 'No sync time recorded'}
                                                </Typography>
                                            </Grid>
                                            <Grid size={{ xs: 12, md: 4 }}>
                                                <Typography variant="subtitle2" color="text.secondary">LAST SYNC DATE</Typography>
                                                <Typography variant="h6">
                                                    {latestClock?.dateEntered ? dayjs(latestClock.dateEntered).format('MMM D, YYYY HH:mm:ss') : '-'}
                                                </Typography>
                                            </Grid>
                                        </Grid>
                                    </CardContent>
                                </Card>

                                <Typography variant="h6" sx={{ fontWeight: 'medium' }}>Live Readings</Typography>
                                <Grid container spacing={3}>
                                    {/* Voltages */}
                                    {['L1', 'L2', 'L3'].map((phase, idx) => {
                                        const v = getLiveValueObj(`Voltage L${idx + 1}`);
                                        return (
                                            <Grid size={{ xs: 12, sm: 4 }} key={`v-${phase}`}>
                                                <Card sx={{ borderLeft: '5px solid #1976d2' }}>
                                                    <CardContent>
                                                        <Typography color="textSecondary" variant="subtitle2">VOLTAGE {phase}</Typography>
                                                        <Typography variant="h4" sx={{ fontWeight: 'bold', mt: 1 }}>
                                                            {v.value !== '-' ? `${v.value} ${v.unit}` : '-'}
                                                        </Typography>
                                                    </CardContent>
                                                </Card>
                                            </Grid>
                                        );
                                    })}

                                    {/* Currents */}
                                    {['L1', 'L2', 'L3'].map((phase, idx) => {
                                        const c = getLiveValueObj(`Current L${idx + 1}`);
                                        return (
                                            <Grid size={{ xs: 12, sm: 4 }} key={`c-${phase}`}>
                                                <Card sx={{ borderLeft: '5px solid #4caf50' }}>
                                                    <CardContent>
                                                        <Typography color="textSecondary" variant="subtitle2">CURRENT {phase}</Typography>
                                                        <Typography variant="h4" sx={{ fontWeight: 'bold', mt: 1 }}>
                                                            {c.value !== '-' ? `${c.value} ${c.unit}` : '-'}
                                                        </Typography>
                                                    </CardContent>
                                                </Card>
                                            </Grid>
                                        );
                                    })}

                                    {/* Grid Frequency */}
                                    <Grid size={{ xs: 12, sm: 4 }}>
                                        <Card sx={{ borderLeft: '5px solid #ff9800' }}>
                                            <CardContent>
                                                <Typography color="textSecondary" variant="subtitle2">FREQUENCY</Typography>
                                                <Typography variant="h4" sx={{ fontWeight: 'bold', mt: 1 }}>
                                                    {getLiveValueObj('Frequency').value !== '-' ? `${getLiveValueObj('Frequency').value} Hz` : '-'}
                                                </Typography>
                                            </CardContent>
                                        </Card>
                                    </Grid>

                                    {/* Active Power */}
                                    <Grid size={{ xs: 12, sm: 4 }}>
                                        <Card sx={{ borderLeft: '5px solid #e91e63' }}>
                                            <CardContent>
                                                <Typography color="textSecondary" variant="subtitle2">ACTIVE POWER</Typography>
                                                <Typography variant="h4" sx={{ fontWeight: 'bold', mt: 1 }}>
                                                    {getLiveValueObj('Active Power').value !== '-' ? `${getLiveValueObj('Active Power').value} ${getLiveValueObj('Active Power').unit}` : '-'}
                                                </Typography>
                                            </CardContent>
                                        </Card>
                                    </Grid>

                                    {/* Power Factor */}
                                    <Grid size={{ xs: 12, sm: 4 }}>
                                        <Card sx={{ borderLeft: '5px solid #9c27b0' }}>
                                            <CardContent>
                                                <Typography color="textSecondary" variant="subtitle2">AVERAGE POWER FACTOR</Typography>
                                                <Typography variant="h4" sx={{ fontWeight: 'bold', mt: 1 }}>
                                                    {getLiveValueObj('Power Factor').value}
                                                </Typography>
                                            </CardContent>
                                        </Card>
                                    </Grid>
                                </Grid>
                            </Stack>
                        )}

                        {/* TAB 1: HISTORICAL LOGS */}
                        {activeTab === 1 && (
                            <Box>
                                {isVisible ? (
                                    <DeviceRTable
                                        rows={deviceLogArr}
                                        allParam={false}
                                        paramterString={selParamName}
                                    />
                                ) : (
                                    <Typography color="textSecondary" align="center" sx={{ py: 5 }}>
                                        Please use the search filter above to load historical parameter logs.
                                    </Typography>
                                )}
                            </Box>
                        )}

                        {/* TAB 2: LOAD & BILLING PROFILES */}
                        {activeTab === 2 && (
                            <Stack spacing={3}>
                                <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
                                    <FormControl sx={{ minWidth: 250 }}>
                                        <InputLabel>Profile Type</InputLabel>
                                        <Select
                                            value={profileObis}
                                            label="Profile Type"
                                            onChange={(e) => handleProfileObisChange(e.target.value as string)}
                                        >
                                            <MenuItem value="1.0.99.1.0.255">Load Profile (1.0.99.1.0.255)</MenuItem>
                                            <MenuItem value="1.0.98.1.0.255">Billing Profile (1.0.98.1.0.255)</MenuItem>
                                            <MenuItem value="0.0.99.98.0.255">Event Profile (0.0.99.98.0.255)</MenuItem>
                                        </Select>
                                    </FormControl>
                                </Box>

                                {profileRows.length > 0 ? (
                                    <TableContainer component={Paper} sx={{ maxHeight: 600 }}>
                                        <Table stickyHeader size="small">
                                            <TableHead>
                                                <TableRow>
                                                    {profileColumns.map((col) => (
                                                        <TableCell key={col} sx={{ fontWeight: 'bold' }}>{col}</TableCell>
                                                    ))}
                                                </TableRow>
                                            </TableHead>
                                            <TableBody>
                                                {profileRows.map((row, idx) => (
                                                    <TableRow key={idx} hover>
                                                        {profileColumns.map((col) => (
                                                            <TableCell key={col}>{row[col] ?? '-'}</TableCell>
                                                        ))}
                                                    </TableRow>
                                                ))}
                                            </TableBody>
                                        </Table>
                                    </TableContainer>
                                ) : (
                                    <Typography color="textSecondary" align="center" sx={{ py: 8 }}>
                                        No entries found for this profile type. Ensure the meter has been scanned.
                                    </Typography>
                                )}
                            </Stack>
                        )}

                        {/* TAB 3: DEVICE SETTINGS */}
                        {activeTab === 3 && (
                            <Grid container spacing={3}>
                                {/* Network IP & Port */}
                                <Grid size={{ xs: 12, md: 6 }}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant="h6" gutterBottom>IP & Ethernet Setup</Typography>
                                            <Divider sx={{ mb: 2 }} />
                                            <Table size="small">
                                                <TableBody>
                                                    {configData?.ip4?.length > 0 ? configData.ip4.map((item: any) => (
                                                        <TableRow key={item.name}>
                                                            <TableCell sx={{ fontWeight: 'medium' }}>{item.name}</TableCell>
                                                            <TableCell align="right">{item.value}</TableCell>
                                                        </TableRow>
                                                    )) : (
                                                        <TableRow>
                                                            <TableCell colSpan={2} align="center">No IP Configuration Scanned</TableCell>
                                                        </TableRow>
                                                    )}
                                                    {configData?.mac?.length > 0 && (
                                                        <TableRow>
                                                            <TableCell sx={{ fontWeight: 'medium' }}>MAC Address</TableCell>
                                                            <TableCell align="right">{configData.mac[0].value}</TableCell>
                                                        </TableRow>
                                                    )}
                                                </TableBody>
                                            </Table>
                                        </CardContent>
                                    </Card>
                                </Grid>

                                {/* HDLC Connection Details */}
                                <Grid size={{ xs: 12, md: 6 }}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant="h6" gutterBottom>IEC HDLC Setup</Typography>
                                            <Divider sx={{ mb: 2 }} />
                                            <Table size="small">
                                                <TableBody>
                                                    {configData?.hdlc?.length > 0 ? configData.hdlc.map((item: any) => (
                                                        <TableRow key={item.name}>
                                                            <TableCell sx={{ fontWeight: 'medium' }}>{item.name}</TableCell>
                                                            <TableCell align="right">{item.value}</TableCell>
                                                        </TableRow>
                                                    )) : (
                                                        <TableRow>
                                                            <TableCell colSpan={2} align="center">No HDLC Config Scanned</TableCell>
                                                        </TableRow>
                                                    )}
                                                </TableBody>
                                            </Table>
                                        </CardContent>
                                    </Card>
                                </Grid>

                                {/* TCP Port & MSS */}
                                <Grid size={{ xs: 12, md: 6 }}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant="h6" gutterBottom>TCP/UDP Setup</Typography>
                                            <Divider sx={{ mb: 2 }} />
                                            <Table size="small">
                                                <TableBody>
                                                    {configData?.tcp?.length > 0 ? configData.tcp.map((item: any) => (
                                                        <TableRow key={item.name}>
                                                            <TableCell sx={{ fontWeight: 'medium' }}>{item.name}</TableCell>
                                                            <TableCell align="right">{item.value}</TableCell>
                                                        </TableRow>
                                                    )) : (
                                                        <TableRow>
                                                            <TableCell colSpan={2} align="center">No TCP Config Scanned</TableCell>
                                                        </TableRow>
                                                    )}
                                                </TableBody>
                                            </Table>
                                        </CardContent>
                                    </Card>
                                </Grid>

                                {/* Tariff / Activity Calendar */}
                                <Grid size={{ xs: 12, md: 6 }}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant="h6" gutterBottom>Activity Calendar & Tariff</Typography>
                                            <Divider sx={{ mb: 2 }} />
                                            {latestCalendar ? (
                                                <Box>
                                                    <Typography variant="subtitle2" color="primary" sx={{ mb: 1 }}>
                                                        Calendar Name: {latestCalendar.name}
                                                    </Typography>
                                                    <Typography variant="body2" sx={{ whiteSpace: 'pre-line', p: 1, bgcolor: '#f5f5f5', borderRadius: 1 }}>
                                                        {latestCalendar.value}
                                                    </Typography>
                                                </Box>
                                            ) : (
                                                <Typography color="textSecondary" align="center">
                                                    No Activity Calendar/Tariff found
                                                </Typography>
                                            )}
                                        </CardContent>
                                    </Card>
                                </Grid>
                            </Grid>
                        )}
                    </Box>
                )}
            </Stack>
        </div>
    );
}
