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
    const [txtClientAddress, setTxtClientAddress] = React.useState('16');
    const [txtServerAddress, setTxtServerAddress] = React.useState('1');
    const [txtAuthentication, setTxtAuthentication] = React.useState('None');
    const [txtPassword, setTxtPassword] = React.useState('');
    const [txtTimeout, setTxtTimeout] = React.useState('30000');
    const [txtTypeName, setTxtTypeName] = React.useState<'ABT' | 'PQ'>('ABT');

    const [errors, setErrors] = React.useState({
        name: '',
        serialNo: '',
        consumerNo: '',
        ftpFolder: '',
        ip: '',
        port: '',
        clientAddress: '',
        serverAddress: '',
        timeout: '',
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
            setTxtClientAddress(String(editingDevice.clientAddress ?? 16));
            setTxtServerAddress(String(editingDevice.serverAddress ?? 1));
            setTxtAuthentication(editingDevice.authentication || 'None');
            setTxtPassword(editingDevice.password || '');
            setTxtTimeout(String(editingDevice.timeout ?? 30000));
            setTxtTypeName((editingDevice.typeName as 'ABT' | 'PQ') || 'ABT');
        } else {
            setTxtName('');
            setTxtIP('');
            setTxtPort('');
            setTxtConsumerNo('');
            setTxtSerialNo('');
            setTxtFtpFolder('');
            setSelectedValue('1');
            setTxtClientAddress('16');
            setTxtServerAddress('1');
            setTxtAuthentication('None');
            setTxtPassword('');
            setTxtTimeout('30000');
            setTxtTypeName('ABT');
            setErrors({
                name: '',
                serialNo: '',
                consumerNo: '',
                ftpFolder: '',
                ip: '',
                port: '',
                clientAddress: '',
                serverAddress: '',
                timeout: '',
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

    const handleClientAddressChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtClientAddress(event.target.value);
        setErrors((prev) => ({ ...prev, clientAddress: '', general: '' }));
    };

    const handleServerAddressChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtServerAddress(event.target.value);
        setErrors((prev) => ({ ...prev, serverAddress: '', general: '' }));
    };

    const handleAuthenticationChange = (event: SelectChangeEvent<string>) => {
        setTxtAuthentication(event.target.value);
        setErrors((prev) => ({ ...prev, general: '' }));
    };

    const handlePasswordChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtPassword(event.target.value);
        setErrors((prev) => ({ ...prev, general: '' }));
    };

    const handleTimeoutChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setTxtTimeout(event.target.value);
        setErrors((prev) => ({ ...prev, timeout: '', general: '' }));
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
            clientAddress: '',
            serverAddress: '',
            timeout: '',
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

        const clientAddrNum = parseInt(txtClientAddress, 10);
        if (!txtClientAddress.trim()) {
            newErrors.clientAddress = 'Client address is required';
            isValid = false;
        } else if (isNaN(clientAddrNum) || clientAddrNum < 0) {
            newErrors.clientAddress = 'Must be a non-negative number';
            isValid = false;
        }

        const serverAddrNum = parseInt(txtServerAddress, 10);
        if (!txtServerAddress.trim()) {
            newErrors.serverAddress = 'Server address is required';
            isValid = false;
        } else if (isNaN(serverAddrNum) || serverAddrNum < 0) {
            newErrors.serverAddress = 'Must be a non-negative number';
            isValid = false;
        }

        const timeoutNum = parseInt(txtTimeout, 10);
        if (!txtTimeout.trim()) {
            newErrors.timeout = 'Timeout is required';
            isValid = false;
        } else if (isNaN(timeoutNum) || timeoutNum < 100) {
            newErrors.timeout = 'Timeout must be at least 100ms';
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
            clientAddress: Number(txtClientAddress),
            serverAddress: Number(txtServerAddress),
            authentication: txtAuthentication,
            password: txtPassword,
            timeout: Number(txtTimeout),
            typeName: txtTypeName,
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
            setTxtClientAddress('16');
            setTxtServerAddress('1');
            setTxtAuthentication('None');
            setTxtPassword('');
            setTxtTimeout('30000');
            setTxtTypeName('ABT');
            setErrors({
                name: '',
                serialNo: '',
                consumerNo: '',
                ftpFolder: '',
                ip: '',
                port: '',
                clientAddress: '',
                serverAddress: '',
                timeout: '',
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
        setTxtClientAddress('16');
        setTxtServerAddress('1');
        setTxtAuthentication('None');
        setTxtPassword('');
        setTxtTimeout('30000');
        setErrors({
            name: '',
            serialNo: '',
            consumerNo: '',
            ftpFolder: '',
            ip: '',
            port: '',
            clientAddress: '',
            serverAddress: '',
            timeout: '',
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

                        <FormControl fullWidth>
                            <InputLabel id="metertype-label">Meter Type</InputLabel>
                            <Select
                                labelId="metertype-label"
                                id="metertype"
                                name="metertype"
                                value={txtTypeName}
                                label="Meter Type"
                                onChange={(e) => setTxtTypeName(e.target.value as 'ABT' | 'PQ')}
                            >
                                <MenuItem value="ABT">ABT</MenuItem>
                                <MenuItem value="PQ">PQ</MenuItem>
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

                        {/* ---- DLMS Connection Configuration ---- */}
                        <Stack direction="row" spacing={2}>
                            <FormControl fullWidth error={!!errors.clientAddress}>
                                <InputLabel>Client Address</InputLabel>
                                <OutlinedInput
                                    label="Client Address"
                                    name="clientAddress"
                                    type="number"
                                    value={txtClientAddress}
                                    onChange={handleClientAddressChange}
                                />
                                {errors.clientAddress && <FormHelperText>{errors.clientAddress}</FormHelperText>}
                            </FormControl>
                            <FormControl fullWidth error={!!errors.serverAddress}>
                                <InputLabel>Server Address</InputLabel>
                                <OutlinedInput
                                    label="Server Address"
                                    name="serverAddress"
                                    type="number"
                                    value={txtServerAddress}
                                    onChange={handleServerAddressChange}
                                />
                                {errors.serverAddress && <FormHelperText>{errors.serverAddress}</FormHelperText>}
                            </FormControl>
                        </Stack>

                        <FormControl fullWidth>
                            <InputLabel id="auth-label">Authentication</InputLabel>
                            <Select
                                labelId="auth-label"
                                id="authentication"
                                name="authentication"
                                value={txtAuthentication}
                                label="Authentication"
                                onChange={handleAuthenticationChange}
                            >
                                <MenuItem value="None">None</MenuItem>
                                <MenuItem value="Low">Low (Password)</MenuItem>
                                <MenuItem value="High">High (HLS)</MenuItem>
                                <MenuItem value="HighGmac">High GMAC</MenuItem>
                                <MenuItem value="HighSha256">High SHA-256</MenuItem>
                                <MenuItem value="HighEcdsa">High ECDSA</MenuItem>
                            </Select>
                        </FormControl>

                        {txtAuthentication !== 'None' && (
                            <FormControl fullWidth>
                                <InputLabel>Password</InputLabel>
                                <OutlinedInput
                                    label="Password"
                                    name="password"
                                    type="password"
                                    value={txtPassword}
                                    onChange={handlePasswordChange}
                                />
                            </FormControl>
                        )}

                        <FormControl fullWidth error={!!errors.timeout}>
                            <InputLabel>Timeout (ms)</InputLabel>
                            <OutlinedInput
                                label="Timeout (ms)"
                                name="timeout"
                                type="number"
                                value={txtTimeout}
                                onChange={handleTimeoutChange}
                            />
                            {errors.timeout && <FormHelperText>{errors.timeout}</FormHelperText>}
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
