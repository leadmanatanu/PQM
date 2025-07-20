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


export interface Device {
    id: number;
    name: string;
    ip: string;
    port: number;
    isActive: string;
    isDeleted: string;
    createdDate: Date;
    createdId: number;
    modifiedDate: Date;
    modifiedId: number;
}

interface DeviceFiltersProps {
  rows: Device[];
  onDeviceSelect?: (id: string | number) => void;
}

  export function DeviceFilters({
  rows = [],
  onDeviceSelect = () => {},
}: DeviceFiltersProps): React.JSX.Element {
  const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
  const [startValue, setStartValue] = React.useState<Dayjs | null>(dayjs('2022-04-17T15:30'));
  const [endValue, setEndValue] = React.useState<Dayjs | null>(dayjs('2022-04-17T15:30'));

  const handleChange = (
    event: React.SyntheticEvent, // Event object (can be null for some actions)
    newValue: Device | null // The selected Device object, or null if cleared
  ) => {
    setSelectedDevice(newValue); // Update internal state with the selected object
    onDeviceSelect(newValue ? newValue.id : null); // Pass string ID or null
  };
// <Card sx={{ p: 2, maxWidth: '700px' }}>
  return (
   <Card>
    <Divider />
    <CardContent>
    <Stack spacing={3} sx={{ maxWidth: 'sm' }}>
      <FormControl fullWidth>
        <Autocomplete
          id="device-filter-autocomplete" // Unique ID for accessibility
          options={rows} // Provide the full array of Device objects
          getOptionLabel={(device) => device.name} // Tell Autocomplete how to get the display string from a Device object
          value={selectedDevice} // The currently selected Device object (from state)
          onChange={handleChange} // Our handler for selection/clearing
          isOptionEqualToValue={(option, value) => option.id === value.id}
          renderInput={(params) => (
            <TextField
              {...params}
              label="Select or type to search device" // This serves as the label for the input
              variant="outlined" // Standard Material-UI TextField variants
            />
          )}
          openOnFocus
        />
      </FormControl>
      <FormControl fullWidth>
        <LocalizationProvider dateAdapter={AdapterDayjs}>
      <DemoContainer components={['DateTimePicker', 'DateTimePicker']}>
        <DateTimePicker
          label="Start Time"
          value={startValue}
          onChange={(newValue) => setStartValue(newValue)}
        />
        <DateTimePicker
          label="End Time"
          value={endValue}
          onChange={(newValue) => setEndValue(newValue)}
        />
      </DemoContainer>
    </LocalizationProvider>
      </FormControl>
      </Stack>
      </CardContent>
      <Divider />
        <CardActions sx={{ justifyContent: 'flex-end' }}>
          <Button variant="contained">Search</Button>
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