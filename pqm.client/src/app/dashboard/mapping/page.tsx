'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import Snackbar, { SnackbarCloseReason } from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import TableContainer from '@mui/material/TableContainer';
import Table from '@mui/material/Table';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TableCell from '@mui/material/TableCell';
import TableBody from '@mui/material/TableBody';
import Paper from '@mui/material/Paper';
import Card from '@mui/material/Card';
import CardHeader from '@mui/material/CardHeader';
import CardContent from '@mui/material/CardContent';
import Divider from '@mui/material/Divider';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import TextField from '@mui/material/TextField';

import { DeviceFilters } from '@/components/dashboard/mapping/device-selection';
import { 
    fetchDevices, 
    fetchConnectedHeaders, 
    fetchDLMSObjects, 
    readDLMSObject 
} from '../../../api/device';
import { Device } from '../../../components/dashboard/device/devices-table';

export default function Page(): React.JSX.Element {
    const [devices, setDevices] = useState<Device[]>([]);
    const [selectedDeviceId, setSelectedDeviceId] = useState<string | number>(0);
    
    const [headers, setHeaders] = useState<any[]>([]);
    const [selectedHeaderId, setSelectedHeaderId] = useState<string | number>('');
    
    const [objects, setObjects] = useState<any[]>([]);
    const [selectedObjectId, setSelectedObjectId] = useState<string | number>('');

    const selectedHeader = headers.find((h: any) => h.id === selectedHeaderId);
    const isDataObjectType = selectedHeader?.name === 'Data' || selectedHeader?.name === 'iecHdlcSetup' || selectedHeader?.name === 'lecHdlcSetup' || selectedHeader?.name === 'TcpUdpSetup' || selectedHeader?.name === 'Ip4Setup' || selectedHeader?.name === 'MacAddressSetup' || selectedHeader?.name === 'AssociationLogicalName' || selectedHeader?.name === 'Clock' || selectedHeader?.name === 'ScriptTable' || selectedHeader?.name === 'ActionSchedule' || selectedHeader?.name === 'ActivityCalendar';
    const isExtendedRegisterType = selectedHeader?.name === 'ExtendedRegister';

    const [discoveredParams, setDiscoveredParams] = useState<any[]>([]);
    const [discovering, setDiscovering] = useState<boolean>(false);
    
    const [openSnackbar, setOpenSnackbar] = useState(false);
    const [displayMsg, setDisplayMsg] = useState<string | null>(null);

    // Object list modal states
    const [objectListOpen, setObjectListOpen] = useState(false);
    const [objectListData, setObjectListData] = useState<any[]>([]);
    const [objectListTitle, setObjectListTitle] = useState('');
    const [searchQuery, setSearchQuery] = useState('');

    // Context/Authentication modal states
    const [contextAuthOpen, setContextAuthOpen] = useState(false);
    const [contextAuthData, setContextAuthData] = useState<any>(null);
    const [contextAuthTitle, setContextAuthTitle] = useState('');

    // ProfileGeneric buffer table modal states
    const [pgTableOpen, setPgTableOpen] = useState(false);
    const [pgTableData, setPgTableData] = useState<any[]>([]);
    const [pgTableColumns, setPgTableColumns] = useState<string[]>([]);
    const [pgTableTitle, setPgTableTitle] = useState('');
    const [pgSearchQuery, setPgSearchQuery] = useState('');

    // ActivityCalendar modal states
    const [calendarDialogOpen, setCalendarDialogOpen] = useState(false);
    const [calendarData, setCalendarData] = useState<any>(null);
    const [calendarTitle, setCalendarTitle] = useState('');

    const getAttrValue = (attrs: any[] | undefined, attrId: number): string => {
        if (!attrs || !Array.isArray(attrs)) return 'Waiting...';
        const found = attrs.find((a: any) => a.attributeId === attrId);
        return found ? found.value : 'Waiting...';
    };

    const parseSeasonProfiles = (str: string) => {
        if (!str || str === 'None' || str === 'Waiting...') return [];
        return str.split('; ').map(s => {
            const parts = s.split(': ');
            const name = parts[0] || 'Unknown';
            const details = parts[1] || '';
            const startMatch = details.match(/Start=([^,]+)/);
            const weekMatch = details.match(/Week=(.+)/);
            return {
                name,
                start: startMatch ? startMatch[1] : 'N/A',
                week: weekMatch ? weekMatch[1] : 'N/A'
            };
        });
    };

    const parseWeekProfiles = (str: string) => {
        if (!str || str === 'None' || str === 'Waiting...') return [];
        return str.split('; ').map(s => {
            const parts = s.split(': ');
            const name = parts[0] || 'Unknown';
            const days = parts[1] ? parts[1].split(',') : [];
            return { name, days };
        });
    };

    const parseDayProfiles = (str: string) => {
        if (!str || str === 'None' || str === 'Waiting...') return [];
        return str.split('; ').map(s => {
            const parts = s.split(': ');
            const dayId = parts[0] || 'Day';
            let schedules: any[] = [];
            const schedStr = parts.slice(1).join(': ');
            if (schedStr.startsWith('[') && schedStr.endsWith(']')) {
                const inner = schedStr.slice(1, -1);
                if (inner) {
                    schedules = inner.split(', ').map(item => {
                        const colonIdx = item.indexOf(': ');
                        if (colonIdx > 0) {
                            const time = item.substring(0, colonIdx);
                            const rest = item.substring(colonIdx + 2);
                            const hashIdx = rest.lastIndexOf(' #');
                            if (hashIdx > 0) {
                                return {
                                    time,
                                    script: rest.substring(0, hashIdx),
                                    selector: rest.substring(hashIdx + 2)
                                };
                            }
                            return { time, script: rest, selector: '' };
                        }
                        return { time: item, script: 'Unknown', selector: '' };
                    });
                }
            }
            return { dayId, schedules };
        });
    };

    const handleOpenPgTable = (jsonStr: string, title: string) => {
        try {
            const rows = JSON.parse(jsonStr) as any[];
            const cols = rows.length > 0 ? Object.keys(rows[0]) : [];
            setPgTableData(rows);
            setPgTableColumns(cols);
            setPgTableTitle(title);
            setPgSearchQuery('');
            setPgTableOpen(true);
        } catch (e) {
            console.error('Failed to parse ProfileGeneric buffer json:', e);
        }
    };

    const handleOpenContextOrAuth = (jsonStr: string, title: string) => {
        try {
            const data = JSON.parse(jsonStr);
            setContextAuthData(data);
            setContextAuthTitle(title);
            setContextAuthOpen(true);
        } catch (e) {
            console.error('Failed to parse context/auth json:', e);
        }
    };

    useEffect(() => {
        const loadDevices = async () => {
            try {
                const fetchedDevices = await fetchDevices();
                setDevices(fetchedDevices);
            } catch (error) {
                console.error('Failed to fetch devices:', error);
            }
        };
        loadDevices();
    }, []);

    const handleDeviceSelection = async (id: string | number) => {
        setSelectedDeviceId(id);
        setSelectedHeaderId('');
        setObjects([]);
        setSelectedObjectId('');
        setDiscoveredParams([]);
        
        if (Number(id) > 0) {
            try {
                const result = await fetchConnectedHeaders(id);
                if (result && result.status) {
                    setHeaders(result.data);
                    
                    // Auto-select "Register" header if present
                    const regHeader = result.data.find((h: any) => h.name === 'Register');
                    if (regHeader) {
                        setSelectedHeaderId(regHeader.id);
                        handleHeaderSelection(regHeader.id);
                    }
                }
            } catch (error) {
                console.error('Failed to fetch connected headers:', error);
            }
        } else {
            setHeaders([]);
        }
    };

    const handleHeaderSelection = async (headerId: string | number) => {
        setSelectedHeaderId(headerId);
        setObjects([]);
        setSelectedObjectId('');
        setDiscoveredParams([]);
        
        try {
            const result = await fetchDLMSObjects(headerId);
            if (result && result.status) {
                setObjects(result.data);
                // Map to table rows
                let mapped = result.data.map((obj: any) => ({
                    id: obj.id,
                    name: `${obj.obisCode} ${obj.name}`,
                    obisCode: obj.obisCode,
                    objectType: obj.objectType,
                    value: obj.attribute2 || 'Waiting...',
                    attribute3: obj.attribute3 || 'Waiting...',
                    allAttributes: obj.allAttributes || []
                }));

                // Check header name to filter for AssociationLogicalName parameters
                const currentHeader = headers.find((h: any) => h.id === headerId);
                if (currentHeader?.name === 'AssociationLogicalName') {
                    const allowedSubstrings = [
                        'association status',
                        'object list',
                        'associated partners id',
                        'application context name',
                        'authentication mechanism name'
                    ];
                    mapped = mapped.filter((p: any) => {
                        const nameLower = p.name?.toLowerCase() || '';
                        return allowedSubstrings.some(allowed => nameLower.includes(allowed));
                    });
                }

                setDiscoveredParams(mapped);
            }
        } catch (error) {
            console.error('Failed to fetch DLMS objects:', error);
        }
    };

    const handleObjectSelection = (objectId: string | number) => {
        setSelectedObjectId(objectId);
    };

    const handleDiscoverParameters = async () => {
        if (!selectedDeviceId || discoveredParams.length === 0) return;
        setDiscovering(true);
        try {
            for (let i = 0; i < discoveredParams.length; i++) {
                const param = discoveredParams[i];
                
                // Update row value to Scanning...
                setDiscoveredParams((prev) => {
                    const next = [...prev];
                    next[i] = { ...next[i], value: 'Scanning...' };
                    return next;
                });

                // Call backend read-object API
                const result = await readDLMSObject(selectedDeviceId, param.id);

                // Update row with returned values
                setDiscoveredParams((prev) => {
                    const next = [...prev];
                    if (result && result.status && Array.isArray(result.data)) {
                        const isRegister = param.objectType?.toLowerCase().includes('register');
                        const isProfileGeneric = param.objectType?.toLowerCase() === 'profilegeneric';
                        const isExtReg = param.objectType?.toLowerCase() === 'extendedregister';
                        const valItem = (isRegister || isProfileGeneric)
                            ? result.data.find((item: any) => item.attributeId === 2)
                            : result.data[0];

                        const unitItem = (isRegister || isProfileGeneric)
                            ? result.data.find((item: any) => item.attributeId === 3)
                            : null;

                        const statusItem = isExtReg
                            ? result.data.find((item: any) => item.attributeId === 4)
                            : null;

                        const captureItem = isExtReg
                            ? result.data.find((item: any) => item.attributeId === 5)
                            : null;

                        next[i] = {
                            ...next[i],
                            value: valItem ? valItem.value : 'Error',
                            attribute3: unitItem ? unitItem.value : next[i].attribute3,
                            attribute4: statusItem ? statusItem.value : next[i].attribute4,
                            attribute5: captureItem ? captureItem.value : next[i].attribute5,
                            allAttributes: result.data || []
                        };
                    } else {
                        next[i] = { ...next[i], value: 'Error' };
                    }
                    return next;
                });
            }
            setDisplayMsg('Scan completed successfully.');
        } catch (error) {
            console.error('Scan error:', error);
            setDisplayMsg('Failed to read parameters due to an unexpected error.');
        } finally {
            setDiscovering(false);
            setOpenSnackbar(true);
        }
    };

    const handleSnackBarClose = (
        event?: React.SyntheticEvent | Event,
        reason?: SnackbarCloseReason,
    ) => {
        if (reason === 'clickaway') return;
        setOpenSnackbar(false);
    };

    return (
        <Stack spacing={3}>
            <div>
                <Typography variant="h4">Device Mapping</Typography>
            </div>
            <DeviceFilters 
                rows={devices} 
                onDeviceSelect={handleDeviceSelection} 
                headers={headers}
                selectedHeaderId={selectedHeaderId}
                onHeaderSelect={handleHeaderSelection}
                objects={objects}
                selectedObjectId={selectedObjectId}
                onObjectSelect={handleObjectSelection}
            />
            
            {Number(selectedDeviceId) > 0 && discoveredParams.length > 0 && (
                <Stack direction="row" spacing={2} alignItems="center">
                    <Button 
                        variant="contained" 
                        color="secondary" 
                        onClick={handleDiscoverParameters}
                        disabled={discovering}
                    >
                        {discovering ? 'Reading Meter...' : 'Scan & Discover Meter Parameters'}
                    </Button>
                    {discovering && <CircularProgress size={24} />}
                </Stack>
            )}

            {discoveredParams.length > 0 && (
                <Card>
                    <CardHeader title="Discovered Meter Parameters (Current Values)" />
                    <Divider />
                    <CardContent>
                        <TableContainer component={Paper} sx={{ maxHeight: 400 }}>
                            <Table stickyHeader aria-label="discovered parameters table">
                                <TableHead>
                                    <TableRow>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Name</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Object Type</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Attribute 2</TableCell>
                                        {!isDataObjectType && <TableCell sx={{ fontWeight: 'bold' }}>Attribute 3</TableCell>}
                                        {isExtendedRegisterType && <TableCell sx={{ fontWeight: 'bold' }}>Status</TableCell>}
                                        {isExtendedRegisterType && <TableCell sx={{ fontWeight: 'bold' }}>Capture Time</TableCell>}
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {discoveredParams.map((row, idx) => {
                                        const isJsonObjectList = row.name?.toLowerCase().includes('object list') && row.value?.startsWith('[') && row.value?.endsWith(']');
                                        const isContextOrAuthJson = row.value?.startsWith('{') && row.value?.endsWith('}') && (
                                            row.name?.toLowerCase().includes('context name') || 
                                            row.name?.toLowerCase().includes('mechanism name')
                                        );
                                        const isProfileGenericBuffer = row.objectType?.toLowerCase() === 'profilegeneric' && row.value?.startsWith('[') && row.value?.endsWith(']');
                                        const isActivityCalendar = row.objectType?.toLowerCase() === 'activitycalendar';
                                        return (
                                            <TableRow key={idx} hover>
                                                <TableCell>{row.name}</TableCell>
                                                <TableCell>{row.objectType}</TableCell>
                                                <TableCell sx={{ fontWeight: 'bold', color: 'primary.main' }}>
                                                    {isJsonObjectList ? (
                                                        (() => {
                                                            try {
                                                                const list = JSON.parse(row.value);
                                                                return (
                                                                    <Button
                                                                        variant="outlined"
                                                                        size="small"
                                                                        color="primary"
                                                                        onClick={() => {
                                                                            setObjectListData(list);
                                                                            setObjectListTitle(row.name);
                                                                            setObjectListOpen(true);
                                                                            setSearchQuery('');
                                                                        }}
                                                                    >
                                                                        View Decoded List ({list.length} Objects)
                                                                    </Button>
                                                                );
                                                            } catch (e) {
                                                                return <span>{row.value}</span>;
                                                            }
                                                        })()
                                                    ) : isProfileGenericBuffer ? (
                                                        (() => {
                                                            try {
                                                                const rows2 = JSON.parse(row.value);
                                                                return (
                                                                    <Button
                                                                        variant="outlined"
                                                                        size="small"
                                                                        color="secondary"
                                                                        onClick={() => handleOpenPgTable(row.value, row.name)}
                                                                    >
                                                                        View Table ({rows2.length} Rows)
                                                                    </Button>
                                                                );
                                                            } catch (e) {
                                                                return <span>{row.value}</span>;
                                                            }
                                                        })()
                                                    ) : isActivityCalendar ? (
                                                         <Button
                                                             variant="outlined"
                                                             size="small"
                                                             color="success"
                                                             onClick={() => {
                                                                 setCalendarData(row);
                                                                 setCalendarTitle(row.name);
                                                                 setCalendarDialogOpen(true);
                                                             }}
                                                         >
                                                             View Calendar Config
                                                         </Button>
                                                     ) : isContextOrAuthJson ? (
                                                        <Button
                                                            variant="outlined"
                                                            size="small"
                                                            color="primary"
                                                            onClick={() => handleOpenContextOrAuth(row.value, row.name)}
                                                        >
                                                            View Details
                                                        </Button>
                                                    ) : (
                                                        row.value?.startsWith('Error') ? (
                                                            <span style={{ color: '#d32f2f', fontWeight: 'normal' }}>
                                                                {row.value.replace('Error: ', '')}
                                                            </span>
                                                        ) : (
                                                            row.value || 'N/A'
                                                        )
                                                    )}
                                                </TableCell>
                                                {!isDataObjectType && (
                                                    <TableCell sx={{ color: 'text.secondary', fontFamily: 'monospace' }}>
                                                        {row.attribute3 || 'N/A'}
                                                    </TableCell>
                                                )}
                                                {isExtendedRegisterType && (
                                                    <TableCell sx={{ color: 'text.secondary', fontFamily: 'monospace' }}>
                                                        {row.attribute4 || 'N/A'}
                                                    </TableCell>
                                                )}
                                                {isExtendedRegisterType && (
                                                    <TableCell sx={{ color: 'text.secondary', fontFamily: 'monospace' }}>
                                                        {row.attribute5 || 'N/A'}
                                                    </TableCell>
                                                )}
                                            </TableRow>
                                        );
                                    })}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </CardContent>
                </Card>
            )}

            {/* Object List Dialog Modal */}
            <Dialog 
                open={objectListOpen} 
                onClose={() => setObjectListOpen(false)}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle sx={{ fontWeight: 'bold', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                        Decoded {objectListTitle}
                    </Typography>
                    <Typography variant="subtitle2" color="text.secondary">
                        {objectListData.length} Total Objects
                    </Typography>
                </DialogTitle>
                <Divider />
                <DialogContent>
                    <Stack spacing={2} sx={{ mt: 1 }}>
                        <TextField
                            placeholder="Search objects (Class ID, OBIS, Access)..."
                            variant="outlined"
                            size="small"
                            fullWidth
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                        />
                        <TableContainer component={Paper} sx={{ maxHeight: 450 }}>
                            <Table stickyHeader size="small">
                                <TableHead>
                                    <TableRow>
                                        <TableCell sx={{ fontWeight: 'bold', backgroundColor: 'background.paper' }}>Class ID</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold', backgroundColor: 'background.paper' }}>Version</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold', backgroundColor: 'background.paper' }}>Logical Name</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold', backgroundColor: 'background.paper' }}>Attribute Access</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold', backgroundColor: 'background.paper' }}>Method Access</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {(() => {
                                        const query = searchQuery.toLowerCase();
                                        const filtered = objectListData.filter((item: any) => 
                                            item.ClassId?.toLowerCase().includes(query) ||
                                            item.LogicalName?.toLowerCase().includes(query) ||
                                            item.AttributeAccess?.toLowerCase().includes(query) ||
                                            item.MethodAccess?.toLowerCase().includes(query)
                                        );
                                        
                                        if (filtered.length > 0) {
                                            return filtered.map((item: any, idx: number) => (
                                                <TableRow key={idx} hover>
                                                    <TableCell sx={{ fontWeight: 'medium' }}>{item.ClassId}</TableCell>
                                                    <TableCell>{item.Version}</TableCell>
                                                    <TableCell sx={{ fontFamily: 'monospace' }}>{item.LogicalName}</TableCell>
                                                    <TableCell sx={{ fontSize: '0.825rem' }}>{item.AttributeAccess}</TableCell>
                                                    <TableCell sx={{ fontSize: '0.825rem' }}>{item.MethodAccess}</TableCell>
                                                </TableRow>
                                            ));
                                        } else {
                                            return (
                                                <TableRow>
                                                    <TableCell colSpan={5} align="center" sx={{ py: 3, color: 'text.secondary' }}>
                                                        No objects match the search query.
                                                    </TableCell>
                                                </TableRow>
                                            );
                                        }
                                    })()}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </Stack>
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={() => setObjectListOpen(false)} variant="contained" color="primary">
                        Close
                    </Button>
                </DialogActions>
            </Dialog>

            {/* ProfileGeneric Buffer Table Dialog */}
            <Dialog
                open={pgTableOpen}
                onClose={() => setPgTableOpen(false)}
                maxWidth="lg"
                fullWidth
            >
                <DialogTitle sx={{ fontWeight: 'bold', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                        {pgTableTitle} — Buffer
                    </Typography>
                    <Typography variant="subtitle2" color="text.secondary">
                        {pgTableData.length} Rows
                    </Typography>
                </DialogTitle>
                <Divider />
                <DialogContent>
                    <Stack spacing={2} sx={{ mt: 1 }}>
                        <TextField
                            placeholder="Search buffer rows..."
                            variant="outlined"
                            size="small"
                            fullWidth
                            value={pgSearchQuery}
                            onChange={(e) => setPgSearchQuery(e.target.value)}
                        />
                        <TableContainer component={Paper} sx={{ maxHeight: 450 }}>
                            <Table stickyHeader size="small">
                                <TableHead>
                                    <TableRow>
                                        {pgTableColumns.map((col, ci) => (
                                            <TableCell key={ci} sx={{ fontWeight: 'bold', backgroundColor: 'background.paper', whiteSpace: 'nowrap' }}>
                                                {col}
                                            </TableCell>
                                        ))}
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {(() => {
                                        const query = pgSearchQuery.toLowerCase();
                                        const filtered = pgTableData.filter((row: any) =>
                                            pgTableColumns.some(col =>
                                                String(row[col] ?? '').toLowerCase().includes(query)
                                            )
                                        );
                                        if (filtered.length > 0) {
                                            return filtered.map((row: any, ri: number) => (
                                                <TableRow key={ri} hover>
                                                    {pgTableColumns.map((col, ci) => (
                                                        <TableCell key={ci} sx={{ fontFamily: 'monospace', fontSize: '0.8rem', whiteSpace: 'nowrap' }}>
                                                            {String(row[col] ?? '')}
                                                        </TableCell>
                                                    ))}
                                                </TableRow>
                                            ));
                                        } else {
                                            return (
                                                <TableRow>
                                                    <TableCell colSpan={pgTableColumns.length || 1} align="center" sx={{ py: 3, color: 'text.secondary' }}>
                                                        {pgTableData.length === 0 ? 'No data in buffer.' : 'No rows match the search query.'}
                                                    </TableCell>
                                                </TableRow>
                                            );
                                        }
                                    })()}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </Stack>
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={() => setPgTableOpen(false)} variant="contained" color="primary">
                        Close
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Context/Authentication Name Dialog Modal */}
            <Dialog 
                open={contextAuthOpen} 
                onClose={() => setContextAuthOpen(false)}
                maxWidth="xs"
                fullWidth
            >
                <DialogTitle sx={{ fontWeight: 'bold' }}>
                    {contextAuthTitle} Details
                </DialogTitle>
                <Divider />
                <DialogContent>
                    {contextAuthData && (
                        <TableContainer component={Paper} sx={{ mt: 1 }}>
                            <Table size="small">
                                <TableBody>
                                    {Object.entries(contextAuthData).map(([key, val]) => {
                                        const label = key
                                            .replace(/([A-Z])/g, ' $1')
                                            .trim()
                                            .replace('Joint Iso Ctt', 'Joint ISO CTT')
                                            .replace('Dlms U A', 'DLMS UA')
                                            .replace('Context Id', 'Context ID')
                                            .replace('Mechanism Id', 'Mechanism ID')
                                            .replace('Country Name', 'Country name')
                                            .replace(/^Enabled$/, 'Daylight Savings Enabled')
                                            .replace(/^Deviation$/, 'Daylight Savings Deviation')
                                            .replace(/^Begin$/, 'Daylight Savings Begin')
                                            .replace(/^End$/, 'Daylight Savings End');
                                        return (
                                            <TableRow key={key}>
                                                <TableCell sx={{ fontWeight: 'bold', width: '60%' }}>{label}</TableCell>
                                                <TableCell>{String(val)}</TableCell>
                                            </TableRow>
                                        );
                                    })}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    )}
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={() => setContextAuthOpen(false)} variant="contained" color="primary">
                        Close
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Activity Calendar Profiles Dialog Modal */}
            {(() => {
                const activeName = calendarData ? getAttrValue(calendarData.allAttributes, 2) : '';
                const activeSeasons = calendarData ? parseSeasonProfiles(getAttrValue(calendarData.allAttributes, 3)) : [];
                const activeWeeks = calendarData ? parseWeekProfiles(getAttrValue(calendarData.allAttributes, 4)) : [];
                const activeDays = calendarData ? parseDayProfiles(getAttrValue(calendarData.allAttributes, 5)) : [];

                const passiveName = calendarData ? getAttrValue(calendarData.allAttributes, 6) : '';
                const passiveSeasons = calendarData ? parseSeasonProfiles(getAttrValue(calendarData.allAttributes, 7)) : [];
                const passiveWeeks = calendarData ? parseWeekProfiles(getAttrValue(calendarData.allAttributes, 8)) : [];
                const passiveDays = calendarData ? parseDayProfiles(getAttrValue(calendarData.allAttributes, 9)) : [];
                const passiveActivationTime = calendarData ? getAttrValue(calendarData.allAttributes, 10) : '';

                return (
                    <Dialog
                        open={calendarDialogOpen}
                        onClose={() => setCalendarDialogOpen(false)}
                        maxWidth="lg"
                        fullWidth
                    >
                        <DialogTitle sx={{ fontWeight: 'bold', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                                Activity Calendar Profiles: {calendarTitle}
                            </Typography>
                            {passiveActivationTime && passiveActivationTime !== 'Waiting...' && passiveActivationTime !== 'N/A' && (
                                <Alert severity="info" icon={false} sx={{ py: 0, px: 2, display: 'flex', alignItems: 'center' }}>
                                    Upcoming Activation Time: <strong>{passiveActivationTime}</strong>
                                </Alert>
                            )}
                        </DialogTitle>
                        <Divider />
                        <DialogContent>
                            <Stack spacing={3} sx={{ mt: 1 }}>
                                
                                {passiveActivationTime && passiveActivationTime !== 'Waiting...' && passiveActivationTime !== 'N/A' && (
                                    <Alert severity="warning" sx={{ borderLeft: '4px solid #ed6c02', borderRadius: '4px' }}>
                                        <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 0.5 }}>
                                            Upcoming Calendar Transition Scheduled
                                        </Typography>
                                        Passive calendar <strong>"{passiveName || '(unnamed)'}"</strong> is scheduled to activate at <strong>{passiveActivationTime}</strong>, replacing the active calendar config.
                                    </Alert>
                                )}

                                <Stack direction={{ xs: 'column', md: 'row' }} spacing={3}>
                                    
                                    {/* Active Calendar Card */}
                                    <Card sx={{ flex: 1, borderTop: '4px solid #2e7d32', boxShadow: 2 }}>
                                        <CardHeader 
                                            title="Active Calendar Config" 
                                            subheader={activeName ? `Name: ${activeName}` : 'No active calendar name'}
                                            titleTypographyProps={{ variant: 'h6', fontWeight: 'bold', color: 'success.main' }}
                                        />
                                        <Divider />
                                        <CardContent>
                                            <Stack spacing={3}>
                                                
                                                {/* Season Profiles */}
                                                <div>
                                                    <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1, color: 'text.primary' }}>
                                                        Active Season Profiles
                                                    </Typography>
                                                    {activeSeasons.length > 0 ? (
                                                        <TableContainer component={Paper} variant="outlined">
                                                            <Table size="small">
                                                                <TableHead>
                                                                    <TableRow>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Season</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Start Time</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Week Name</TableCell>
                                                                    </TableRow>
                                                                </TableHead>
                                                                <TableBody>
                                                                    {activeSeasons.map((s, idx) => (
                                                                        <TableRow key={idx}>
                                                                            <TableCell sx={{ fontWeight: 'medium' }}>{s.name}</TableCell>
                                                                            <TableCell>{s.start}</TableCell>
                                                                            <TableCell sx={{ fontFamily: 'monospace' }}>{s.week}</TableCell>
                                                                        </TableRow>
                                                                    ))}
                                                                </TableBody>
                                                            </Table>
                                                        </TableContainer>
                                                    ) : (
                                                        <Typography variant="body2" color="text.secondary">None configured.</Typography>
                                                    )}
                                                </div>

                                                {/* Week Profiles */}
                                                <div>
                                                    <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1, color: 'text.primary' }}>
                                                        Active Week Profiles
                                                    </Typography>
                                                    {activeWeeks.length > 0 ? (
                                                        <TableContainer component={Paper} variant="outlined">
                                                            <Table size="small">
                                                                <TableHead>
                                                                    <TableRow>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Week</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Mon-Sun Days Mapping</TableCell>
                                                                    </TableRow>
                                                                </TableHead>
                                                                <TableBody>
                                                                    {activeWeeks.map((w, idx) => (
                                                                        <TableRow key={idx}>
                                                                            <TableCell sx={{ fontWeight: 'medium' }}>{w.name}</TableCell>
                                                                            <TableCell sx={{ fontSize: '0.8rem', fontFamily: 'monospace' }}>
                                                                                {w.days.join(' → ')}
                                                                            </TableCell>
                                                                        </TableRow>
                                                                    ))}
                                                                </TableBody>
                                                            </Table>
                                                        </TableContainer>
                                                    ) : (
                                                        <Typography variant="body2" color="text.secondary">None configured.</Typography>
                                                    )}
                                                </div>

                                                {/* Day Profiles */}
                                                <div>
                                                    <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1, color: 'text.primary' }}>
                                                        Active Day Schedules
                                                    </Typography>
                                                    {activeDays.length > 0 ? (
                                                        <Stack spacing={1.5}>
                                                            {activeDays.map((d, idx) => (
                                                                <Card key={idx} variant="outlined" sx={{ bgcolor: 'action.hover' }}>
                                                                    <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
                                                                        <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 1 }}>
                                                                            {d.dayId}
                                                                        </Typography>
                                                                        {d.schedules.length > 0 ? (
                                                                            <Stack spacing={1}>
                                                                                {d.schedules.map((s, si) => (
                                                                                    <Stack key={si} direction="row" justifyContent="space-between" sx={{ fontSize: '0.8rem' }}>
                                                                                        <span style={{ fontWeight: 'bold' }}>{s.time}</span>
                                                                                        <span style={{ fontFamily: 'monospace', color: 'text.secondary' }}>Script: {s.script} #{s.selector}</span>
                                                                                    </Stack>
                                                                                ))}
                                                                            </Stack>
                                                                        ) : (
                                                                            <Typography variant="caption" color="text.secondary">No schedules</Typography>
                                                                        )}
                                                                    </CardContent>
                                                                </Card>
                                                            ))}
                                                        </Stack>
                                                    ) : (
                                                        <Typography variant="body2" color="text.secondary">None configured.</Typography>
                                                    )}
                                                </div>

                                            </Stack>
                                        </CardContent>
                                    </Card>

                                    {/* Passive Calendar Card */}
                                    <Card sx={{ flex: 1, borderTop: '4px solid #ed6c02', boxShadow: 2 }}>
                                        <CardHeader 
                                            title="Passive Calendar Config (Upcoming)" 
                                            subheader={passiveName ? `Name: ${passiveName}` : 'No passive calendar name'}
                                            titleTypographyProps={{ variant: 'h6', fontWeight: 'bold', color: 'warning.main' }}
                                        />
                                        <Divider />
                                        <CardContent>
                                            <Stack spacing={3}>
                                                
                                                {/* Season Profiles */}
                                                <div>
                                                    <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1, color: 'text.primary' }}>
                                                        Passive Season Profiles
                                                    </Typography>
                                                    {passiveSeasons.length > 0 ? (
                                                        <TableContainer component={Paper} variant="outlined">
                                                            <Table size="small">
                                                                <TableHead>
                                                                    <TableRow>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Season</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Start Time</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Week Name</TableCell>
                                                                    </TableRow>
                                                                </TableHead>
                                                                <TableBody>
                                                                    {passiveSeasons.map((s, idx) => (
                                                                        <TableRow key={idx}>
                                                                            <TableCell sx={{ fontWeight: 'medium' }}>{s.name}</TableCell>
                                                                            <TableCell>{s.start}</TableCell>
                                                                            <TableCell sx={{ fontFamily: 'monospace' }}>{s.week}</TableCell>
                                                                        </TableRow>
                                                                    ))}
                                                                </TableBody>
                                                            </Table>
                                                        </TableContainer>
                                                    ) : (
                                                        <Typography variant="body2" color="text.secondary">None configured.</Typography>
                                                    )}
                                                </div>

                                                {/* Week Profiles */}
                                                <div>
                                                    <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1, color: 'text.primary' }}>
                                                        Passive Week Profiles
                                                    </Typography>
                                                    {passiveWeeks.length > 0 ? (
                                                        <TableContainer component={Paper} variant="outlined">
                                                            <Table size="small">
                                                                <TableHead>
                                                                    <TableRow>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Week</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold' }}>Mon-Sun Days Mapping</TableCell>
                                                                    </TableRow>
                                                                </TableHead>
                                                                <TableBody>
                                                                    {passiveWeeks.map((w, idx) => (
                                                                        <TableRow key={idx}>
                                                                            <TableCell sx={{ fontWeight: 'medium' }}>{w.name}</TableCell>
                                                                            <TableCell sx={{ fontSize: '0.8rem', fontFamily: 'monospace' }}>
                                                                                {w.days.join(' → ')}
                                                                            </TableCell>
                                                                        </TableRow>
                                                                    ))}
                                                                </TableBody>
                                                            </Table>
                                                        </TableContainer>
                                                    ) : (
                                                        <Typography variant="body2" color="text.secondary">None configured.</Typography>
                                                    )}
                                                </div>

                                                {/* Day Profiles */}
                                                <div>
                                                    <Typography variant="subtitle2" sx={{ fontWeight: 'bold', mb: 1, color: 'text.primary' }}>
                                                        Passive Day Schedules
                                                    </Typography>
                                                    {passiveDays.length > 0 ? (
                                                        <Stack spacing={1.5}>
                                                            {passiveDays.map((d, idx) => (
                                                                <Card key={idx} variant="outlined" sx={{ bgcolor: 'action.hover' }}>
                                                                    <CardContent sx={{ p: 1.5, '&:last-child': { pb: 1.5 } }}>
                                                                        <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 1 }}>
                                                                            {d.dayId}
                                                                        </Typography>
                                                                        {d.schedules.length > 0 ? (
                                                                            <Stack spacing={1}>
                                                                                {d.schedules.map((s, si) => (
                                                                                    <Stack key={si} direction="row" justifyContent="space-between" sx={{ fontSize: '0.8rem' }}>
                                                                                        <span style={{ fontWeight: 'bold' }}>{s.time}</span>
                                                                                        <span style={{ fontFamily: 'monospace', color: 'text.secondary' }}>Script: {s.script} #{s.selector}</span>
                                                                                    </Stack>
                                                                                ))}
                                                                            </Stack>
                                                                        ) : (
                                                                            <Typography variant="caption" color="text.secondary">No schedules</Typography>
                                                                        )}
                                                                    </CardContent>
                                                                </Card>
                                                            ))}
                                                        </Stack>
                                                    ) : (
                                                        <Typography variant="body2" color="text.secondary">None configured.</Typography>
                                                    )}
                                                </div>

                                            </Stack>
                                        </CardContent>
                                    </Card>

                                </Stack>

                            </Stack>
                        </DialogContent>
                        <Divider />
                        <DialogActions>
                            <Button onClick={() => setCalendarDialogOpen(false)} variant="contained" color="primary">
                                Close
                            </Button>
                        </DialogActions>
                    </Dialog>
                );
            })()}

            <Snackbar
                open={openSnackbar}
                autoHideDuration={6000}
                onClose={handleSnackBarClose}
                anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
            >
                <Alert
                    severity={displayMsg?.includes('successful') || displayMsg?.includes('Successfully') ? 'success' : 'error'}
                    sx={{ width: '100%' }}
                    onClose={handleSnackBarClose}
                    variant="filled"
                >
                    {displayMsg}
                </Alert>
            </Snackbar>
        </Stack>
    );
}
