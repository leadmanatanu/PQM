'use client';

import type { Device } from '@/components/dashboard/device/devices-table';
import React, { useState } from 'react';
import {
    FormControl,
    TextField,
    Card,
} from '@mui/material';
import { Autocomplete } from '@mui/material';

interface DeviceFiltersProps {
    rows: Device[];
    onDeviceSelect?: (id: string | number) => void;
}

export function DeviceFilters({
    rows = [],
    onDeviceSelect = () => { },
}: DeviceFiltersProps): React.JSX.Element {
    const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);

    const handleChange = (
        event: React.SyntheticEvent,
        newValue: Device | null,
    ) => {
        setSelectedDevice(newValue);
        onDeviceSelect(newValue ? newValue.id : 0); // send 0 if cleared
    };

    return (
        <Card sx={{ p: 2, maxWidth: '500px' }}>
            <FormControl fullWidth>
                <Autocomplete
                    id="device-filter-autocomplete"
                    options={rows}
                    getOptionLabel={(device) => device.name}
                    value={selectedDevice}
                    onChange={handleChange}
                    isOptionEqualToValue={(option, value) => option.id === value.id}
                    renderInput={(params) => (
                        <TextField
                            {...params}
                            label="Select or type to search device"
                            variant="outlined"
                        />
                    )}
                    openOnFocus
                />
            </FormControl>
        </Card>
    );
}
