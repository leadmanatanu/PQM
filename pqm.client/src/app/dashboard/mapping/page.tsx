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
    const isDataObjectType = selectedHeader?.name === 'Data';

    const [discoveredParams, setDiscoveredParams] = useState<any[]>([]);
    const [discovering, setDiscovering] = useState<boolean>(false);
    
    const [openSnackbar, setOpenSnackbar] = useState(false);
    const [displayMsg, setDisplayMsg] = useState<string | null>(null);

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
                const mapped = result.data.map((obj: any) => ({
                    id: obj.id,
                    name: `${obj.obisCode} ${obj.name}`,
                    obisCode: obj.obisCode,
                    objectType: obj.objectType,
                    value: obj.attribute2 || 'Waiting...',
                    attribute3: obj.attribute3 || 'Waiting...'
                }));
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
                        const valItem = result.data.find((item: any) => item.attributeId === 2);
                        const unitItem = result.data.find((item: any) => item.attributeId === 3);

                        next[i] = {
                            ...next[i],
                            value: valItem ? valItem.value : 'Error',
                            attribute3: unitItem ? unitItem.value : next[i].attribute3
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
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {discoveredParams.map((row, idx) => (
                                        <TableRow key={idx} hover>
                                            <TableCell>{row.name}</TableCell>
                                            <TableCell>{row.objectType}</TableCell>
                                            <TableCell sx={{ fontWeight: 'bold', color: 'primary.main' }}>
                                                {row.value || 'N/A'}
                                            </TableCell>
                                            {!isDataObjectType && (
                                                <TableCell sx={{ color: 'text.secondary', fontFamily: 'monospace' }}>
                                                    {row.attribute3 || 'N/A'}
                                                </TableCell>
                                            )}
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </CardContent>
                </Card>
            )}

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
