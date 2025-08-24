'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';  // ✅ add this line
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardActions from '@mui/material/CardActions';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Checkbox from '@mui/material/Checkbox';
import Divider from '@mui/material/Divider';
import FormControlLabel from '@mui/material/FormControlLabel';
import FormGroup from '@mui/material/FormGroup';
import Stack from '@mui/material/Stack';
import Grid from '@mui/material/Grid';

export interface DeviceParam {
    id: number;
    name: string;
    isSelected: boolean;
}
export function DeviceParameter({
    device,
    onDeviceUpdate
}: {
    device: DeviceParam[];
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

    const handleSubmit = (event: React.FormEvent) => {
        event.preventDefault();
        onDeviceUpdate?.(updatedDevice);
    };

    return (
        <form onSubmit={handleSubmit}>
            <Card>
                <CardHeader title="Device Information" />
                <Divider />
                <CardContent sx={{
                    maxHeight: '400px',
                    overflowY: 'auto',
                    '&::-webkit-scrollbar': {
                        width: '8px',
                    },
                    '&::-webkit-scrollbar-track': {
                        background: '#f1f1f1',
                        borderRadius: '4px',
                    },
                    '&::-webkit-scrollbar-thumb': {
                        background: '#888',
                        borderRadius: '4px',
                    },
                    '&::-webkit-scrollbar-thumb:hover': {
                        background: '#555',
                    },
                }}>
                    {updatedDevice.length > 0 ? (
                        <Grid container spacing={6} wrap="wrap">
                            <Grid
                                size={{
                                    md: 4,
                                    sm: 6,
                                    xs: 12,
                                }}
                            >
                                <Stack spacing={1}>
                                    <FormGroup>
                                        {updatedDevice.map((row, index) => (
                                            <FormControlLabel
                                                key={row.id || index}
                                                control={
                                                    <Checkbox
                                                        checked={row.isSelected}
                                                        onChange={() => handleCheckboxChange(index)}
                                                    />
                                                }
                                                label={row.name}
                                            />
                                        ))}
                                    </FormGroup>
                                </Stack>
                            </Grid>
                            <Grid
                                size={{
                                    md: 4,
                                    sm: 6,
                                    xs: 12,
                                }}
                            >
                            </Grid>
                        </Grid>
                    ) : (
                        <CardHeader title="No device data available" />
                    )}
                </CardContent>
                {/*<CardContent sx={{ maxHeight: '400px', overflowY: 'auto' }}>*/}
                {/*    {updatedDevice.length > 0 ? (*/}
                {/*        <Grid container spacing={6} wrap="wrap">*/}
                {/*            <Grid md={4} sm={6} xs={12}>*/}
                {/*                <Stack spacing={1}>*/}
                {/*                    <FormGroup>*/}
                {/*                        {updatedDevice.map((row, index) => (*/}
                {/*                            <FormControlLabel*/}
                {/*                                key={row.id}*/}
                {/*                                control={*/}
                {/*                                    <Checkbox*/}
                {/*                                        checked={row.isSelected}*/}
                {/*                                        onChange={() => handleCheckboxChange(index)}*/}
                {/*                                    />*/}
                {/*                                }*/}
                {/*                                label={row.name}*/}
                {/*                            />*/}
                {/*                        ))}*/}
                {/*                    </FormGroup>*/}
                {/*                </Stack>*/}
                {/*            </Grid>*/}
                {/*        </Grid>*/}
                {/*    ) : (*/}
                {/*        <CardHeader title="No device data available" />*/}
                {/*    )}*/}
                {/*</CardContent>*/}
                <Divider />
                <CardActions sx={{ justifyContent: 'flex-end' }}>
                    <Button variant="contained" type="submit">
                        Save changes
                    </Button>
                </CardActions>
            </Card>
        </form>
    );
}
