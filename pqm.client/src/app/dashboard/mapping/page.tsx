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
import Tabs from '@mui/material/Tabs';
import Tab from '@mui/material/Tab';
import Box from '@mui/material/Box';
import Grid from '@mui/material/Grid';
import FormControlLabel from '@mui/material/FormControlLabel';
import Checkbox from '@mui/material/Checkbox';
import Select from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';

const CLOCK_ATTRIBUTES = [
    { index: 1, name: 'Logical Name', type: 'OctetString' },
    { index: 2, name: 'Time', type: 'OctetString' },
    { index: 3, name: 'Time Zone', type: 'Int16' },
    { index: 4, name: 'Status', type: 'UInt8' },
    { index: 5, name: 'Begin', type: 'OctetString' },
    { index: 6, name: 'End', type: 'OctetString' },
    { index: 7, name: 'Deviation', type: 'Int8' },
    { index: 8, name: 'Enabled', type: 'Boolean' },
    { index: 9, name: 'Clock Base', type: 'Enum' },
];

const CLOCK_METHODS = [
    { index: 1, name: 'Adjust to quarter' },
    { index: 2, name: 'Adjust to measuring period' },
    { index: 3, name: 'Adjust to minute' },
    { index: 4, name: 'Adjust to preset time' },
    { index: 5, name: 'Preset adjusting time' },
    { index: 6, name: 'Shift time' },
];

const parseClockStatus = (statusVal: string | null | undefined) => {
    if (!statusVal || statusVal === 'Waiting...' || statusVal === 'Scanning...') {
        return {
            invalidValue: false,
            doubtfulValue: false,
            differentClockBase: false,
            invalidClockStatus: false,
            daylightSavingActive: false
        };
    }
    
    const valLower = statusVal.toLowerCase();
    
    // If it's a number, parse as bitmask
    const numVal = parseInt(statusVal, 10);
    if (!isNaN(numVal)) {
        return {
            invalidValue: (numVal & 1) !== 0,
            doubtfulValue: (numVal & 2) !== 0,
            differentClockBase: (numVal & 4) !== 0,
            invalidClockStatus: (numVal & 8) !== 0,
            daylightSavingActive: (numVal & 128) !== 0
        };
    }
    
    // If it's a comma-separated enum string
    return {
        invalidValue: valLower.includes('invalidvalue'),
        doubtfulValue: valLower.includes('doubtfulvalue'),
        differentClockBase: valLower.includes('differentclockbase'),
        invalidClockStatus: valLower.includes('invalidclockstatus'),
        daylightSavingActive: valLower.includes('daylightsavingactive')
    };
};

const getClockBaseDisplay = (baseVal: string | null | undefined) => {
    if (!baseVal || baseVal === 'Waiting...' || baseVal === 'Scanning...') return baseVal || 'Waiting...';
    const valLower = baseVal.toLowerCase();
    if (valLower === '1' || valLower === 'crystal') return 'Crystal';
    if (valLower === '2' || valLower === 'mainsfrequency' || valLower === 'mains frequency') return 'MainsFrequency';
    if (valLower === '3' || valLower === 'gps') return 'GPS';
    if (valLower === '4' || valLower === 'radio') return 'Radio';
    return baseVal;
};

const METHOD_DESCRIPTIONS: Record<string, string[]> = {
    'clock': [
        'Adjust to quarter',
        'Adjust to measuring period',
        'Adjust to minute',
        'Adjust to preset time',
        'Preset adjusting time',
        'Shift time'
    ],
    'scripttable': [
        'Execute'
    ],
    'associationlogicalname': [
        'Reply to connect',
        'Disconnect',
        'Update secret',
        'Add object',
        'Remove object'
    ],
    'activitycalendar': [
        'Activate passive calendar'
    ],
    'profilegeneric': [
        'Reset',
        'Capture'
    ],
    'actionschedule': [
        'Reset'
    ],
    'register': [
        'Reset'
    ],
    'extendedregister': [
        'Reset'
    ],
    'demandregister': [
        'Reset'
    ]
};

const ATTRIBUTE_TYPES: Record<string, string[]> = {
    'clock': [
        'OctetString', 'OctetString', 'Int16', 'UInt8', 'OctetString', 'OctetString', 'Int8', 'Boolean', 'Enum'
    ],
    'scripttable': [
        'OctetString', 'Array'
    ],
    'actionschedule': [
        'OctetString', 'UInt16', 'UInt8', 'OctetString', 'OctetString'
    ],
    'activitycalendar': [
        'OctetString', 'OctetString', 'Array', 'Array', 'Array', 'OctetString'
    ],
    'associationlogicalname': [
        'OctetString', 'Array', 'OctetString', 'Structure', 'Enum', 'OctetString'
    ],
    'iechdlcsetup': [
        'OctetString', 'Enum', 'Enum', 'UInt8', 'UInt8', 'UInt16', 'UInt16', 'UInt32', 'UInt16'
    ],
    'lechdlcsetup': [
        'OctetString', 'Enum', 'Enum', 'UInt8', 'UInt8', 'UInt16', 'UInt16', 'UInt32', 'UInt16'
    ],
    'tcpudpsetup': [
        'OctetString', 'UInt16', 'OctetString'
    ],
    'ip4setup': [
        'OctetString', 'OctetString', 'Array', 'OctetString', 'OctetString', 'UInt16'
    ],
    'macaddresssetup': [
        'OctetString', 'OctetString'
    ]
};

const getAttributeDescription = (obj: any, indexStr: string) => {
    const idx = parseInt(indexStr, 10);
    if (isNaN(idx)) return indexStr;
    const attr = obj.allAttributes?.find((a: any) => (a.AttributeId || a.attributeId) === idx);
    if (attr && attr.name) return attr.name;
    return `Attribute ${idx}`;
};

const getMethodDescription = (objectType: string | null | undefined, indexStr: string) => {
    const idx = parseInt(indexStr, 10);
    if (isNaN(idx)) return indexStr;
    const typeKey = (objectType || '').toLowerCase();
    const list = METHOD_DESCRIPTIONS[typeKey];
    if (list && idx >= 1 && idx <= list.length) {
        return list[idx - 1];
    }
    return `Method ${idx}`;
};

const getAttributeType = (objectType: string | null | undefined, indexStr: string) => {
    const idx = parseInt(indexStr, 10);
    if (isNaN(idx)) return 'None';
    const typeKey = (objectType || '').toLowerCase();
    const list = ATTRIBUTE_TYPES[typeKey];
    if (list && idx >= 1 && idx <= list.length) {
        return list[idx - 1];
    }
    return 'None';
};

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
    
    const [activeTab, setActiveTab] = useState<number>(0);
    const [associationObjectList, setAssociationObjectList] = useState<any[]>([]);

    const selectedHeader = headers.find((h: any) => h.id === selectedHeaderId);
    const isDataObjectType = selectedHeader?.name === 'Data' || selectedHeader?.name === 'iecHdlcSetup' || selectedHeader?.name === 'lecHdlcSetup' || selectedHeader?.name === 'TcpUdpSetup' || selectedHeader?.name === 'Ip4Setup' || selectedHeader?.name === 'MacAddressSetup' || selectedHeader?.name === 'AssociationLogicalName' || selectedHeader?.name === 'Clock' || selectedHeader?.name === 'ScriptTable' || selectedHeader?.name === 'ActionSchedule' || selectedHeader?.name === 'ActivityCalendar';
    const isExtendedRegisterType = selectedHeader?.name === 'ExtendedRegister';
    
    const isTableObjectType = selectedHeader && [
        'data',
        'register',
        'profilegeneric',
        'extendedregister'
    ].includes(selectedHeader.name.toLowerCase());

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
        setAssociationObjectList([]);
        
        if (Number(id) > 0) {
            try {
                const result = await fetchConnectedHeaders(id);
                if (result && result.status) {
                    setHeaders(result.data);
                    
                    // Pre-fetch association object list if AssociationLogicalName is present
                    const assocHeader = result.data.find((h: any) => h.name === 'AssociationLogicalName');
                    if (assocHeader) {
                        fetchDLMSObjects(assocHeader.id).then(objectsRes => {
                            if (objectsRes && objectsRes.status) {
                                const objListParam = objectsRes.data.find((o: any) => 
                                    o.name?.toLowerCase().includes('object list')
                                );
                                if (objListParam && objListParam.attribute2) {
                                    try {
                                        const parsed = JSON.parse(objListParam.attribute2);
                                        setAssociationObjectList(parsed);
                                    } catch (e) {
                                        console.error('Failed to parse association object list:', e);
                                    }
                                }
                            }
                        }).catch(err => {
                            console.error('Failed to fetch DLMS objects for AssociationLogicalName:', err);
                        });
                    }
                    
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
                    const objListParam = mapped.find((p: any) => p.name?.toLowerCase().includes('object list'));
                    if (objListParam && objListParam.value && objListParam.value.startsWith('[')) {
                        try {
                            const parsed = JSON.parse(objListParam.value);
                            setAssociationObjectList(parsed);
                        } catch (e) {
                            console.error('Failed to parse association object list:', e);
                        }
                    }

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
            const currentHeader = headers.find((h: any) => h.id === selectedHeaderId);
            if (currentHeader?.name === 'AssociationLogicalName') {
                setDiscoveredParams((currentParams) => {
                    const objListParam = currentParams.find((p: any) => p.name?.toLowerCase().includes('object list'));
                    if (objListParam && objListParam.value && objListParam.value.startsWith('[')) {
                        try {
                            const parsed = JSON.parse(objListParam.value);
                            setAssociationObjectList(parsed);
                        } catch (e) {}
                    }
                    return currentParams;
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

    const handleScanSingleObject = async (objectId: string | number) => {
        if (!selectedDeviceId || !objectId) return;
        setDiscovering(true);
        try {
            const idx = discoveredParams.findIndex(p => p.id === objectId);
            if (idx === -1) return;

            setDiscoveredParams((prev) => {
                const next = [...prev];
                next[idx] = { ...next[idx], value: 'Scanning...' };
                return next;
            });

            const result = await readDLMSObject(selectedDeviceId, objectId);

            setDiscoveredParams((prev) => {
                const next = [...prev];
                if (result && result.status && Array.isArray(result.data)) {
                    const param = next[idx];
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

                    next[idx] = {
                        ...next[idx],
                        value: valItem ? valItem.value : 'Error',
                        attribute3: unitItem ? unitItem.value : next[idx].attribute3,
                        attribute4: statusItem ? statusItem.value : next[idx].attribute4,
                        attribute5: captureItem ? captureItem.value : next[idx].attribute5,
                        allAttributes: result.data || []
                    };
                } else {
                    next[idx] = { ...next[idx], value: 'Error' };
                }
                return next;
            });

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

    const renderAttributeValue = (attr: any, row: any) => {
        if (!attr || attr.value === undefined) return 'N/A';
        
        const value = attr.value;
        const name = row.name;
        
        const isJsonObjectList = name?.toLowerCase().includes('object list') && value?.startsWith('[') && value?.endsWith(']');
        const isContextOrAuthJson = value?.startsWith('{') && value?.endsWith('}') && (
            name?.toLowerCase().includes('context name') || 
            name?.toLowerCase().includes('mechanism name')
        );
        const isProfileGenericBuffer = row.objectType?.toLowerCase() === 'profilegeneric' && value?.startsWith('[') && value?.endsWith(']');
        const isActivityCalendar = row.objectType?.toLowerCase() === 'activitycalendar';

        if (isJsonObjectList) {
            try {
                const list = JSON.parse(value);
                return (
                    <Button
                        variant="outlined"
                        size="small"
                        color="primary"
                        onClick={() => {
                            setObjectListData(list);
                            setObjectListTitle(name);
                            setObjectListOpen(true);
                            setSearchQuery('');
                        }}
                    >
                        View Decoded List ({list.length} Objects)
                    </Button>
                );
            } catch (e) {
                return <span>{value}</span>;
            }
        }
        
        if (isProfileGenericBuffer) {
            try {
                const rows2 = JSON.parse(value);
                return (
                    <Button
                        variant="outlined"
                        size="small"
                        color="secondary"
                        onClick={() => handleOpenPgTable(value, name)}
                    >
                        View Table ({rows2.length} Rows)
                    </Button>
                );
            } catch (e) {
                return <span>{value}</span>;
            }
        }
        
        if (isActivityCalendar && attr.attributeId === 2) {
            return (
                <Stack direction="row" spacing={2} alignItems="center">
                    <span>{value || 'N/A'}</span>
                    <Button
                        variant="outlined"
                        size="small"
                        color="success"
                        onClick={() => {
                            setCalendarData(row);
                            setCalendarTitle(name);
                            setCalendarDialogOpen(true);
                        }}
                    >
                        View Calendar Config
                    </Button>
                </Stack>
            );
        }
        
        if (isContextOrAuthJson) {
            return (
                <Button
                    variant="outlined"
                    size="small"
                    color="primary"
                    onClick={() => handleOpenContextOrAuth(value, name)}
                >
                    View Details
                </Button>
            );
        }
        
        if (value?.startsWith('Error')) {
            return (
                <span style={{ color: '#d32f2f', fontWeight: 'normal' }}>
                    {value.replace('Error: ', '')}
                </span>
            );
        }
        
        return <span>{value || 'N/A'}</span>;
    };

    const parseAccessRights = (accessStr: string) => {
        if (!accessStr) return [];
        const parts = accessStr.split(/[;]/);
        return parts.map(part => {
            const colonIndex = part.indexOf(':');
            if (colonIndex > -1) {
                return {
                    name: part.substring(0, colonIndex).trim(),
                    rights: part.substring(colonIndex + 1).trim()
                };
            }
            return { name: part.trim(), rights: '' };
        }).filter(p => p.name);
    };

    const renderGenericCustomForm = (obj: any) => {
        if (!obj.allAttributes || obj.allAttributes.length === 0) {
            return (
                <Alert severity="info">
                    No attribute values found. Please click 'Scan Object' to read data from the device.
                </Alert>
            );
        }
        
        return (
            <Stack spacing={3} sx={{ mt: 1 }}>
                <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 2, position: 'relative', pt: 3 }}>
                    <Typography 
                        variant="subtitle2" 
                        sx={{ 
                            position: 'absolute', 
                            top: -12, 
                            left: 10, 
                            bgcolor: 'background.paper', 
                            px: 1, 
                            fontWeight: 'bold', 
                            color: 'text.secondary' 
                        }}
                    >
                        {(obj.objectType || 'Parameters')} Details
                    </Typography>
                    <Grid container spacing={2}>
                        {obj.allAttributes.map((attr: any) => {
                            const attrId = attr.AttributeId || attr.attributeId;
                            const attrName = attr.Name || attr.name || `Attribute ${attrId}`;
                            const attrValue = attr.value || 'Waiting...';
                            return (
                                <Grid size={{ xs: 12, md: attrId === 1 ? 12 : 6 }} key={attrId}>
                                    {attrId === 2 && obj.objectType?.toLowerCase() === 'associationlogicalname' ? (
                                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                                            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 'medium' }}>
                                                {attrName}
                                            </Typography>
                                            {renderAttributeValue(attr, obj)}
                                        </Box>
                                    ) : (
                                        <TextField
                                            label={attrName}
                                            value={attrValue}
                                            size="small"
                                            fullWidth
                                            disabled
                                        />
                                    )}
                                </Grid>
                            );
                        })}
                    </Grid>
                </Box>
            </Stack>
        );
    };

    const renderClockCustomForm = (obj: any) => {
        const getVal = (attrId: number) => {
            const attr = obj.allAttributes?.find((a: any) => (a.AttributeId || a.attributeId) === attrId);
            return attr ? attr.value : 'Waiting...';
        };

        const timeVal = getVal(2);
        const timeZoneVal = getVal(3);
        const statusVal = getVal(4);
        const beginVal = getVal(5);
        const endVal = getVal(6);
        const deviationVal = getVal(7);
        const enabledVal = getVal(8);
        const clockBaseVal = getVal(9);

        const status = parseClockStatus(statusVal);
        const isEnabled = enabledVal?.toLowerCase() === 'true' || enabledVal === '1' || enabledVal === 'Enabled';
        const clockBaseDisplay = getClockBaseDisplay(clockBaseVal);

        return (
            <Stack spacing={3} sx={{ mt: 1 }}>
                {/* Clock Object Section */}
                <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 2, position: 'relative', pt: 3 }}>
                    <Typography 
                        variant="subtitle2" 
                        sx={{ 
                            position: 'absolute', 
                            top: -12, 
                            left: 10, 
                            bgcolor: 'background.paper', 
                            px: 1, 
                            fontWeight: 'bold', 
                            color: 'text.secondary' 
                        }}
                    >
                        Clock Object
                    </Typography>
                    <Grid container spacing={2}>
                        <Grid size={{ xs: 12 }}>
                            <TextField
                                label="Logical Name"
                                value={obj.obisCode || '0.0.1.0.0.255'}
                                size="small"
                                fullWidth
                                disabled
                            />
                        </Grid>
                        <Grid size={{ xs: 12, md: 8 }}>
                            <TextField
                                label="Time"
                                value={timeVal}
                                size="small"
                                fullWidth
                                disabled
                            />
                        </Grid>
                        <Grid size={{ xs: 12, md: 4 }}>
                            <Button variant="contained" disabled fullWidth sx={{ height: '40px' }}>
                                Current time
                            </Button>
                        </Grid>
                        <Grid size={{ xs: 12, md: 6 }}>
                            <TextField
                                label="Time Zone"
                                value={timeZoneVal}
                                size="small"
                                fullWidth
                                disabled
                            />
                        </Grid>
                        <Grid size={{ xs: 12, md: 6 }} sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <FormControlLabel
                                control={<Checkbox disabled checked={false} size="small" />}
                                label=""
                                sx={{ mr: 0 }}
                            />
                            <Button variant="contained" disabled fullWidth sx={{ height: '40px' }}>
                                Current time Zone
                            </Button>
                        </Grid>
                        <Grid size={{ xs: 12 }}>
                            <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 'medium' }}>Status:</Typography>
                            <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 2, maxHeight: 180, overflowY: 'auto' }}>
                                <Stack spacing={0.5}>
                                    <FormControlLabel
                                        control={<Checkbox checked={status.invalidValue} disabled size="small" />}
                                        label="InvalidValue"
                                    />
                                    <FormControlLabel
                                        control={<Checkbox checked={status.doubtfulValue} disabled size="small" />}
                                        label="DoubtfulValue"
                                    />
                                    <FormControlLabel
                                        control={<Checkbox checked={status.differentClockBase} disabled size="small" />}
                                        label="DifferentClockBase"
                                    />
                                    <FormControlLabel
                                        control={<Checkbox checked={status.invalidClockStatus} disabled size="small" />}
                                        label="InvalidClockStatus"
                                    />
                                    <FormControlLabel
                                        control={<Checkbox checked={status.daylightSavingActive} disabled size="small" />}
                                        label="DaylightSavingActive"
                                    />
                                </Stack>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>

                {/* Daylight Savings Section */}
                <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 2, position: 'relative', pt: 3 }}>
                    <Typography 
                        variant="subtitle2" 
                        sx={{ 
                            position: 'absolute', 
                            top: -12, 
                            left: 10, 
                            bgcolor: 'background.paper', 
                            px: 1, 
                            fontWeight: 'bold', 
                            color: 'text.secondary' 
                        }}
                    >
                        Daylight Savings
                    </Typography>
                    <Grid container spacing={2} alignItems="center">
                        <Grid size={{ xs: 12, md: 4 }}>
                            <FormControlLabel
                                control={<Checkbox checked={isEnabled} disabled size="small" />}
                                label="Enabled"
                            />
                        </Grid>
                        <Grid size={{ xs: 12, md: 8 }}>
                            <TextField
                                label="Deviation"
                                value={deviationVal}
                                size="small"
                                fullWidth
                                disabled
                            />
                        </Grid>
                        <Grid size={{ xs: 12, md: 6 }}>
                            <TextField
                                label="Begin"
                                value={beginVal}
                                size="small"
                                fullWidth
                                disabled
                            />
                        </Grid>
                        <Grid size={{ xs: 12, md: 6 }}>
                            <TextField
                                label="End"
                                value={endVal}
                                size="small"
                                fullWidth
                                disabled
                            />
                        </Grid>
                    </Grid>
                </Box>

                {/* Clock Base & Adjust to */}
                <Grid container spacing={2}>
                    <Grid size={{ xs: 12, md: 6 }}>
                        <FormControl fullWidth size="small" disabled>
                            <InputLabel id="clock-base-label">Clock Base</InputLabel>
                            <Select
                                labelId="clock-base-label"
                                value={clockBaseDisplay === 'Waiting...' || clockBaseDisplay === 'Scanning...' ? '' : clockBaseDisplay}
                                label="Clock Base"
                            >
                                <MenuItem value="Crystal">Crystal</MenuItem>
                                <MenuItem value="MainsFrequency">MainsFrequency</MenuItem>
                                <MenuItem value="GPS">GPS</MenuItem>
                                <MenuItem value="Radio">Radio</MenuItem>
                            </Select>
                        </FormControl>
                    </Grid>
                </Grid>

                <Box sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 2, position: 'relative', pt: 3 }}>
                    <Typography 
                        variant="subtitle2" 
                        sx={{ 
                            position: 'absolute', 
                            top: -12, 
                            left: 10, 
                            bgcolor: 'background.paper', 
                            px: 1, 
                            fontWeight: 'bold', 
                            color: 'text.secondary' 
                        }}
                    >
                        Adjust to
                    </Typography>
                    <Grid container spacing={2}>
                        <Grid size={{ xs: 6, md: 4 }}>
                            <Button variant="contained" disabled fullWidth size="small">Quarter</Button>
                        </Grid>
                        <Grid size={{ xs: 6, md: 4 }}>
                            <Button variant="contained" disabled fullWidth size="small">Measuring period</Button>
                        </Grid>
                        <Grid size={{ xs: 6, md: 4 }}>
                            <Button variant="contained" disabled fullWidth size="small">Minute</Button>
                        </Grid>
                        <Grid size={{ xs: 6, md: 4 }}>
                            <Button variant="contained" disabled fullWidth size="small">Preset time</Button>
                        </Grid>
                        <Grid size={{ xs: 6, md: 4 }}>
                            <Button variant="contained" disabled fullWidth size="small">Preset Adjusting</Button>
                        </Grid>
                        <Grid size={{ xs: 6, md: 4 }}>
                            <Button variant="contained" disabled fullWidth size="small">Shift Time...</Button>
                        </Grid>
                    </Grid>
                </Box>
            </Stack>
        );
    };

    const renderClockLastErrors = (obj: any) => {
        return (
            <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                    <TableHead>
                        <TableRow>
                            <TableCell sx={{ fontWeight: 'bold', width: '20%' }}>Attribute Index</TableCell>
                            <TableCell sx={{ fontWeight: 'bold', width: '40%' }}>Description</TableCell>
                            <TableCell sx={{ fontWeight: 'bold', width: '40%' }}>Last error</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {CLOCK_ATTRIBUTES.map((attr) => {
                            const attrVal = obj.allAttributes?.find(
                                (a: any) => (a.AttributeId || a.attributeId) === attr.index
                            );
                            const isError = attrVal?.value?.startsWith('Error');
                            const errorMsg = isError ? attrVal.value.replace('Error: ', '') : '';
                            return (
                                <TableRow key={attr.index} hover>
                                    <TableCell>{attr.index}</TableCell>
                                    <TableCell>{attr.name}</TableCell>
                                    <TableCell sx={{ color: isError ? '#d32f2f' : 'text.secondary' }}>
                                        {errorMsg}
                                    </TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
            </TableContainer>
        );
    };

    const renderClockAccessRights = (obj: any) => {
        const assocObj = associationObjectList.find((item: any) => 
            item.LogicalName === obj.obisCode
        );
        
        const rights = assocObj ? parseAccessRights(assocObj.AttributeAccess) : [];

        return (
            <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                    <TableHead>
                        <TableRow>
                            <TableCell sx={{ fontWeight: 'bold' }}>Attribute Index</TableCell>
                            <TableCell sx={{ fontWeight: 'bold' }}>Description</TableCell>
                            <TableCell sx={{ fontWeight: 'bold' }}>Access</TableCell>
                            <TableCell sx={{ fontWeight: 'bold' }}>Access Selector</TableCell>
                            <TableCell sx={{ fontWeight: 'bold', textAlign: 'center' }}>Static</TableCell>
                            <TableCell sx={{ fontWeight: 'bold' }}>Type</TableCell>
                            <TableCell sx={{ fontWeight: 'bold' }}>UIType</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {CLOCK_ATTRIBUTES.map((attr) => {
                            const rightItem = rights.find(r => r.name === attr.index.toString());
                            const accessRight = rightItem ? rightItem.rights : 'None';
                            return (
                                <TableRow key={attr.index} hover>
                                    <TableCell>{attr.index}</TableCell>
                                    <TableCell>{attr.name}</TableCell>
                                    <TableCell>{accessRight}</TableCell>
                                    <TableCell></TableCell>
                                    <TableCell align="center">
                                        <Checkbox size="small" disabled checked={false} />
                                    </TableCell>
                                    <TableCell sx={{ color: 'text.secondary' }}>{attr.type}</TableCell>
                                    <TableCell sx={{ color: 'text.secondary' }}>None</TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
            </TableContainer>
        );
    };

    const renderClockMethodAccessRights = (obj: any) => {
        const assocObj = associationObjectList.find((item: any) => 
            item.LogicalName === obj.obisCode
        );
        
        const rights = assocObj ? parseAccessRights(assocObj.MethodAccess) : [];

        return (
            <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                    <TableHead>
                        <TableRow>
                            <TableCell sx={{ fontWeight: 'bold', width: '20%' }}>Attribute Index</TableCell>
                            <TableCell sx={{ fontWeight: 'bold', width: '50%' }}>Description</TableCell>
                            <TableCell sx={{ fontWeight: 'bold', width: '30%' }}>Method access</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {CLOCK_METHODS.map((method) => {
                            const rightItem = rights.find(r => r.name === method.index.toString());
                            const methodAccess = rightItem ? rightItem.rights : 'NoAccess';
                            return (
                                <TableRow key={method.index} hover>
                                    <TableCell>{method.index}</TableCell>
                                    <TableCell>{method.name}</TableCell>
                                    <TableCell>{methodAccess}</TableCell>
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>
            </TableContainer>
        );
    };

    const selectedObj = discoveredParams.find(p => p.id === selectedObjectId);

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

            {isTableObjectType && discoveredParams.length > 0 && (
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

            {!isTableObjectType && discoveredParams.length > 0 && (
                <Card>
                    <CardHeader 
                        title={`${selectedHeader?.name || 'Object'} Configuration & Details`} 
                        subheader={selectedObj ? `Viewing details for: ${selectedObj.name}` : `Select an object from the dropdown or list below to view its details.`}
                    />
                    <Divider />
                    <CardContent>
                        {selectedObj ? (
                            <Stack spacing={3}>
                                <Stack direction="row" justifyContent="space-between" alignItems="center">
                                    <Button 
                                        variant="outlined" 
                                        color="primary"
                                        onClick={() => setSelectedObjectId('')}
                                    >
                                        ← Back to Object List
                                    </Button>
                                    <Stack direction="row" spacing={2} alignItems="center">
                                        <Button
                                            variant="contained"
                                            color="secondary"
                                            onClick={() => handleScanSingleObject(selectedObj.id)}
                                            disabled={discovering}
                                            size="small"
                                        >
                                            {discovering ? 'Scanning...' : 'Scan Object'}
                                        </Button>
                                        <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                                            OBIS: {selectedObj.obisCode}
                                        </Typography>
                                    </Stack>
                                </Stack>

                                <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
                                    <Tabs 
                                        value={activeTab} 
                                        onChange={(e, newValue) => setActiveTab(newValue)} 
                                        aria-label="object details tabs"
                                    >
                                        <Tab label="Data" />
                                        <Tab label="Last Errors" />
                                        <Tab label="Access Rights" />
                                        <Tab label="Method Access Rights" />
                                    </Tabs>
                                </Box>

                                {/* Tab 0: Data */}
                                {activeTab === 0 && (
                                    <Box sx={{ py: 2 }}>
                                        {selectedHeader?.name === 'Clock' ? (
                                            renderClockCustomForm(selectedObj)
                                        ) : (
                                            renderGenericCustomForm(selectedObj)
                                        )}
                                    </Box>
                                )}

                                {/* Tab 1: Last Errors */}
                                {activeTab === 1 && (
                                    <Box sx={{ py: 2 }}>
                                        {selectedHeader?.name === 'Clock' ? (
                                            renderClockLastErrors(selectedObj)
                                        ) : (() => {
                                            const errors = (selectedObj.allAttributes || []).filter((attr: any) => 
                                                attr.value?.startsWith('Error') || 
                                                attr.value?.toLowerCase().includes('fail')
                                            );
                                            
                                            if (errors.length > 0) {
                                                return (
                                                    <Stack spacing={2}>
                                                        <Alert severity="error">
                                                            Found {errors.length} error(s) during the last scan of this object.
                                                        </Alert>
                                                        <TableContainer component={Paper} variant="outlined">
                                                            <Table size="small">
                                                                <TableHead>
                                                                    <TableRow>
                                                                        <TableCell sx={{ fontWeight: 'bold', width: '20%' }}>Attribute Index</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold', width: '40%' }}>Description</TableCell>
                                                                        <TableCell sx={{ fontWeight: 'bold', width: '40%' }}>Last error</TableCell>
                                                                    </TableRow>
                                                                </TableHead>
                                                                <TableBody>
                                                                    {errors.map((err: any) => (
                                                                        <TableRow key={err.AttributeId || err.attributeId}>
                                                                            <TableCell>{err.AttributeId || err.attributeId}</TableCell>
                                                                            <TableCell>{err.Name || err.name || 'N/A'}</TableCell>
                                                                            <TableCell sx={{ color: '#d32f2f' }}>
                                                                                {err.value}
                                                                            </TableCell>
                                                                        </TableRow>
                                                                    ))}
                                                                </TableBody>
                                                            </Table>
                                                        </TableContainer>
                                                    </Stack>
                                                );
                                            }
                                            
                                            const hasScanned = (selectedObj.allAttributes || []).some((attr: any) => 
                                                attr.value && attr.value !== 'Waiting...' && attr.value !== 'Scanning...'
                                            );
                                            
                                            if (!hasScanned) {
                                                return (
                                                    <Alert severity="info">
                                                        No scan has been performed yet. Click 'Scan Object' or 'Scan & Discover Meter Parameters' to read parameter values.
                                                    </Alert>
                                                );
                                            }

                                            return (
                                                <Alert severity="success">
                                                    All attributes were read successfully without any errors!
                                                </Alert>
                                            );
                                        })()}
                                    </Box>
                                )}

                                {/* Tab 2: Access Rights */}
                                {activeTab === 2 && (
                                    <Box sx={{ py: 2 }}>
                                        {selectedHeader?.name === 'Clock' ? (
                                            renderClockAccessRights(selectedObj)
                                        ) : (() => {
                                            const assocObj = associationObjectList.find((item: any) => 
                                                item.LogicalName === selectedObj.obisCode
                                            );
                                            
                                            if (!assocObj) {
                                                return (
                                                    <Alert severity="warning">
                                                        Access rights mapping not found for OBIS Code: {selectedObj.obisCode}. 
                                                        Please make sure to select and scan the <strong>AssociationLogicalName</strong> object list first.
                                                    </Alert>
                                                );
                                            }
                                            
                                            const rights = parseAccessRights(assocObj.AttributeAccess);
                                            if (rights.length === 0) {
                                                return (
                                                    <Alert severity="info">
                                                        No attribute access rights defined or available. Raw value: {assocObj.AttributeAccess || 'None'}
                                                    </Alert>
                                                );
                                            }

                                            return (
                                                <TableContainer component={Paper} variant="outlined">
                                                    <Table size="small">
                                                        <TableHead>
                                                            <TableRow>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Attribute Index</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Description</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Access</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Access Selector</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold', textAlign: 'center' }}>Static</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Type</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>UIType</TableCell>
                                                            </TableRow>
                                                        </TableHead>
                                                        <TableBody>
                                                            {rights.map((r, ri) => {
                                                                const desc = getAttributeDescription(selectedObj, r.name);
                                                                const type = getAttributeType(selectedObj.objectType, r.name);
                                                                return (
                                                                    <TableRow key={ri} hover>
                                                                        <TableCell>{r.name}</TableCell>
                                                                        <TableCell>{desc}</TableCell>
                                                                        <TableCell>{r.rights}</TableCell>
                                                                        <TableCell></TableCell>
                                                                        <TableCell align="center">
                                                                            <Checkbox size="small" disabled checked={false} />
                                                                        </TableCell>
                                                                        <TableCell sx={{ color: 'text.secondary' }}>{type}</TableCell>
                                                                        <TableCell sx={{ color: 'text.secondary' }}>None</TableCell>
                                                                    </TableRow>
                                                                );
                                                            })}
                                                        </TableBody>
                                                    </Table>
                                                </TableContainer>
                                            );
                                        })()}
                                    </Box>
                                )}

                                {/* Tab 3: Method Access Rights */}
                                {activeTab === 3 && (
                                    <Box sx={{ py: 2 }}>
                                        {selectedHeader?.name === 'Clock' ? (
                                            renderClockMethodAccessRights(selectedObj)
                                        ) : (() => {
                                            const assocObj = associationObjectList.find((item: any) => 
                                                item.LogicalName === selectedObj.obisCode
                                            );
                                            
                                            if (!assocObj) {
                                                return (
                                                    <Alert severity="warning">
                                                        Method access rights mapping not found for OBIS Code: {selectedObj.obisCode}. 
                                                        Please make sure to select and scan the <strong>AssociationLogicalName</strong> object list first.
                                                    </Alert>
                                                );
                                            }
                                            
                                            const rights = parseAccessRights(assocObj.MethodAccess);
                                            if (rights.length === 0) {
                                                return (
                                                    <Alert severity="info">
                                                        No method access rights defined or available. Raw value: {assocObj.MethodAccess || 'None'}
                                                    </Alert>
                                                );
                                            }

                                            return (
                                                <TableContainer component={Paper} variant="outlined">
                                                    <Table size="small">
                                                        <TableHead>
                                                            <TableRow>
                                                                <TableCell sx={{ fontWeight: 'bold', width: '20%' }}>Attribute Index</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold', width: '50%' }}>Description</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold', width: '30%' }}>Method access</TableCell>
                                                            </TableRow>
                                                        </TableHead>
                                                        <TableBody>
                                                            {rights.map((r, ri) => {
                                                                const desc = getMethodDescription(selectedObj.objectType, r.name);
                                                                return (
                                                                    <TableRow key={ri} hover>
                                                                        <TableCell>{r.name}</TableCell>
                                                                        <TableCell>{desc}</TableCell>
                                                                        <TableCell>{r.rights}</TableCell>
                                                                    </TableRow>
                                                                );
                                                            })}
                                                        </TableBody>
                                                    </Table>
                                                </TableContainer>
                                            );
                                        })()}
                                    </Box>
                                )}
                            </Stack>
                        ) : (
                            <TableContainer component={Paper} sx={{ boxShadow: 'none' }}>
                                <Table stickyHeader aria-label="objects summary table">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell sx={{ fontWeight: 'bold' }}>OBIS Code</TableCell>
                                            <TableCell sx={{ fontWeight: 'bold' }}>Object Name</TableCell>
                                            <TableCell sx={{ fontWeight: 'bold' }}>Object Type</TableCell>
                                            <TableCell sx={{ fontWeight: 'bold', textAlign: 'center' }}>Action</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {discoveredParams.map((row, idx) => (
                                            <TableRow key={idx} hover>
                                                <TableCell sx={{ fontFamily: 'monospace' }}>{row.obisCode}</TableCell>
                                                <TableCell>{row.name}</TableCell>
                                                <TableCell>{row.objectType}</TableCell>
                                                <TableCell sx={{ textAlign: 'center' }}>
                                                    <Button 
                                                        variant="contained" 
                                                        size="small" 
                                                        onClick={() => {
                                                            setSelectedObjectId(row.id);
                                                            setActiveTab(0);
                                                        }}
                                                    >
                                                        View Details
                                                    </Button>
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        )}
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
