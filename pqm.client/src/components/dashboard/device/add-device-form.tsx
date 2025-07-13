'use client';

import * as React from 'react';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardActions from '@mui/material/CardActions';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Divider from '@mui/material/Divider';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import OutlinedInput from '@mui/material/OutlinedInput';
import Stack from '@mui/material/Stack';
import { Select, MenuItem } from '@mui/material';
import { addDevice } from '../../../api/device'
import type { Device } from '@/components/dashboard/device/devices-table';

export function AddDeviceForm({ isVisible }): React.JSX.Element {
    //console.log("isVisible " + isVisible);

    const [selectedValue, setSelectedValue] = React.useState('1');
    const handleChange = (event) => {
        setSelectedValue(event.target.value);
    };
    const [txtName, setTxtName] = React.useState('');
    const handleNameChange = (event) => {
        setTxtName(event.target.value);
    };
    const [txtIP, setTxtIP] = React.useState('');
    const handleIPChange = (event) => {
        setTxtIP(event.target.value);
    };
    const [txtPort, setTxtPort] = React.useState('');
    const handlePortChange = (event) => {
        setTxtPort(event.target.value);
    };

    const [txtConsumerNo, setTxtConsumerNo] = React.useState('');
    const handleConChange = (event) => {
        setTxtConsumerNo(event.target.value);
    };

    const [txtSerialNo, setTxtSerialNo] = React.useState('');
    const handleSerChange = (event) => {
        setTxtSerialNo(event.target.value);
    };

    const [txtFtpFolder, setTxtFtpFolder] = React.useState('');
    const handleFtpChange = (event) => {
        setTxtFtpFolder(event.target.value);
    };

    const addDeviceClick = (e) => {
        e.preventDefault(); // Prevent default Link navigation
        //router.push('/another-page'); // Programmatic navigation
        const device: Device = {
            name: txtName,
            isActive: selectedValue,
            ip: txtIP,
            port: txtPort,
            consumerNo : txtConsumerNo,
            serialNo : txtSerialNo,
            ftpFolder : txtFtpFolder
            };
        //console.log(device);
        addDevice(device);
        setTxtName('');
        setTxtIP('');
        setTxtPort('');
        setTxtConsumerNo('');
        setTxtSerialNo('');
        setTxtFtpFolder('');
    };

    return (
        <form
            onSubmit={(event) => {
                event.preventDefault();
            }}
        >
            <Card>
                <CardHeader title="Add device" />
                <Divider />
                <CardContent>
                    <Stack spacing={3} sx={{ maxWidth: 'sm' }}>
                        <FormControl fullWidth>
                            <InputLabel>Device</InputLabel>
                            <OutlinedInput label="Device" name="device" type="device" value={txtName} onChange={handleNameChange} />
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel>Serial No</InputLabel>
                            <OutlinedInput label="Serial No" name="serialNo" type="serialNo" value={txtSerialNo} onChange={handleSerChange} />
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel>Consumer No</InputLabel>
                            <OutlinedInput label="Consumer No" name="ConsumerNo" type="ConsumerNo" value={txtConsumerNo} onChange={handleConChange} />
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel>FTP Folder</InputLabel>
                            <OutlinedInput label="Port" name="port" type="port" value={txtFtpFolder} onChange={handlePortChange} />
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel id="isactive-label">Select Option</InputLabel>
                            <Select
                                labelId="isactive-label"
                                id="isactive"
                                name="isactive"
                                value={selectedValue}
                                label="Select Option"
                                onChange={handleChange}
                            >
                                <MenuItem value={1}>Active</MenuItem>
                                <MenuItem value={0}>Inactive</MenuItem>
                            </Select>
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel>IP</InputLabel>
                            <OutlinedInput label="IP" name="ip" type="ip" value={txtIP} onChange={handleIPChange} />
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel>PORT</InputLabel>
                            <OutlinedInput label="Port" name="port" type="port" value={txtPort} onChange={handlePortChange} />
                        </FormControl>
                        
                    </Stack>
                </CardContent>
                <Divider />
                <CardActions sx={{ justifyContent: 'flex-end' }}>
                    <Button variant="contained" onClick={addDeviceClick}>Add</Button>
                    <Button variant="contained">Cancel</Button>
                </CardActions>
            </Card>
        </form>
    );
}
