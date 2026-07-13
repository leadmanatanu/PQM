'use client';
 
import type { Device } from '@/components/dashboard/device/devices-table';
import React, { useState } from 'react';
import {
    FormControl,
    TextField,
    Card,
    Autocomplete,
    Select,
    MenuItem,
    InputLabel,
    Stack,
} from '@mui/material';
 
interface DeviceFiltersProps {
    rows: Device[];
    onDeviceSelect?: (id: string | number) => void;
    headers?: any[];
    selectedHeaderId?: string | number;
    onHeaderSelect?: (id: string | number) => void;
    objects?: any[];
    selectedObjectId?: string | number;
    onObjectSelect?: (id: string | number) => void;
}
 
export function DeviceFilters({
    rows = [],
    onDeviceSelect = () => { },
    headers = [],
    selectedHeaderId = '',
    onHeaderSelect = () => { },
    objects = [],
    selectedObjectId = '',
    onObjectSelect = () => { },
}: DeviceFiltersProps): React.JSX.Element {
    const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
 
    const handleChange = (
        event: React.SyntheticEvent,
        newValue: Device | null,
    ) => {
        setSelectedDevice(newValue);
        onDeviceSelect(newValue ? newValue.id : 0);
    };
 
    return (
        <Card sx={{ p: 2, maxWidth: '500px', width: '100%', borderRadius: '8px' }}>
            <Stack direction="column" spacing={2}>
                <FormControl fullWidth size="small">
                    <Autocomplete
                        id="device-filter-autocomplete"
                        options={rows}
                        size="small"
                        getOptionLabel={(device) => device.name}
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
                {selectedDevice && headers.length > 0 && (
                    <FormControl fullWidth size="small">
                        <InputLabel id="header-select-label">Select Object Type</InputLabel>
                        <Select
                            labelId="header-select-label"
                            id="header-select"
                            value={selectedHeaderId}
                            label="Select Object Type"
                            size="small"
                            onChange={(e) => onHeaderSelect(e.target.value as string | number)}
                        >
                            {headers.map((h) => (
                                <MenuItem key={h.id} value={h.id}>{h.name}</MenuItem>
                            ))}
                        </Select>
                    </FormControl>
                )}
                {/*{selectedDevice && selectedHeaderId && objects.length > 0 && (*/}
                {/*    <FormControl fullWidth>*/}
                {/*        <InputLabel id="object-select-label">Select Object</InputLabel>*/}
                {/*        <Select*/}
                {/*            labelId="object-select-label"*/}
                {/*            id="object-select"*/}
                {/*            value={selectedObjectId}*/}
                {/*            label="Select Object"*/}
                {/*            onChange={(e) => onObjectSelect(e.target.value as string | number)}*/}
                {/*        >*/}
                {/*            {objects.map((obj) => (*/}
                {/*                <MenuItem key={obj.id} value={obj.id}>*/}
                {/*                    {obj.obisCode} {obj.name}*/}
                {/*                </MenuItem>*/}
                {/*            ))}*/}
                {/*        </Select>*/}
                {/*    </FormControl>*/}
                {/*)}*/}
            </Stack>
        </Card>
    );
}
