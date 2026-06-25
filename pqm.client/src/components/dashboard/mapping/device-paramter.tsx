'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardActions from '@mui/material/CardActions';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Checkbox from '@mui/material/Checkbox';
import Divider from '@mui/material/Divider';
import TableContainer from '@mui/material/TableContainer';
import Table from '@mui/material/Table';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TableCell from '@mui/material/TableCell';
import TableBody from '@mui/material/TableBody';
import Paper from '@mui/material/Paper';
import Box from '@mui/material/Box';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import Select from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';

export interface DeviceParam {
    id: number;
    name: string;
    obisCode?: string;
    objectType?: string;
    isSelected: boolean;
}

export function DeviceParameter({
    device,
    selectedObjectType = 'All',
    onDeviceUpdate
}: {
    device: DeviceParam[];
    selectedObjectType?: string;
    onDeviceUpdate?: (updatedDevice: DeviceParam[]) => void;
}): React.JSX.Element {
    const [updatedDevice, setUpdatedDevice] = useState<DeviceParam[]>(device);
 
    useEffect(() => {
        setUpdatedDevice(device);
    }, [device]);
 
    const handleCheckboxChange = (index: number) => {
        const newDevice = [...updatedDevice];
        newDevice[index] = { ...newDevice[index], isSelected: !newDevice[index].isSelected };
        setUpdatedDevice(newDevice);
    };
 
    const handleSelectAll = (event: React.ChangeEvent<HTMLInputElement>) => {
        const checked = event.target.checked;
        const newDevice = updatedDevice.map(item => ({ ...item, isSelected: checked }));
        setUpdatedDevice(newDevice);
    };
 
    const handleSubmit = (event: React.FormEvent) => {
        event.preventDefault();
        onDeviceUpdate?.(updatedDevice);
    };
 
    // Filter displayed devices based on selected ObjectType
    const displayedDevice = selectedObjectType === 'All' 
        ? updatedDevice 
        : updatedDevice.filter(item => (item.objectType || 'N/A') === selectedObjectType);
 
    const allSelected = displayedDevice.length > 0 && displayedDevice.every(item => item.isSelected);
    const someSelected = displayedDevice.length > 0 && displayedDevice.some(item => item.isSelected) && !allSelected;
 
    return (
        <form onSubmit={handleSubmit}>
            <Card>
                <CardHeader title="Device Parameters Mapping" subheader="Select parameters to map/monitor on this device" />
                <Divider />
                <CardContent sx={{ p: 0 }}>
                    {displayedDevice.length > 0 ? (
                        <TableContainer component={Paper} sx={{ maxHeight: '400px', boxShadow: 'none' }}>
                            <Table stickyHeader aria-label="device parameters mapping table">
                                <TableHead>
                                    <TableRow>
                                        <TableCell padding="checkbox">
                                            <Checkbox
                                                indeterminate={someSelected}
                                                checked={allSelected}
                                                onChange={handleSelectAll}
                                            />
                                        </TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Parameter Name</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>OBIS Code</TableCell>
                                        <TableCell sx={{ fontWeight: 'bold' }}>Object Type</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {displayedDevice.map((row) => {
                                        // find original index to update correctly
                                        const originalIndex = updatedDevice.findIndex(item => item.id === row.id);
                                        return (
                                            <TableRow key={row.id} hover>
                                                <TableCell padding="checkbox">
                                                    <Checkbox
                                                        checked={row.isSelected}
                                                        onChange={() => handleCheckboxChange(originalIndex)}
                                                    />
                                                </TableCell>
                                                <TableCell>{row.name}</TableCell>
                                                <TableCell sx={{ fontFamily: 'monospace', color: 'text.secondary' }}>
                                                    {row.obisCode || 'N/A'}
                                                </TableCell>
                                                <TableCell>{row.objectType || 'N/A'}</TableCell>
                                            </TableRow>
                                        );
                                    })}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    ) : (
                        <Box sx={{ p: 2 }}>
                            No parameters match the selected Object Type.
                        </Box>
                    )}
                </CardContent>
                <Divider />
                <CardActions sx={{ justifyContent: 'flex-end', p: 2 }}>
                    <Button variant="contained" type="submit">
                        Save changes
                    </Button>
                </CardActions>
            </Card>
        </form>
    );
}
