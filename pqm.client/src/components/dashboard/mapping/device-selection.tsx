import type { Device } from '@/components/dashboard/device/devices-table';
import React, { useState, useEffect } from 'react';
import {
    FormControl,
    TextField,
    Card,
    Autocomplete,
    Stack,
    FormControlLabel,
    Checkbox,
    Typography,
    Chip,
    Button,
    Box,
} from '@mui/material';

interface DeviceFiltersProps {
    rows: Device[];
    onDeviceSelect?: (id: string | number) => void;
    onSelectParametersClick?: () => void;
}

export function DeviceFilters({
    rows = [],
    onDeviceSelect = () => { },
    onSelectParametersClick = () => { },
}: DeviceFiltersProps): React.JSX.Element {
    const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
    const [showOnlyConnected, setShowOnlyConnected] = useState<boolean>(true);

    // Sync local selected device state when rows change
    useEffect(() => {
        if (selectedDevice) {
            const current = rows.find(r => r.id === selectedDevice.id);
            if (current) {
                setSelectedDevice(current);
            }
        }
    }, [rows, selectedDevice]);

    const handleChange = (
        event: React.SyntheticEvent,
        newValue: Device | null,
    ) => {
        setSelectedDevice(newValue);
        onDeviceSelect(newValue ? newValue.id : 0);
    };

    const displayedOptions = showOnlyConnected 
        ? rows.filter(device => device.isConnected)
        : rows;

    return (
        <Card sx={{ p: 3, maxWidth: '500px', width: '100%', borderRadius: '8px' }}>
            <Stack direction="column" spacing={2}>
                <FormControl fullWidth size="small">
                    <Autocomplete
                        id="device-filter-autocomplete"
                        options={displayedOptions}
                        size="small"
                        getOptionLabel={(device) => `${device.name} (Id: ${device.id})`}
                        value={selectedDevice}
                        onChange={handleChange}
                        isOptionEqualToValue={(option, value) => option.id === value.id}
                        renderInput={(params) => (
                            <TextField
                                {...params}
                                label="Select or type to search device"
                                variant="outlined"
                                size="small"
                             />
                        )}
                        openOnFocus
                    />
                </FormControl>

                <FormControlLabel
                    control={
                        <Checkbox
                            checked={showOnlyConnected}
                            onChange={(e) => setShowOnlyConnected(e.target.checked)}
                            color="primary"
                            size="small"
                        />
                    }
                    label={
                        <Typography variant="body2" color="text.secondary">
                            Show only connected devices
                        </Typography>
                    }
                />

                {selectedDevice && (
                    <Box sx={{ mt: 1, p: 2, bgcolor: 'var(--mui-palette-neutral-50)', borderRadius: '6px', border: '1px solid var(--mui-palette-divider)' }}>
                        <Stack spacing={1.5}>
                            <Stack direction="row" justifyContent="space-between" alignItems="center">
                                <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                                    {selectedDevice.name}
                                </Typography>
                                <Chip
                                    label={selectedDevice.isConnected ? 'Connected' : 'Disconnected'}
                                    color={selectedDevice.isConnected ? 'success' : 'error'}
                                    size="small"
                                    variant="outlined"
                                />
                            </Stack>
                            <Typography variant="caption" color="text.secondary" display="block">
                                <strong>IP:</strong> {selectedDevice.ip} | <strong>Port:</strong> {selectedDevice.port}
                            </Typography>
                            <Typography variant="caption" color="text.secondary" display="block">
                                <strong>Serial No:</strong> {selectedDevice.serialNumber || 'N/A'} | <strong>Account No:</strong> {selectedDevice.consumerNumber || 'N/A'}
                            </Typography>
                            {selectedDevice.isConnected && (
                                <Button
                                    variant="contained"
                                    size="small"
                                    color="primary"
                                    sx={{ mt: 1, textTransform: 'none' }}
                                    onClick={onSelectParametersClick}
                                >
                                    Select Parameters to Scan
                                </Button>
                            )}
                        </Stack>
                    </Box>
                )}
            </Stack>
        </Card>
    );
}
