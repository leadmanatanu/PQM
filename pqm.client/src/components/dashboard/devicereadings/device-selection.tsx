"use client"; // <--- Add this line at the very top!
import React, { useState } from 'react';
import {
    FormControl,
    InputLabel, // We'll replace this with label on TextField for Autocomplete
    Select,      // This will be removed
    MenuItem,    // This will be removed
    TextField,   // Needed for Autocomplete's input
} from '@mui/material';
import { Autocomplete } from '@mui/material'; // Import Autocomplete
import { MagnifyingGlassIcon } from '@phosphor-icons/react/dist/ssr/MagnifyingGlass';
import Card from '@mui/material/Card';
import CardActions from '@mui/material/CardActions';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Divider from '@mui/material/Divider';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';

import dayjs, { Dayjs } from 'dayjs';
import { DemoContainer } from '@mui/x-date-pickers/internals/demo';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';


import type { Device } from '@/components/dashboard/device/devices-table';

// export interface Device {
//     id: number;
//     name: string;
//     ip: string;
//     port: number;
//     isActive: string;
//     isDeleted: string;
//     createdDate: Date;
//     createdId: number;
//     modifiedDate: Date;
//     modifiedId: number;
// }

interface DeviceFiltersProps {
    rows: Device[];
    onDeviceSelect?: (id: string | number) => void;
    paramArray: any[];
    onSearch?: (searchParams: {
        deviceId: string | number | null;
        startTime: Dayjs | null;
        endTime: Dayjs | null;
        paramId: string | number | null;
    }) => void;
}

export function DeviceFilters({
    rows = [],
    onDeviceSelect = () => { },
    paramArray = [],
    onSearch = () => { },
}: DeviceFiltersProps): React.JSX.Element {
    const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
    const [endValue, setEndValue] = useState<Dayjs | null>(dayjs());
    const [startValue, setStartValue] = useState<Dayjs | null>(
        dayjs().subtract(1, "day")
    );
    const [selectedParam, setSelectedParam] = useState<any | null>(null);

    // Validation states
    const [errors, setErrors] = useState({
        device: false,
        paramId: false,
        start: false,
        end: false,
    });

    const handleChange = (
        event: React.SyntheticEvent, // Event object (can be null for some actions)
        newValue: Device | null // The selected Device object, or null if cleared
    ) => {
        setSelectedDevice(newValue); // Update internal state with the selected object
        onDeviceSelect(newValue ? newValue.id : 0); // Pass string ID or null
    };

    const handleParameterChange = (
        event: React.SyntheticEvent, // Event object (can be null for some actions)
        newValue: any | null // The selected Device object, or null if cleared
    ) => {
        setSelectedParam(newValue); // Update internal state with the selected object
        //onDeviceSelect(newValue ? newValue.id : null); // Pass string ID or null
    };

    const handleSearch = () => {
        console.log("handleSearch");
        const newErrors = {
            device: !selectedDevice,
            paramId: !selectedParam,
            start: !startValue,
            end: !endValue,
        };
        setErrors(newErrors);

        if (Object.values(newErrors).some(Boolean)) return;
        onSearch({
            deviceId: selectedDevice ? selectedDevice.id : null,
            startTime: startValue,
            endTime: endValue,
            paramId: selectedParam ? selectedParam.id : null,
        });
    };

    // <Card sx={{ p: 2, maxWidth: '700px' }}>
    return (
        <Card sx={{ maxWidth: '600px', width: '100%', borderRadius: '8px' }}>
            <Divider />
            <CardContent sx={{ p: 2 }}>
                <Stack spacing={2} sx={{ maxWidth: '100%' }}>
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
                                    error={errors.device}
                                    helperText={errors.device ? "Device is required" : ""}
                                />
                            )}
                            openOnFocus
                        />
                    </FormControl>
                    <FormControl fullWidth size="small">
                        <Autocomplete
                            id="parameter-filter-autocomplete"
                            options={paramArray}
                            size="small"
                            getOptionLabel={(param) => param.name}
                            value={selectedParam}
                            onChange={handleParameterChange}
                            isOptionEqualToValue={(option, value) => option.id === value.id}
                            renderInput={(params) => (
                                <TextField
                                    {...params}
                                    label="Select or type to parameter"
                                    variant="outlined"
                                    size="small"
                                    error={errors.paramId}
                                    helperText={errors.paramId ? "Parameter is required" : ""}
                                />
                            )}
                            openOnFocus
                        />
                    </FormControl>
                    <FormControl fullWidth>
                        <LocalizationProvider dateAdapter={AdapterDayjs}>
                            <DemoContainer components={['DatePicker', 'DatePicker']}>
                                <DatePicker
                                    label="Start Date"
                                    value={startValue}
                                    onChange={(newValue) => setStartValue(newValue)}
                                    slotProps={{
                                        textField: {
                                            size: 'small',
                                            error: errors.start,
                                            helperText: errors.start
                                                ? "Start date is required"
                                                : "",
                                        },
                                    }}
                                />
                                <DatePicker
                                    label="End Date"
                                    value={endValue}
                                    onChange={(newValue) => setEndValue(newValue)}
                                    slotProps={{
                                        textField: {
                                            size: 'small',
                                            error: errors.end,
                                            helperText: errors.end ? "End date is required" : "",
                                        },
                                    }}
                                />
                            </DemoContainer>
                        </LocalizationProvider>
                    </FormControl>
                </Stack>
            </CardContent>
            <Divider />
            <CardActions sx={{ justifyContent: 'flex-end', py: 1, px: 2 }}>
                <Button variant="contained" size="small" onClick={handleSearch}>
                    Search
                </Button>
            </CardActions>
        </Card>
    );
}


// export function DeviceFilters({
//   rows = [],
//   onDeviceSelect = () => {},
// }: DeviceFiltersProps): React.JSX.Element {
//   // We'll manage the *selected Device object* internally for Autocomplete.
//   // We need to derive this from an initial `selectedId` if available, or set to null.
//   const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);

//   // If you had an initial `selectedId` coming into DeviceFilters, you'd use useEffect
//   // to set the initial `selectedDevice` state based on `rows`.
//   // Example if DeviceFiltersProps had an optional `initialSelectedId: string | number;`
//   // useEffect(() => {
//   //   if (initialSelectedId) {
//   //     const foundDevice = rows.find(d => d.id === initialSelectedId);
//   //     setSelectedDevice(foundDevice || null);
//   //   }
//   // }, [initialSelectedId, rows]);


//   // Autocomplete's onChange handler provides the selected *object* or null.
//   // We then extract the ID to match your existing onDeviceSelect callback.
//   const handleChange = (
//     event: React.SyntheticEvent, // Event object (can be null for some actions)
//     newValue: Device | null      // The selected Device object, or null if cleared
//   ) => {
//     setSelectedDevice(newValue); // Update internal state with the selected object

//     // Call the parent's callback, passing the ID (or null if cleared)
//     if (onDeviceSelect) {
//       onDeviceSelect(newValue ? newValue.id : null);
//     }
//   };

//   return (
//     <Card sx={{ p: 2, maxWidth: '500px' }}>
//       <FormControl fullWidth>
//         {/* InputLabel and Select are removed. Autocomplete uses TextField for its input. */}
//         {/* <InputLabel id="device-select-label">Select device</InputLabel> */}

//         <Autocomplete
//           id="device-filter-autocomplete" // Unique ID for accessibility
//           options={rows} // Provide the full array of Device objects
//           getOptionLabel={(device) => device.name} // Tell Autocomplete how to get the display string from a Device object
//           value={selectedDevice} // The currently selected Device object (from state)
//           onChange={handleChange} // Our handler for selection/clearing
//           // Crucial for objects: tells Autocomplete how to compare an option from `options`
//           // with the `value` prop to determine if they represent the same item.
//           isOptionEqualToValue={(option, value) => option.id === value.id}
//           renderInput={(params) => (
//             // TextField replaces InputLabel and provides the input field
//             <TextField
//               {...params}
//               label="Select or type to search device" // This serves as the label for the input
//               variant="outlined" // Standard Material-UI TextField variants
//             />
//           )}
//           // Optional: Makes the dropdown open when the input is focused, like a standard Select
//           openOnFocus
//           // Optional: To customize how each option is rendered in the dropdown
//           // renderOption={(props, option) => (
//           //   <li {...props} key={option.id}>
//           //     {option.name}
//           //     {/* You could add more info here, e.g., device.status */}
//           //   </li>
//           // )}
//         />
//       </FormControl>
//     </Card>
//   );
// }