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
import FormHelperText from '@mui/material/FormHelperText';
import Stack from '@mui/material/Stack';
import { Select, MenuItem } from '@mui/material';
import { SelectChangeEvent } from '@mui/material/Select';
import { addDevice, editDevice } from '../../../api/device';
import type { Device } from '@/components/dashboard/device/devices-table';

interface AddDeviceFormProps {
    show?: boolean;
    onToggleVisibility: (device: Device | null) => void;
    editingDevice: Device | null;
    setEditingDevice: (device: Device | null) => void;
}

export function AddDeviceForm({
    show = true,
    onToggleVisibility,
    editingDevice,
    setEditingDevice,
}: AddDeviceFormProps): React.JSX.Element | null {
    const [selectedValue, setSelectedValue] = React.useState<'1' | '0'>('1');
    const [txtName, setTxtName] = React.useState('');
    const [txtIP, setTxtIP] = React.useState('');
    const [txtPort, setTxtPort] = React.useState('');
    const [txtConsumerNo, setTxtConsumerNo] = React.useState('');
    const [txtSerialNo, setTxtSerialNo] = React.useState('');
    const [txtFtpFolder, setTxtFtpFolder] = React.useState('');

    const [errors, setErrors] = React.useState({
        name: '',
        serialNo: '',
        consumerNo: '',
        ftpFolder: '',
        ip: '',
        port: '',
        general: '',
    });

    React.useEffect(() => {
        if (editingDevice) {
            setTxtName(editingDevice.name || '');
            setTxtIP(editingDevice.ip || '');
            setTxtPort(String(editingDevice.port ?? ''));
            setTxtConsumerNo(editingDevice.consumerNumber || '');
            setTxtSerialNo(editingDevice.serialNumber || '');
            setTxtFtpFolder(editingDevice.ftpFolder || '');
            setSelectedValue(editingDevice.isActive ? '1' : '0'); // normalize
        } else {
            setTxtName('');
            setTxtIP('');
            setTxtPort('');
            setTxtConsumerNo('');
            setTxtSerialNo('');
            setTxtFtpFolder('');
            setSelectedValue('1');
            setErrors({
                name: '',
                serialNo: '',
                consumerNo: '',
                ftpFolder: '',
                ip: '',
                port: '',
                general: '',
            });
        }
    }, [editingDevice]);

    if (!show) return null;

    // ---- Handlers ----
    const handleChange = (event: SelectChangeEvent<string>) => {
        setSelectedValue(event.target.value as '1' | '0');
        setErrors((prev) => ({ ...prev, general: '' }));
    };

    const handleNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtName(event.target.value);
        setErrors((prev) => ({ ...prev, name: '', general: '' }));
    };

    const handleIPChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtIP(event.target.value);
        setErrors((prev) => ({ ...prev, ip: '', general: '' }));
    };

    const handlePortChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtPort(event.target.value);
        setErrors((prev) => ({ ...prev, port: '', general: '' }));
    };

    const handleConChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtConsumerNo(event.target.value);
        setErrors((prev) => ({ ...prev, consumerNo: '', general: '' }));
    };

    const handleSerChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtSerialNo(event.target.value);
        setErrors((prev) => ({ ...prev, serialNo: '', general: '' }));
    };

    const handleFtpChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtFtpFolder(event.target.value);
        setErrors((prev) => ({ ...prev, ftpFolder: '', general: '' }));
    };

    // ---- Validation ----
    const validateForm = (): boolean => {
        const newErrors = {
            name: '',
            serialNo: '',
            consumerNo: '',
            ftpFolder: '',
            ip: '',
            port: '',
            general: '',
        };
        let isValid = true;

        if (!txtName.trim()) {
            newErrors.name = 'Device name is required';
            isValid = false;
        }

        if (!txtSerialNo.trim()) {
            newErrors.serialNo = 'Serial number is required';
            isValid = false;
        }

        if (!txtConsumerNo.trim()) {
            newErrors.consumerNo = 'Consumer number is required';
            isValid = false;
        }

        if (!txtFtpFolder.trim()) {
            newErrors.ftpFolder = 'FTP folder is required';
            isValid = false;
        }

        const ipRegex =
            /^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;
        if (!txtIP.trim()) {
            newErrors.ip = 'IP address is required';
            isValid = false;
        } else if (!ipRegex.test(txtIP)) {
            newErrors.ip = 'Invalid IP address format';
            isValid = false;
        }

        const portNum = parseInt(txtPort, 10);
        if (!txtPort.trim()) {
            newErrors.port = 'Port is required';
            isValid = false;
        } else if (isNaN(portNum) || portNum < 1 || portNum > 65535) {
            newErrors.port = 'Port must be a number between 1 and 65535';
            isValid = false;
        }

        setErrors(newErrors);
        return isValid;
    };

    // ---- Submit ----
    const handleSubmit = async (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();
        if (!validateForm()) return;

        const device: Device = {
            id: editingDevice?.id || 0,
            name: txtName,
            isActive: selectedValue, // convert to boolean
            ip: txtIP,
            port: Number(txtPort),
            consumerNumber: txtConsumerNo,
            serialNumber: txtSerialNo,
            ftpFolder: txtFtpFolder,
        };

        try {
            let result: any;
            if (editingDevice) {
                result = await editDevice(device);
            } else {
                result = await addDevice(device);
            }

            if (!result.status) {
                setErrors(prev => ({
                    ...prev,
                    general: result.errors,
                }));
                return;
            }

            // Reset form
            setTxtName('');
            setTxtIP('');
            setTxtPort('');
            setTxtConsumerNo('');
            setTxtSerialNo('');
            setTxtFtpFolder('');
            setErrors({
                name: '',
                serialNo: '',
                consumerNo: '',
                ftpFolder: '',
                ip: '',
                port: '',
                general: '',
            });
            setEditingDevice(null);
            onToggleVisibility(null);
        } catch (error) {
            setErrors((prev) => ({ ...prev, general: 'Failed to save device' }));
        }
    };

    // ---- Cancel ----
    const cancelDeviceClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();
        setTxtName('');
        setTxtIP('');
        setTxtPort('');
        setTxtConsumerNo('');
        setTxtSerialNo('');
        setTxtFtpFolder('');
        setErrors({
            name: '',
            serialNo: '',
            consumerNo: '',
            ftpFolder: '',
            ip: '',
            port: '',
            general: '',
        });
        setEditingDevice(null);
        onToggleVisibility(null);
    };

    return (
        <form
            onSubmit={(event) => {
                event.preventDefault();
            }}
        >
            <Card>
                <CardHeader title={editingDevice ? 'Edit Device' : 'Add Device'} />
                <Divider />
                <CardContent>
                    <Stack spacing={3} sx={{ maxWidth: 'sm' }}>
                        <FormControl fullWidth error={!!errors.name}>
                            <InputLabel>Device</InputLabel>
                            <OutlinedInput
                                label="Device"
                                name="device"
                                type="text"
                                value={txtName}
                                onChange={handleNameChange}
                            />
                            {errors.name && <FormHelperText>{errors.name}</FormHelperText>}
                        </FormControl>

                        <FormControl fullWidth error={!!errors.serialNo}>
                            <InputLabel>Serial No</InputLabel>
                            <OutlinedInput
                                label="Serial No"
                                name="serialNo"
                                type="text"
                                value={txtSerialNo}
                                onChange={handleSerChange}
                            />
                            {errors.serialNo && <FormHelperText>{errors.serialNo}</FormHelperText>}
                        </FormControl>

                        <FormControl fullWidth error={!!errors.consumerNo}>
                            <InputLabel>Consumer No</InputLabel>
                            <OutlinedInput
                                label="Consumer No"
                                name="consumerNo"
                                type="text"
                                value={txtConsumerNo}
                                onChange={handleConChange}
                            />
                            {errors.consumerNo && <FormHelperText>{errors.consumerNo}</FormHelperText>}
                        </FormControl>

                        <FormControl fullWidth error={!!errors.ftpFolder}>
                            <InputLabel>FTP Folder</InputLabel>
                            <OutlinedInput
                                label="FTP Folder"
                                name="ftpFolder"
                                type="text"
                                value={txtFtpFolder}
                                onChange={handleFtpChange}
                            />
                            {errors.ftpFolder && <FormHelperText>{errors.ftpFolder}</FormHelperText>}
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
                                <MenuItem value="1">Active</MenuItem>
                                <MenuItem value="0">Inactive</MenuItem>
                            </Select>
                        </FormControl>

                        <FormControl fullWidth error={!!errors.ip}>
                            <InputLabel>IP</InputLabel>
                            <OutlinedInput
                                label="IP"
                                name="ip"
                                type="text"
                                value={txtIP}
                                onChange={handleIPChange}
                            />
                            {errors.ip && <FormHelperText>{errors.ip}</FormHelperText>}
                        </FormControl>

                        <FormControl fullWidth error={!!errors.port}>
                            <InputLabel>Port</InputLabel>
                            <OutlinedInput
                                label="Port"
                                name="port"
                                type="text"
                                value={txtPort}
                                onChange={handlePortChange}
                            />
                            {errors.port && <FormHelperText>{errors.port}</FormHelperText>}
                        </FormControl>

                        {errors.general && <FormHelperText error>
                            {/* {errors.general}*/}
                            {(Array.isArray(errors.general) ? errors.general : [errors.general]).map((err, index) => (
                                <span key={index} style={{ display: 'block' }}>{err}</span>
                            ))}
                        </FormHelperText>}
                    </Stack>
                </CardContent>
                <Divider />
                <CardActions sx={{ justifyContent: 'flex-end' }}>
                    <Button variant="contained" onClick={handleSubmit}>
                        {editingDevice ? 'Update' : 'Add'}
                    </Button>
                    <Button variant="outlined" onClick={cancelDeviceClick}>
                        Cancel
                    </Button>
                </CardActions>
            </Card>
        </form>
    );
}
