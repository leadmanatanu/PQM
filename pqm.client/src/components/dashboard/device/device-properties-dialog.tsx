'use client';

import * as React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Tabs,
  Tab,
  Box,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Checkbox,
  FormControlLabel,
  Grid,
  Typography,
  IconButton,
  FormHelperText
} from '@mui/material';
import { X as CloseIcon } from '@phosphor-icons/react/dist/ssr/X';
import { addDevice, connectDevice } from '../../../api/device';
import type { Device } from './devices-table';

interface DevicePropertiesDialogProps {
  open: boolean;
  onClose: () => void;
  device: Device | null;
  onSave: (updatedDevice: Device) => Promise<void>;
}

export function DevicePropertiesDialog({
  open,
  onClose,
  device,
  onSave
}: DevicePropertiesDialogProps): React.JSX.Element {
  const [tabValue, setTabValue] = React.useState(0);
  const [isSaving, setIsSaving] = React.useState(false);

  // Basic device fields state
  const [name, setName] = React.useState('');
  const [serialNumber, setSerialNumber] = React.useState('');
  const [consumerNumber, setConsumerNumber] = React.useState('');
  const [ip, setIp] = React.useState('');
  const [port, setPort] = React.useState('4059');
  const [deviceType, setDeviceType] = React.useState('LnT');

  // Connection settings states
  const [manufacturer, setManufacturer] = React.useState('IndianStandard');
  const [connectionInterface, setConnectionInterface] = React.useState('HDLC');
  const [authentication, setAuthentication] = React.useState('None');
  const [password, setPassword] = React.useState('');
  const [waitTime, setWaitTime] = React.useState('00:00:05');
  const [addressType, setAddressType] = React.useState('Default');
  const [logicalServer, setLogicalServer] = React.useState(0);
  const [media, setMedia] = React.useState('Net');
  const [logicalNameReferencing, setLogicalNameReferencing] = React.useState(true);
  const [clientAddress, setClientAddress] = React.useState(16);
  const [ascii, setAscii] = React.useState(true);
  const [resendCount, setResendCount] = React.useState(3);
  const [broadcast, setBroadcast] = React.useState(false);
  const [physicalServer, setPhysicalServer] = React.useState(1);
  const [verboseMode, setVerboseMode] = React.useState(false);
  const [protocol, setProtocol] = React.useState('Tcp');
  const [useSerialPort, setUseSerialPort] = React.useState(false);

  // Validation errors state
  const [errors, setErrors] = React.useState({
    name: '',
    serialNumber: '',
    consumerNumber: '',
    ip: '',
    port: '',
    general: ''
  });

  React.useEffect(() => {
    if (device) {
      // Prefill with device info
      setName(device.name || '');
      setSerialNumber(device.serialNumber || '');
      setConsumerNumber(device.consumerNumber || '');
      setIp(device.ip || '');
      setPort(String(device.port ?? '4059'));
      setDeviceType(device.deviceType || 'LnT');

      // Connection settings parsing
      if (device.connectionSettings) {
        try {
          const settings = JSON.parse(device.connectionSettings);
          setManufacturer(settings.manufacturer ?? 'IndianStandard');
          setConnectionInterface(settings.interface ?? 'HDLC');
          setAuthentication(settings.authentication ?? 'None');
          setPassword(settings.password ?? '');
          setWaitTime(settings.waitTime ?? '00:00:05');
          setAddressType(settings.addressType ?? 'Default');
          setLogicalServer(settings.logicalServer ?? 0);
          setMedia(settings.media ?? 'Net');
          setLogicalNameReferencing(settings.logicalNameReferencing ?? true);
          setClientAddress(settings.clientAddress ?? 16);
          setAscii(settings.ascii ?? true);
          setResendCount(settings.resendCount ?? 3);
          setBroadcast(settings.broadcast ?? false);
          setPhysicalServer(settings.physicalServer ?? 1);
          setVerboseMode(settings.verboseMode ?? false);
          setProtocol(settings.protocol ?? 'Tcp');
          setUseSerialPort(settings.useSerialPort ?? false);
        } catch (e) {
          console.error('Error parsing device connection settings:', e);
        }
      } else {
        resetConnectionDefaults();
      }
    } else {
      // Add mode defaults
      setName('');
      setSerialNumber('');
      setConsumerNumber('');
      setIp('');
      setPort('4059');
      setDeviceType('LnT');
      resetConnectionDefaults();
    }
    
    // Clear errors and tab
    setErrors({
      name: '',
      serialNumber: '',
      consumerNumber: '',
      ip: '',
      port: '',
      general: ''
    });
    setTabValue(0);
  }, [device, open]);

  const resetConnectionDefaults = () => {
    setManufacturer('IndianStandard');
    setConnectionInterface('HDLC');
    setAuthentication('None');
    setPassword('');
    setWaitTime('00:00:05');
    setAddressType('Default');
    setLogicalServer(0);
    setMedia('Net');
    setLogicalNameReferencing(true);
    setClientAddress(16);
    setAscii(true);
    setResendCount(3);
    setBroadcast(false);
    setPhysicalServer(1);
    setVerboseMode(false);
    setProtocol('Tcp');
    setUseSerialPort(false);
  };

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

  const validateForm = (): boolean => {
    const newErrors = {
      name: '',
      serialNumber: '',
      consumerNumber: '',
      ip: '',
      port: '',
      general: ''
    };
    let isValid = true;

    if (!name.trim()) {
      newErrors.name = 'Name is required';
      isValid = false;
    }
    if (!serialNumber.trim()) {
      newErrors.serialNumber = 'Serial number is required';
      isValid = false;
    }
    if (!consumerNumber.trim()) {
      newErrors.consumerNumber = 'Account number is required';
      isValid = false;
    }

    const ipRegex =
      /^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;
    if (!ip.trim()) {
      newErrors.ip = 'Host name (IP) is required';
      isValid = false;
    } else if (!ipRegex.test(ip)) {
      newErrors.ip = 'Invalid IP address format';
      isValid = false;
    }

    const portNum = parseInt(port, 10);
    if (!port.trim()) {
      newErrors.port = 'Port is required';
      isValid = false;
    } else if (isNaN(portNum) || portNum < 1 || portNum > 65535) {
      newErrors.port = 'Port must be a number (1-65535)';
      isValid = false;
    }

    setErrors(newErrors);
    return isValid;
  };

  const handleSave = async () => {
    if (!validateForm()) return;
    setIsSaving(true);

    const settingsObj = {
      manufacturer,
      interface: connectionInterface,
      authentication,
      password,
      waitTime,
      addressType,
      logicalServer,
      media,
      logicalNameReferencing,
      clientAddress,
      ascii,
      resendCount,
      broadcast,
      physicalServer,
      verboseMode,
      protocol,
      useSerialPort
    };

    const devicePayload: Device = {
      id: device?.id || 0,
      name,
      ip,
      port: Number(port),
      serialNumber,
      consumerNumber,
      isActive: device?.isActive || '1',
      connectionSettings: JSON.stringify(settingsObj),
      deviceType
    };

    try {
      if (device) {
        // Edit mode
        await onSave(devicePayload);
      } else {
        // Add mode
        const result = await addDevice(devicePayload);
        if (result && !result.status) {
          setErrors((prev) => ({
            ...prev,
            general: Array.isArray(result.errors) ? result.errors.join(', ') : result.errors || 'Failed to add device'
          }));
          setIsSaving(false);
          return;
        }
        await onSave(devicePayload); // Trigger parent refresh
      }
      onClose();
    } catch (e) {
      console.error('Failed to save device connection properties:', e);
      setErrors((prev) => ({ ...prev, general: 'Network connection or database saving error.' }));
    } finally {
      setIsSaving(false);
    }
  };

  const handleConnect = async () => {
    if (!validateForm()) return;
    setIsSaving(true);
    setErrors((prev) => ({ ...prev, general: '' }));

    const settingsObj = {
      manufacturer,
      interface: connectionInterface,
      authentication,
      password,
      waitTime,
      addressType,
      logicalServer,
      media,
      logicalNameReferencing,
      clientAddress,
      ascii,
      resendCount,
      broadcast,
      physicalServer,
      verboseMode,
      protocol,
      useSerialPort
    };

    const devicePayload: Device = {
      id: device?.id || 0,
      name,
      ip,
      port: Number(port),
      serialNumber,
      consumerNumber,
      isActive: device?.isActive || '1',
      connectionSettings: JSON.stringify(settingsObj),
      deviceType
    };

    try {
      // 1. Save properties first
      await onSave(devicePayload);

      // 2. Perform connection handshake
      const connectResult = await connectDevice(devicePayload.id);
      if (connectResult && connectResult.status) {
        // Success - trigger a parent update so the list table gets the Connected chip
        await onSave(devicePayload);
        onClose();
      } else {
        // Handshake failed
        const errMsg = Array.isArray(connectResult.errors)
          ? connectResult.errors.join(', ')
          : connectResult.errors || 'Handshake failed. Check host, port, and security settings.';
        setErrors((prev) => ({ ...prev, general: `Handshake failed: ${errMsg}` }));
      }
    } catch (e) {
      console.error('Failed connection test:', e);
      setErrors((prev) => ({ ...prev, general: 'Connection test failed due to a network error.' }));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ m: 0, p: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h6" component="div" sx={{ fontWeight: 600 }}>
          {device ? 'Device Properties' : 'Add Device'}
        </Typography>
        <IconButton onClick={onClose} size="small">
          <CloseIcon size={20} />
        </IconButton>
      </DialogTitle>
      
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs value={tabValue} onChange={handleTabChange} variant="scrollable" scrollButtons="auto">
          <Tab label="Device Settings" sx={{ textTransform: 'none', fontWeight: 600 }} />
        </Tabs>
      </Box>

      <DialogContent sx={{ minHeight: '420px', p: 3 }}>
        {tabValue === 0 && (
          <Box sx={{ mt: 1 }}>
            <Grid container spacing={2}>
              {/* Name */}
              <Grid size={{ xs: 12 }}>
                <TextField
                  label="Name"
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    setErrors((prev) => ({ ...prev, name: '' }));
                  }}
                  error={!!errors.name}
                  helperText={errors.name}
                  fullWidth
                  size="small"
                  variant="outlined"
                />
              </Grid>

              {/* Type */}
              <Grid size={{ xs: 12 }}>
                <FormControl fullWidth size="small">
                  <InputLabel>Type</InputLabel>
                  <Select
                    value={deviceType}
                    label="Type"
                    onChange={(e) => setDeviceType(e.target.value)}
                  >
                    <MenuItem value="LnT">LnT</MenuItem>
                    <MenuItem value="ABT">ABT</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              {/* Serial No & Account No */}
              <Grid size={{ xs: 6 }}>
                <TextField
                  label="Serial No"
                  value={serialNumber}
                  onChange={(e) => {
                    setSerialNumber(e.target.value);
                    setErrors((prev) => ({ ...prev, serialNumber: '' }));
                  }}
                  error={!!errors.serialNumber}
                  helperText={errors.serialNumber}
                  fullWidth
                  size="small"
                />
              </Grid>
              <Grid size={{ xs: 6 }}>
                <TextField
                  label="Account No"
                  value={consumerNumber}
                  onChange={(e) => {
                    setConsumerNumber(e.target.value);
                    setErrors((prev) => ({ ...prev, consumerNumber: '' }));
                  }}
                  error={!!errors.consumerNumber}
                  helperText={errors.consumerNumber}
                  fullWidth
                  size="small"
                />
              </Grid>

              {/* Interface & Logical Name Referencing */}
              <Grid size={{ xs: 6 }}>
                <FormControl fullWidth size="small">
                  <InputLabel>Interface</InputLabel>
                  <Select
                    value={connectionInterface}
                    label="Interface"
                    onChange={(e) => setConnectionInterface(e.target.value)}
                  >
                    <MenuItem value="HDLC">HDLC</MenuItem>
                    <MenuItem value="WRAPPER">WRAPPER</MenuItem>
                    <MenuItem value="Net">Net</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 6 }} sx={{ display: 'flex', alignItems: 'center' }}>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={logicalNameReferencing}
                      onChange={(e) => setLogicalNameReferencing(e.target.checked)}
                      color="primary"
                    />
                  }
                  label="Logical Name Referencing"
                />
              </Grid>

              {/* Authentication & Client Address */}
              <Grid size={{ xs: 6 }}>
                <FormControl fullWidth size="small">
                  <InputLabel>Authentication</InputLabel>
                  <Select
                    value={authentication}
                    label="Authentication"
                    onChange={(e) => setAuthentication(e.target.value)}
                  >
                    <MenuItem value="None">None</MenuItem>
                    <MenuItem value="Low">Low</MenuItem>
                    <MenuItem value="High">High</MenuItem>
                    <MenuItem value="MR">MR</MenuItem>
                    <MenuItem value="PUSH">PUSH</MenuItem>
                    <MenuItem value="HighMD5">High MD5</MenuItem>
                    <MenuItem value="HighSHA1">High SHA1</MenuItem>
                    <MenuItem value="HighSHA256">High SHA256</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 6 }}>
                <TextField
                  label="Client Address"
                  type="number"
                  value={clientAddress}
                  onChange={(e) => setClientAddress(Number(e.target.value))}
                  fullWidth
                  size="small"
                />
              </Grid>

              {/* Password & ASCII */}
              <Grid size={{ xs: 6 }}>
                <TextField
                  label="Password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  fullWidth
                  size="small"
                />
              </Grid>
              <Grid size={{ xs: 6 }} sx={{ display: 'flex', alignItems: 'center' }}>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={ascii}
                      onChange={(e) => setAscii(e.target.checked)}
                      color="primary"
                    />
                  }
                  label="ASCII"
                />
              </Grid>

              {/* Wait Time & Resend Count */}
              <Grid size={{ xs: 6 }}>
                <TextField
                  label="Wait Time"
                  value={waitTime}
                  onChange={(e) => setWaitTime(e.target.value)}
                  placeholder="00:00:05"
                  fullWidth
                  size="small"
                />
              </Grid>
              <Grid size={{ xs: 6 }}>
                <TextField
                  label="Resend count"
                  type="number"
                  value={resendCount}
                  onChange={(e) => setResendCount(Number(e.target.value))}
                  fullWidth
                  size="small"
                />
              </Grid>

              {/* Address Type & Broadcast */}
              <Grid size={{ xs: 6 }}>
                <FormControl fullWidth size="small">
                  <InputLabel>Address Type</InputLabel>
                  <Select
                    value={addressType}
                    label="Address Type"
                    onChange={(e) => setAddressType(e.target.value)}
                  >
                    <MenuItem value="Default">Default</MenuItem>
                    <MenuItem value="Custom">Custom</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 6 }} sx={{ display: 'flex', alignItems: 'center' }}>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={broadcast}
                      onChange={(e) => setBroadcast(e.target.checked)}
                      color="primary"
                    />
                  }
                  label="Broadcast"
                />
              </Grid>

              {/* Logical Server & Physical Server */}
              <Grid size={{ xs: 6 }}>
                <TextField
                  label="Logical Server"
                  type="number"
                  value={logicalServer}
                  onChange={(e) => setLogicalServer(Number(e.target.value))}
                  fullWidth
                  size="small"
                />
              </Grid>
              <Grid size={{ xs: 6 }}>
                <TextField
                  label="Physical Server"
                  type="number"
                  value={physicalServer}
                  onChange={(e) => setPhysicalServer(Number(e.target.value))}
                  fullWidth
                  size="small"
                />
              </Grid>

              {/* Media & Verbose Mode */}
              <Grid size={{ xs: 6 }}>
                <FormControl fullWidth size="small">
                  <InputLabel>Media</InputLabel>
                  <Select
                    value={media}
                    label="Media"
                    onChange={(e) => setMedia(e.target.value)}
                  >
                    <MenuItem value="Net">Net</MenuItem>
                    <MenuItem value="Serial">Serial</MenuItem>
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 6 }} sx={{ display: 'flex', alignItems: 'center' }}>
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={verboseMode}
                      onChange={(e) => setVerboseMode(e.target.checked)}
                      color="primary"
                    />
                  }
                  label="Verbose Mode"
                />
              </Grid>

              {/* Sub-panel Settings for Network configuration */}
              {media === 'Net' && (
                <Grid size={{ xs: 12 }}>
                  <Box sx={{ border: '1px solid #ccc', borderRadius: '4px', p: 2, mt: 1 }}>
                    <Typography variant="subtitle2" sx={{ mb: 1.5, fontWeight: 600, color: 'text.secondary' }}>
                      Network Settings
                    </Typography>
                    <Grid container spacing={2}>
                      <Grid size={{ xs: 12 }}>
                        <TextField
                          label="Host name"
                          value={ip}
                          onChange={(e) => {
                            setIp(e.target.value);
                            setErrors((prev) => ({ ...prev, ip: '' }));
                          }}
                          error={!!errors.ip}
                          helperText={errors.ip}
                          fullWidth
                          size="small"
                        />
                      </Grid>
                      <Grid size={{ xs: 6 }}>
                        <TextField
                          label="Port"
                          value={port}
                          onChange={(e) => {
                            setPort(e.target.value);
                            setErrors((prev) => ({ ...prev, port: '' }));
                          }}
                          error={!!errors.port}
                          helperText={errors.port}
                          fullWidth
                          size="small"
                        />
                      </Grid>
                      <Grid size={{ xs: 6 }}>
                        <FormControl fullWidth size="small">
                          <InputLabel>Protocol</InputLabel>
                          <Select
                            value={protocol}
                            label="Protocol"
                            onChange={(e) => setProtocol(e.target.value)}
                          >
                            <MenuItem value="Tcp">Tcp</MenuItem>
                            <MenuItem value="Udp">Udp</MenuItem>
                          </Select>
                        </FormControl>
                      </Grid>
                    </Grid>
                  </Box>
                </Grid>
              )}

              {/* General errors */}
              {errors.general && (
                <Grid size={{ xs: 12 }}>
                  <FormHelperText error sx={{ fontSize: '0.875rem' }}>
                    {errors.general}
                  </FormHelperText>
                </Grid>
              )}
            </Grid>
          </Box>
        )}

        {tabValue > 0 && (
          <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100%', minHeight: '300px' }}>
            <Typography variant="body1" color="text.secondary">
              No additional connection settings needed for this profile tab.
            </Typography>
          </Box>
        )}
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 3, display: 'flex', justifyContent: 'flex-end', alignItems: 'center' }}>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {device && device.id > 0 && (
            <Button
              onClick={handleConnect}
              disabled={isSaving}
              variant="contained"
              color="success"
              sx={{ textTransform: 'none' }}
            >
              {isSaving ? 'Connecting...' : 'Connect'}
            </Button>
          )}
          <Button onClick={handleSave} disabled={isSaving} variant="contained" color="primary">
            {isSaving ? 'Saving...' : 'OK'}
          </Button>
          <Button onClick={onClose} disabled={isSaving} variant="outlined">
            Cancel
          </Button>
        </Box>
      </DialogActions>
    </Dialog>
  );
}
