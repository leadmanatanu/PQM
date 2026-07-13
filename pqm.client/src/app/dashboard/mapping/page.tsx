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
import IconButton from '@mui/material/IconButton';
import VisibilityIcon from '@mui/icons-material/Visibility';
import EditIcon from '@mui/icons-material/Edit';
import CheckIcon from '@mui/icons-material/Check';
import CloseIcon from '@mui/icons-material/Close';

// Dynamically obtained from the device

type EventStatusItem = {
    code: number;
    label: string;
};

type EventStatusSection = {
    key: string;
    title: string;
    obisCode: string;
    items: EventStatusItem[];
};

const DEFAULT_EVENT_STATUS_SECTIONS: EventStatusSection[] = [
    {
        key: 'voltage',
        title: 'Voltage Related',
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
        title: 'Current Related',
        obisCode: '0.0.96.11.1.255',
        items: [
            { code: 65, label: 'Current bypass - Occurrence' },              // BitIndex 0
            { code: 66, label: 'Current bypass - Restoration' },             // BitIndex 1
            { code: 67, label: 'Over current in any phase - Occurrence' },   // BitIndex 2
            { code: 68, label: 'Over current in any phase - Restoration' },  // BitIndex 3
            { code: 51, label: 'R Phase - Current reverse - Occurrence' },   // BitIndex 4
            { code: 52, label: 'R Phase - Current reverse - Restoration' },  // BitIndex 5
            { code: 64, label: 'Current Unbalance - Restoration' },          // BitIndex 6
            { code: 63, label: 'Current Unbalance - Occurrence' },           // BitIndex 7
            { code: 53, label: 'Y Phase - Current reverse - Occurrence' },   // BitIndex 8
            { code: 54, label: 'Y Phase - Current reverse - Restoration' },  // BitIndex 9
            { code: 55, label: 'B Phase - Current reverse - Occurrence' },   // Index 10
            { code: 56, label: 'B Phase - Current reverse - Restoration' },  // Index 11
        ],
    },
    {
        key: 'power',
        title: 'Power Related',
        obisCode: '0.0.96.11.2.255',
        items: [
            { code: 101, label: 'Power failure - Occurrence' },
            { code: 102, label: 'Power failure - Restoration' },
        ],
    },
    {
        key: 'transaction',
        title: 'Transaction Related',
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
        title: 'Others',
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

const getMethodDescription = (objectType: string | null | undefined, indexStr: string) => {
    const idx = parseInt(indexStr, 10);
    if (isNaN(idx)) return indexStr;
    const typeKey = (objectType || '').toLowerCase();
    
    // Standard DLMS COSEM method names mapping
    const methods: Record<string, string[]> = {
        'clock': ['Adjust to quarter', 'Adjust to measuring period', 'Adjust to minute', 'Adjust to preset time', 'Preset adjusting time', 'Shift time'],
        'scripttable': ['Execute'],
        'associationlogicalname': ['Reply to connect', 'Disconnect', 'Update secret', 'Add object', 'Remove object'],
        'activitycalendar': ['Activate passive calendar'],
        'profilegeneric': ['Reset', 'Capture'],
        'actionschedule': ['Reset'],
        'register': ['Reset'],
        'extendedregister': ['Reset'],
        'demandregister': ['Reset']
    };
    
    const list = methods[typeKey];
    if (list && idx >= 1 && idx <= list.length) {
        return list[idx - 1];
    }
    return `Method ${idx}`;
};

import { DeviceFilters } from '@/components/dashboard/mapping/device-selection';
import { 
    fetchDevices, 
    fetchConnectedHeaders, 
    fetchDLMSObjects, 
    readDLMSObject,
    readDLMSObjectsBatch,
    fetchEventStatusMappings,
    writeDLMSObjectAttribute
} from '../../../api/device';
import { Device } from '../../../components/dashboard/device/devices-table';

export default function Page(): React.JSX.Element {
    const [devices, setDevices] = useState<Device[]>([]);
    const [selectedDeviceId, setSelectedDeviceId] = useState<string | number>(0);
    
    const [headers, setHeaders] = useState<any[]>([]);
    const [selectedHeaderId, setSelectedHeaderId] = useState<string | number>('');
    
    const [objects, setObjects] = useState<any[]>([]);
    const [selectedObjectId, setSelectedObjectId] = useState<string | number>('');
    const [detailsObjectId, setDetailsObjectId] = useState<string | number>('');
    
    const [activeTab, setActiveTab] = useState<number>(0);
    const [associationObjectList, setAssociationObjectList] = useState<any[]>([]);
    const [eventStatusSections, setEventStatusSections] = useState<EventStatusSection[]>(DEFAULT_EVENT_STATUS_SECTIONS);

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

    const [editingAttrId, setEditingAttrId] = useState<number | null>(null);
    const [editingValue, setEditingValue] = useState<string>('');
    const [writing, setWriting] = useState<boolean>(false);

    const isEventStatusRow = (row: any): boolean => {
        const obisCode = row?.obisCode || '';
        return eventStatusSections.some(section => section.obisCode === obisCode);
    };

    const parseEventStatusValue = (value: string | null | undefined, section: EventStatusSection, obisCode?: string): Set<number> => {
        const activeCodes = new Set<number>();

        // Try reading from localStorage first if value is empty/Waiting...
        if ((!value || value === 'Waiting...') && obisCode) {
            const stored = typeof window !== 'undefined' ? localStorage.getItem(`event_status_selections_${obisCode}`) : null;
            if (stored) {
                try {
                    const parsed = JSON.parse(stored) as { value: number; name: string }[];
                    parsed.forEach(item => activeCodes.add(item.value));
                    return activeCodes;
                } catch (e) {
                    console.error('Failed to parse stored event status:', e);
                }
            }
        }

        if (!value || value === 'Waiting...' || value === 'Scanning...' || value.startsWith('Error')) {
            return activeCodes;
        }

        const normalized = value.trim();
        const numericValue = Number(normalized);
        if (!Number.isNaN(numericValue)) {
            section.items.forEach((item, index) => {
                if ((numericValue & (1 << index)) !== 0) {
                    activeCodes.add(item.code);
                }
            });
            return activeCodes;
        }

        const bitString = normalized.replace(/\s/g, '');
        if (/^[01]+$/.test(bitString)) {
            const reversedBits = bitString.split('').reverse();
            section.items.forEach((item, index) => {
                const bitIndex = index % bitString.length;
                if (reversedBits[bitIndex] === '1') {
                    activeCodes.add(item.code);
                }
            });
            return activeCodes;
        }

        section.items.forEach(item => {
            if (normalized.toLowerCase().includes(item.label.toLowerCase()) || normalized.includes(String(item.code))) {
                activeCodes.add(item.code);
            }
        });

        return activeCodes;
    };

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

    const getReadResultItem = (items: any[], attributeId: number) => {
        return items.find((item: any) => item.attributeId === attributeId || item.AttributeId === attributeId);
    };

    const getPrimaryReadResultItem = (items: any[]) => {
        return getReadResultItem(items, 2) || items.find((item: any) => {
            const attrId = item.attributeId ?? item.AttributeId;
            return attrId !== 1;
        }) || items[0];
    };

    const applyReadResultToRow = (row: any, items: any[]) => {
        const valItem = getPrimaryReadResultItem(items);
        const unitItem = getReadResultItem(items, 3);
        const statusItem = getReadResultItem(items, 4);
        const captureItem = getReadResultItem(items, 5);

        return {
            ...row,
            value: valItem ? valItem.value ?? valItem.Value ?? 'Error' : 'Error',
            attribute3: unitItem ? unitItem.value ?? unitItem.Value : row.attribute3,
            attribute4: statusItem ? statusItem.value ?? statusItem.Value : row.attribute4,
            attribute5: captureItem ? captureItem.value ?? captureItem.Value : row.attribute5,
            allAttributes: items || []
        };
    };

    const getAttributeRows = (obj: any) => {
        const attrs = Array.isArray(obj?.allAttributes) ? obj.allAttributes : [];
        const prevAttrs = Array.isArray(obj?.allAttributesBeforeScan) ? obj.allAttributesBeforeScan : [];
        const rows = attrs.map((attr: any) => {
            const attributeId = attr.AttributeId ?? attr.attributeId;
            let val = attr.value ?? attr.Value ?? 'Waiting...';
            const isValInvalid = !val || val === 'Waiting...' || val === 'Scanning...' || String(val).startsWith('Error');
            if (isValInvalid && prevAttrs.length > 0) {
                const prevAttr = prevAttrs.find((pa: any) => (pa.AttributeId ?? pa.attributeId) === attributeId);
                const prevVal = prevAttr?.value ?? prevAttr?.Value;
                if (prevVal && prevVal !== 'Waiting...' && prevVal !== 'Scanning...' && !String(prevVal).startsWith('Error')) {
                    val = prevVal;
                }
            }
            return {
                attributeId,
                name: attr.Name || attr.name || `Attribute ${attributeId}`,
                value: val,
                dataType: attr.DataType || attr.dataType || 'None',
                accessType: attr.AccessType || attr.accessType || ''
            };
        });

        if ((obj?.objectType || '').toLowerCase() === 'data') {
            if (!rows.some((row: any) => row.attributeId === 1)) {
                rows.unshift({
                    attributeId: 1,
                    name: 'Logical Name',
                    value: obj.obisCode || 'Waiting...',
                    dataType: 'OctetString',
                    accessType: 'Read'
                });
            }

            if (!rows.some((row: any) => row.attributeId === 2)) {
                let attr2Val = obj.value || 'Waiting...';
                if ((!attr2Val || attr2Val === 'Waiting...' || attr2Val === 'Scanning...' || attr2Val.startsWith('Error')) && obj.valueBeforeScan) {
                    attr2Val = obj.valueBeforeScan;
                }
                rows.push({
                    attributeId: 2,
                    name: obj.name?.replace(obj.obisCode, '').trim() || 'Value',
                    value: attr2Val,
                    dataType: 'None',
                    accessType: 'ReadWrite'
                });
            }
        }

        return rows.sort((a: any, b: any) => Number(a.attributeId) - Number(b.attributeId));
    };

    const canReadAccess = (access: string | null | undefined) => {
        const normalized = (access || '').toLowerCase();
        return normalized.includes('read') && !normalized.includes('noaccess') && !normalized.includes('no access');
    };

    const isAttributeReadRequired = (obj: any, index: string | number, access?: string) => {
        const attrIndex = Number(index);
        if (attrIndex === 1 || !canReadAccess(access || (attrIndex === 1 ? 'Read' : 'ReadWrite'))) {
            return false;
        }

        const attr = (obj?.allAttributes || []).find((item: any) => {
            const itemIndex = item.AttributeId ?? item.attributeId;
            return Number(itemIndex) === attrIndex;
        });
        const value = attr?.value ?? attr?.Value ?? (attrIndex === 2 ? obj?.value : undefined);

        return value === undefined ||
            value === null ||
            value === '' ||
            value === 'Waiting...' ||
            value === 'Scanning...' ||
            String(value).startsWith('Error');
    };

    const getRequiredText = (required: boolean) => required ? 'Required' : 'Not required';

    const getAttributeSourceText = (obj: any, index: string | number) => {
        const attrIndex = Number(index);
        if (attrIndex === 1) {
            return 'Device association';
        }

        const attr = (obj?.allAttributes || []).find((item: any) => {
            const itemIndex = item.AttributeId ?? item.attributeId;
            return Number(itemIndex) === attrIndex;
        });
        const value = attr?.value ?? attr?.Value ?? (attrIndex === 2 ? obj?.value : undefined);

        if (value === 'Waiting...' || value === 'Scanning...' || value === undefined || value === null || value === '') {
            return 'Pending device read';
        }

        if (String(value).startsWith('Error')) {
            return 'Device error';
        }

        return 'Device';
    };

    useEffect(() => {
        const loadDevicesAndMappings = async () => {
            try {
                const fetchedDevices = await fetchDevices();
                setDevices(fetchedDevices);
            } catch (error) {
                console.error('Failed to fetch devices:', error);
            }

            try {
                const mappingsRes = await fetchEventStatusMappings();
                if (mappingsRes && mappingsRes.status && Array.isArray(mappingsRes.data) && mappingsRes.data.length > 0) {
                    const grouped = mappingsRes.data.reduce((acc: any, item: any) => {
                        const obis = item.obisCode;
                        if (!acc[obis]) {
                            acc[obis] = {
                                key: item.category,
                                title: `${item.category.charAt(0).toUpperCase() + item.category.slice(1)} Related`,
                                obisCode: obis,
                                items: []
                            };
                        }
                        acc[obis].items.push({
                            code: item.eventCode,
                            label: item.label
                        });
                        return acc;
                    }, {});
                    setEventStatusSections(Object.values(grouped));
                }
            } catch (error) {
                console.error('Failed to fetch dynamic event mappings:', error);
            }
        };
        loadDevicesAndMappings();
    }, []);

    const handleStartEdit = (attrId: number, currentVal: string) => {
        setEditingAttrId(attrId);
        setEditingValue(currentVal || '');
    };

    const handleCancelEdit = () => {
        setEditingAttrId(null);
        setEditingValue('');
    };

    const handleSaveWrite = async (obj: any, attrId: number) => {
        setWriting(true);
        try {
            const res = await writeDLMSObjectAttribute(selectedDeviceId, obj.obisCode, editingValue, attrId);
            if (res && res.status) {
                setDisplayMsg(`Successfully wrote '${editingValue}' to the device.`);
                setOpenSnackbar(true);
                setEditingAttrId(null);
                handleScanSingleObject(obj.id);
            } else {
                setDisplayMsg(res?.errors?.[0] || 'Write request failed.');
                setOpenSnackbar(true);
            }
        } catch (error: any) {
            setDisplayMsg(error.message || 'Error executing write operation.');
            setOpenSnackbar(true);
        } finally {
            setWriting(false);
        }
    };

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
                    value: 'Waiting...',
                    attribute3: 'Waiting...',
                    allAttributes: (obj.allAttributes || []).map((attr: any) => ({
                        ...attr,
                        value: 'Waiting...',
                        Value: 'Waiting...'
                    }))
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
                        'authentication mechanism name',
                        'xdlms context info',
                        'lls secret',
                        'security setup reference',
                        'user list'
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

    const handleOpenDetails = (objectId: string | number) => {
        setDetailsObjectId(objectId);
        setActiveTab(0);
    };

    const handleCloseDetails = () => {
        setDetailsObjectId('');
        setActiveTab(0);
    };

    const handleDiscoverParameters = async () => {
        if (!selectedDeviceId || discoveredParams.length === 0) return;
        setDiscovering(true);
        try {
            // 1. Mark all parameters as Scanning...
            setDiscoveredParams((prev) => 
                prev.map(p => {
                    const currentVal = p.value;
                    const hasValidPrevValue = currentVal && currentVal !== 'Waiting...' && currentVal !== 'Scanning...' && !currentVal.startsWith('Error');
                    return {
                        ...p,
                        valueBeforeScan: hasValidPrevValue ? currentVal : p.valueBeforeScan,
                        allAttributesBeforeScan: (p.allAttributes && p.allAttributes.length > 0) ? p.allAttributes : p.allAttributesBeforeScan,
                        value: 'Scanning...'
                    };
                })
            );

            const idsToRead = discoveredParams.map((p: any) => p.id);

            // 2. Call backend read-objects API in batch
            const result = await readDLMSObjectsBatch(selectedDeviceId, idsToRead);

            // 3. Update all parameters with the returned values
            setDiscoveredParams((prev) => {
                return prev.map(p => {
                    const objResult = result && result.status && result.data ? (result.data[p.id] || result.data[p.id.toString()]) : null;
                    if (objResult && Array.isArray(objResult)) {
                        return applyReadResultToRow(p, objResult);
                    } else {
                        return { ...p, value: 'Error' };
                    }
                });
            });

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

            const param = discoveredParams[idx];
            const currentVal = param.value;
            const hasValidPrevValue = currentVal && currentVal !== 'Waiting...' && currentVal !== 'Scanning...' && !currentVal.startsWith('Error');

            setDiscoveredParams((prev) => {
                const next = [...prev];
                next[idx] = { 
                    ...next[idx], 
                    valueBeforeScan: hasValidPrevValue ? currentVal : next[idx].valueBeforeScan,
                    allAttributesBeforeScan: (next[idx].allAttributes && next[idx].allAttributes.length > 0) ? next[idx].allAttributes : next[idx].allAttributesBeforeScan,
                    value: 'Scanning...' 
                };
                return next;
            });

            const result = await readDLMSObject(selectedDeviceId, objectId);

            setDiscoveredParams((prev) => {
                const next = [...prev];
                if (result && result.status && Array.isArray(result.data)) {
                    next[idx] = applyReadResultToRow(next[idx], result.data);
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

    const handleEventCheckboxChange = (obj: any, section: EventStatusSection, item: EventStatusItem, checked: boolean) => {
        const displayValue = (obj?.value === 'Scanning...' || obj?.value?.startsWith('Error')) 
            ? (obj?.valueBeforeScan || obj?.value) 
            : obj?.value;

        const activeCodes = parseEventStatusValue(displayValue, section, obj?.obisCode);
        
        if (checked) {
            activeCodes.add(item.code);
        } else {
            activeCodes.delete(item.code);
        }

        // Reconstruct the numeric bitmask value
        let newNumericValue = 0;
        section.items.forEach((secItem, index) => {
            if (activeCodes.has(secItem.code)) {
                newNumericValue |= (1 << index);
            }
        });
        
        const newValueStr = String(newNumericValue);

        // Update discoveredParams state in real time
        setDiscoveredParams(prev => prev.map(p => {
            if (p.id === obj.id) {
                const updatedAttrs = (p.allAttributes || []).map((attr: any) => {
                    const attrId = attr.AttributeId ?? attr.attributeId;
                    if (attrId === 2) {
                        return { ...attr, value: newValueStr, Value: newValueStr };
                    }
                    return attr;
                });
                return {
                    ...p,
                    value: newValueStr,
                    allAttributes: updatedAttrs
                };
            }
            return p;
        }));

        // Store selected checkboxes (value = code, name = label) in localStorage
        const selectedItems = section.items
            .filter(secItem => activeCodes.has(secItem.code))
            .map(secItem => ({
                value: secItem.code,
                name: secItem.label
            }));

        localStorage.setItem(`event_status_selections_${obj.obisCode}`, JSON.stringify(selectedItems));
        console.log(`Stored event status selections for ${obj.obisCode}:`, selectedItems);
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
        const isContextOrAuthJson = value?.startsWith('{') && value?.endsWith('}');
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

    const renderEventStatusDetails = (obj: any) => {
        const matchingSections = eventStatusSections.filter(section => section.obisCode === obj?.obisCode);
        const displayValue = (obj?.value === 'Scanning...' || obj?.value?.startsWith('Error')) 
            ? (obj?.valueBeforeScan || obj?.value) 
            : obj?.value;

        return (
            <Stack spacing={2}>
                <Box>
                    <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                        {obj?.name || 'Event Data Object'}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                        Value: {displayValue || 'N/A'}
                    </Typography>
                </Box>
                <Alert severity="info">
                    Event section names use the standard OBIS mapping. The active event state is decoded from the value read from the device.
                </Alert>
                <Grid container spacing={2}>
                    {matchingSections.length === 0 && (
                        <Grid size={{ xs: 12 }}>
                            <Alert severity="info">
                                No event status mapping is configured for this data object.
                            </Alert>
                        </Grid>
                    )}
                    {matchingSections.map(section => {
                        const activeCodes = parseEventStatusValue(displayValue, section, obj?.obisCode);

                        return (
                            <Grid key={section.key} size={{ xs: 12 }}>
                                <Card
                                    variant="outlined"
                                    sx={{
                                        height: '100%',
                                        borderColor: 'primary.main',
                                        bgcolor: 'action.hover',
                                    }}
                                >
                                    <CardHeader
                                        title={section.title}
                                        subheader={section.obisCode}
                                        titleTypographyProps={{ variant: 'subtitle1', fontWeight: 'bold' }}
                                        subheaderTypographyProps={{ fontFamily: 'monospace' }}
                                    />
                                    <Divider />
                                    <CardContent sx={{ py: 1.5 }}>
                                        <Stack spacing={0.5}>
                                            {section.items.map(item => (
                                                <FormControlLabel
                                                    key={item.code}
                                                    control={
                                                        <Checkbox
                                                            checked={activeCodes.has(item.code)}
                                                            disabled={discovering}
                                                            onChange={(e) => handleEventCheckboxChange(obj, section, item, e.target.checked)}
                                                            size="small"
                                                        />
                                                    }
                                                    label={`${item.label} (${item.code})`}
                                                    sx={{
                                                        m: 0,
                                                        '& .MuiFormControlLabel-label': {
                                                            fontSize: '0.875rem',
                                                        },
                                                    }}
                                                />
                                            ))}
                                        </Stack>
                                    </CardContent>
                                </Card>
                            </Grid>
                        );
                    })}
                </Grid>
            </Stack>
        );
    };

    const renderGenericCustomForm = (obj: any) => {
        const attributeRows = getAttributeRows(obj);

        if (attributeRows.length === 0) {
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
                        {attributeRows.map((attr: any) => {
                            const attrId = attr.attributeId;
                            const attrName = attr.name || `Attribute ${attrId}`;
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

    // Generic forms and lists used instead

    const selectedObj = discoveredParams.find(p => p.id === selectedObjectId);
    const detailsObj = discoveredParams.find(p => p.id === detailsObjectId);

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
                <Card sx={{ borderRadius: '8px' }}>
                    <CardHeader 
                        title="Discovered Meter Parameters (Current Values)" 
                        titleTypographyProps={{ variant: 'subtitle1', fontWeight: 600 }}
                        sx={{ py: 1.5, px: 2 }}
                    />
                    <Divider />
                    <CardContent sx={{ p: 1, '&:last-child': { pb: 1 } }}>
                        <TableContainer component={Paper} sx={{ maxHeight: 220, borderRadius: '6px' }}>
                            <Table size="small" stickyHeader aria-label="discovered parameters table">
                                <TableHead sx={{ bgcolor: 'var(--mui-palette-neutral-50)' }}>
                                    <TableRow>
                                        <TableCell sx={{ fontWeight: 600 }}>Name</TableCell>
                                        <TableCell sx={{ fontWeight: 600 }}>Object Type</TableCell>
                                        <TableCell sx={{ fontWeight: 600 }}>Attribute 2</TableCell>
                                        <TableCell sx={{ fontWeight: 600, textAlign: 'center' }}>Details</TableCell>
                                        {isExtendedRegisterType && <TableCell sx={{ fontWeight: 600 }}>Status</TableCell>}
                                        {isExtendedRegisterType && <TableCell sx={{ fontWeight: 600 }}>Capture Time</TableCell>}
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {discoveredParams.map((row, idx) => {
                                        const isJsonObjectList = row.name?.toLowerCase().includes('object list') && row.value?.startsWith('[') && row.value?.endsWith(']');
                                        const isContextOrAuthJson = row.value?.startsWith('{') && row.value?.endsWith('}');
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
                                                            row.valueBeforeScan && row.valueBeforeScan !== 'Waiting...' ? (
                                                                <span>{row.valueBeforeScan} <span style={{ color: '#d32f2f', fontWeight: 'normal', fontSize: '0.825rem' }}>(Error)</span></span>
                                                            ) : (
                                                                <span style={{ color: '#d32f2f', fontWeight: 'normal' }}>
                                                                    {row.value.replace('Error: ', '')}
                                                                </span>
                                                            )
                                                        ) : row.value === 'Scanning...' ? (
                                                            row.valueBeforeScan && row.valueBeforeScan !== 'Waiting...' ? (
                                                                <span>{row.valueBeforeScan} <span style={{ color: 'text.secondary', fontWeight: 'normal', fontSize: '0.825rem' }}>(Scanning...)</span></span>
                                                            ) : (
                                                                <span>Scanning...</span>
                                                            )
                                                        ) : (
                                                            row.value || 'N/A'
                                                        )
                                                    )}
                                                </TableCell>
                                                <TableCell sx={{ textAlign: 'center' }}>
                                                    <Button
                                                        variant="outlined"
                                                        size="small"
                                                        startIcon={<VisibilityIcon fontSize="small" />}
                                                        onClick={() => handleOpenDetails(row.id)}
                                                    >
                                                        View Details
                                                    </Button>
                                                </TableCell>
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

            {discoveredParams.length > 0 && (!isTableObjectType || selectedObj) && (
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
                                        {renderGenericCustomForm(selectedObj)}
                                    </Box>
                                )}

                                {/* Tab 1: Last Errors */}
                                {activeTab === 1 && (
                                    <Box sx={{ py: 2 }}>
                                        {(() => {
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
                                        {(() => {
                                            const assocObj = associationObjectList.find((item: any) => 
                                                item.LogicalName === selectedObj.obisCode
                                            );
                                            
                                            const isDataObject = selectedObj.objectType?.toLowerCase() === 'data';
                                            const fallbackRows = getAttributeRows(selectedObj);

                                            if (!assocObj && !isDataObject) {
                                                return (
                                                    <Alert severity="warning">
                                                        Access rights mapping not found for OBIS Code: {selectedObj.obisCode}. 
                                                        Please make sure to select and scan the <strong>AssociationLogicalName</strong> object list first.
                                                    </Alert>
                                                );
                                            }
                                             
                                            const rights = assocObj ? parseAccessRights(assocObj.AttributeAccess) : [];
                                            if (rights.length === 0) {
                                                if (!isDataObject) {
                                                    return (
                                                         <Alert severity="info">
                                                            No attribute access rights defined or available. Raw value: {assocObj?.AttributeAccess || 'None'}
                                                        </Alert>
                                                    );
                                                }
                                            }
                                             
                                            const accessRows = rights.length > 0
                                                ? rights.map((r: any) => {
                                                    const matchedAttr = fallbackRows.find((f: any) => String(f.attributeId) === r.name);
                                                    return {
                                                        index: r.name,
                                                        description: matchedAttr?.name || `Attribute ${r.name}`,
                                                        access: r.rights,
                                                        type: matchedAttr?.dataType || 'None',
                                                        required: isAttributeReadRequired(selectedObj, r.name, r.rights),
                                                        source: getAttributeSourceText(selectedObj, r.name)
                                                    };
                                                })
                                                : fallbackRows.map((row: any) => ({
                                                    index: String(row.attributeId),
                                                    description: row.name,
                                                    access: row.accessType || (row.attributeId === 1 ? 'Read' : 'ReadWrite'),
                                                    type: row.dataType || 'None',
                                                    required: isAttributeReadRequired(
                                                        selectedObj,
                                                        row.attributeId,
                                                        row.accessType || (row.attributeId === 1 ? 'Read' : 'ReadWrite')
                                                    ),
                                                    source: getAttributeSourceText(selectedObj, row.attributeId)
                                                }));

                                            return (
                                                <TableContainer component={Paper} variant="outlined">
                                                    <Table size="small">
                                                        <TableHead>
                                                            <TableRow>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Attribute Index</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Description</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Access</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Required</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Source</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Access Selector</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold', textAlign: 'center' }}>Static</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Type</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>UIType</TableCell>
                                                            </TableRow>
                                                         </TableHead>
                                                         <TableBody>
                                                            {accessRows.map((row: any, ri: number) => (
                                                                <TableRow key={ri} hover>
                                                                    <TableCell>{row.index}</TableCell>
                                                                    <TableCell>{row.description}</TableCell>
                                                                    <TableCell>{row.access}</TableCell>
                                                                    <TableCell sx={{ color: row.required ? 'warning.main' : 'text.secondary' }}>
                                                                        {getRequiredText(row.required)}
                                                                    </TableCell>
                                                                    <TableCell sx={{ color: row.source === 'Device error' ? 'error.main' : 'text.secondary' }}>
                                                                        {row.source}
                                                                    </TableCell>
                                                                    <TableCell></TableCell>
                                                                    <TableCell align="center">
                                                                        <Checkbox size="small" disabled checked={false} />
                                                                    </TableCell>
                                                                    <TableCell sx={{ color: 'text.secondary' }}>{row.type}</TableCell>
                                                                    <TableCell sx={{ color: 'text.secondary' }}>None</TableCell>
                                                                </TableRow>
                                                            ))}
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
                                        {(() => {
                                            const assocObj = associationObjectList.find((item: any) => 
                                                item.LogicalName === selectedObj.obisCode
                                            );
                                            
                                            const isDataObject = selectedObj.objectType?.toLowerCase() === 'data';

                                            if (!assocObj && !isDataObject) {
                                                return (
                                                    <Alert severity="warning">
                                                        Method access rights mapping not found for OBIS Code: {selectedObj.obisCode}. 
                                                        Please make sure to select and scan the <strong>AssociationLogicalName</strong> object list first.
                                                    </Alert>
                                                );
                                            }
                                             
                                            const rights = assocObj ? parseAccessRights(assocObj.MethodAccess) : [];
                                            if (rights.length === 0) {
                                                if (!isDataObject) {
                                                    return (
                                                        <Alert severity="info">
                                                            No method access rights defined or available. Raw value: {assocObj?.MethodAccess || 'None'}
                                                        </Alert>
                                                    );
                                                }
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
                                                            {rights.length > 0 ? (
                                                                rights.map((r, ri) => {
                                                                    const desc = getMethodDescription(selectedObj.objectType, r.name);
                                                                    return (
                                                                        <TableRow key={ri} hover>
                                                                            <TableCell>{r.name}</TableCell>
                                                                            <TableCell>{desc}</TableCell>
                                                                            <TableCell>{r.rights}</TableCell>
                                                                        </TableRow>
                                                                    );
                                                                })
                                                            ) : (
                                                                <TableRow hover>
                                                                    <TableCell colSpan={3} align="center" sx={{ color: 'text.secondary' }}>
                                                                        No methods defined for Data object.
                                                                    </TableCell>
                                                                </TableRow>
                                                            )}
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
                                                        onClick={() => handleOpenDetails(row.id)}
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

            <Dialog
                open={!!detailsObj}
                onClose={handleCloseDetails}
                maxWidth="lg"
                fullWidth
            >
                <DialogTitle sx={{ fontWeight: 'bold', display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
                    <Box>
                        <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                            {detailsObj?.name || 'Parameter'} Details
                        </Typography>
                        <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace' }}>
                            OBIS: {detailsObj?.obisCode || 'N/A'}
                        </Typography>
                    </Box>
                    {detailsObj && (
                        <Button
                            variant="contained"
                            color="secondary"
                            onClick={() => handleScanSingleObject(detailsObj.id)}
                            disabled={discovering}
                            size="small"
                        >
                            {discovering ? 'Scanning...' : 'Scan Object'}
                        </Button>
                    )}
                </DialogTitle>
                <Divider />
                <DialogContent>
                    {detailsObj && (
                        <Stack spacing={3}>
                            <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
                                <Tabs
                                    value={activeTab}
                                    onChange={(e, newValue) => setActiveTab(newValue)}
                                    aria-label="parameter details tabs"
                                >
                                    <Tab label="Data" />
                                    <Tab label="Last Errors" />
                                    <Tab label="Access Rights" />
                                    <Tab label="Method Access Rights" />
                                    {isEventStatusRow(detailsObj) && <Tab label="Event Status" />}
                                </Tabs>
                            </Box>

                            {activeTab === 0 && (
                                <Box sx={{ py: 2 }}>
                                    {renderGenericCustomForm(detailsObj)}
                                </Box>
                            )}

                            {activeTab === 1 && (
                                <Box sx={{ py: 2 }}>
                                    {(() => {
                                        const errors = (detailsObj.allAttributes || []).filter((attr: any) => {
                                            const value = attr.value ?? attr.Value ?? '';
                                            return value?.startsWith?.('Error') || value?.toLowerCase?.().includes('fail');
                                        });

                                        if (errors.length === 0) {
                                            const hasScanned = (detailsObj.allAttributes || []).some((attr: any) => {
                                                const value = attr.value ?? attr.Value;
                                                return value && value !== 'Waiting...' && value !== 'Scanning...';
                                            });

                                            return (
                                                <Alert severity={hasScanned ? 'success' : 'info'}>
                                                    {hasScanned
                                                        ? 'All attributes were read successfully without any errors.'
                                                        : 'No scan has been performed yet. Click Scan Object to read parameter values.'}
                                                </Alert>
                                            );
                                        }

                                        return (
                                            <Stack spacing={2}>
                                                <Alert severity="error">
                                                    Found {errors.length} error(s) during the last scan of this object.
                                                </Alert>
                                                <TableContainer component={Paper} variant="outlined">
                                                    <Table size="small">
                                                        <TableHead>
                                                            <TableRow>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Attribute Index</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Description</TableCell>
                                                                <TableCell sx={{ fontWeight: 'bold' }}>Last error</TableCell>
                                                            </TableRow>
                                                        </TableHead>
                                                        <TableBody>
                                                            {errors.map((err: any) => {
                                                                const attrId = err.AttributeId ?? err.attributeId;
                                                                return (
                                                                    <TableRow key={attrId}>
                                                                        <TableCell>{attrId}</TableCell>
                                                                        <TableCell>{err.Name || err.name || `Attribute ${attrId}`}</TableCell>
                                                                        <TableCell sx={{ color: '#d32f2f' }}>
                                                                            {err.value ?? err.Value}
                                                                        </TableCell>
                                                                    </TableRow>
                                                                );
                                                            })}
                                                        </TableBody>
                                                    </Table>
                                                </TableContainer>
                                            </Stack>
                                        );
                                    })()}
                                </Box>
                            )}

                            {activeTab === 2 && (
                                <Box sx={{ py: 2 }}>
                                    {(() => {
                                        const assocObj = associationObjectList.find((item: any) => item.LogicalName === detailsObj.obisCode);
                                        const rights = assocObj ? parseAccessRights(assocObj.AttributeAccess) : [];
                                        const fallbackRows = getAttributeRows(detailsObj);
                                        const accessRows = rights.length > 0
                                            ? rights.map((r: any) => {
                                                const matchedAttr = fallbackRows.find((f: any) => String(f.attributeId) === r.name);
                                                return {
                                                    index: r.name,
                                                    description: matchedAttr?.name || `Attribute ${r.name}`,
                                                    access: r.rights,
                                                    type: matchedAttr?.dataType || 'None',
                                                    required: isAttributeReadRequired(detailsObj, r.name, r.rights),
                                                    source: getAttributeSourceText(detailsObj, r.name)
                                                };
                                            })
                                            : fallbackRows.map((row: any) => ({
                                                index: String(row.attributeId),
                                                description: row.name,
                                                access: row.accessType || (row.attributeId === 1 ? 'Read' : 'ReadWrite'),
                                                type: row.dataType || 'None',
                                                required: isAttributeReadRequired(
                                                    detailsObj,
                                                    row.attributeId,
                                                    row.accessType || (row.attributeId === 1 ? 'Read' : 'ReadWrite')
                                                ),
                                                source: getAttributeSourceText(detailsObj, row.attributeId)
                                            }));

                                        return (
                                            <TableContainer component={Paper} variant="outlined">
                                                <Table size="small">
                                                    <TableHead>
                                                        <TableRow>
                                                            <TableCell sx={{ fontWeight: 'bold' }}>Attribute Index</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold' }}>Description</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold' }}>Access</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold' }}>Required</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold' }}>Source</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold' }}>Access Selector</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold', textAlign: 'center' }}>Static</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold' }}>Type</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold' }}>UIType</TableCell>
                                                        </TableRow>
                                                    </TableHead>
                                                    <TableBody>
                                                        {accessRows.map((row: any, ri: number) => (
                                                            <TableRow key={ri} hover>
                                                                <TableCell>{row.index}</TableCell>
                                                                <TableCell>{row.description}</TableCell>
                                                                <TableCell>{row.access}</TableCell>
                                                                <TableCell sx={{ color: row.required ? 'warning.main' : 'text.secondary' }}>
                                                                    {getRequiredText(row.required)}
                                                                </TableCell>
                                                                <TableCell sx={{ color: row.source === 'Device error' ? 'error.main' : 'text.secondary' }}>
                                                                    {row.source}
                                                                </TableCell>
                                                                <TableCell></TableCell>
                                                                <TableCell align="center">
                                                                    <Checkbox size="small" disabled checked={false} />
                                                                </TableCell>
                                                                <TableCell sx={{ color: 'text.secondary' }}>{row.type}</TableCell>
                                                                <TableCell sx={{ color: 'text.secondary' }}>None</TableCell>
                                                            </TableRow>
                                                        ))}
                                                    </TableBody>
                                                </Table>
                                            </TableContainer>
                                        );
                                    })()}
                                </Box>
                            )}

                            {activeTab === 3 && (
                                <Box sx={{ py: 2 }}>
                                    {(() => {
                                        const assocObj = associationObjectList.find((item: any) => item.LogicalName === detailsObj.obisCode);
                                        const rights = assocObj ? parseAccessRights(assocObj.MethodAccess) : [];

                                        return (
                                            <TableContainer component={Paper} variant="outlined">
                                                <Table size="small">
                                                    <TableHead>
                                                        <TableRow>
                                                            <TableCell sx={{ fontWeight: 'bold', width: '20%' }}>Method Index</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold', width: '50%' }}>Description</TableCell>
                                                            <TableCell sx={{ fontWeight: 'bold', width: '30%' }}>Method access</TableCell>
                                                        </TableRow>
                                                    </TableHead>
                                                    <TableBody>
                                                        {rights.length > 0 ? (
                                                            rights.map((r, ri) => (
                                                                <TableRow key={ri} hover>
                                                                    <TableCell>{r.name}</TableCell>
                                                                    <TableCell>{getMethodDescription(detailsObj.objectType, r.name)}</TableCell>
                                                                    <TableCell>{r.rights}</TableCell>
                                                                </TableRow>
                                                            ))
                                                        ) : (
                                                            <TableRow hover>
                                                                <TableCell colSpan={3} align="center" sx={{ color: 'text.secondary' }}>
                                                                    No methods defined for {detailsObj.objectType || 'this'} object.
                                                                </TableCell>
                                                            </TableRow>
                                                        )}
                                                    </TableBody>
                                                </Table>
                                            </TableContainer>
                                        );
                                    })()}
                                </Box>
                            )}

                            {activeTab === 4 && isEventStatusRow(detailsObj) && (
                                <Box sx={{ py: 2 }}>
                                    {renderEventStatusDetails(detailsObj)}
                                </Box>
                            )}
                        </Stack>
                    )}
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={handleCloseDetails} variant="contained" color="primary">
                        Close
                    </Button>
                </DialogActions>
            </Dialog>

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
