"use client";

import React, { useState, useEffect } from 'react';
import {
    Box,
    Card,
    CardContent,
    CardHeader,
    CardActions,
    Divider,
    Button,
    Stack,
    Typography,
    FormControl,
    Autocomplete,
    TextField,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    FormGroup,
    FormControlLabel,
    Checkbox,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Paper,
    CircularProgress,
    Alert,
    Snackbar,
    Grid,
    InputLabel,
    Select,
    MenuItem
} from '@mui/material';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { DemoContainer } from '@mui/x-date-pickers/internals/demo';
import dayjs, { Dayjs } from 'dayjs';
import * as XLSX from 'xlsx';
import { DownloadIcon } from '@phosphor-icons/react/dist/ssr/Download';
import { EyeIcon } from '@phosphor-icons/react/dist/ssr/Eye';
import { FloppyDiskIcon } from '@phosphor-icons/react/dist/ssr/FloppyDisk';

import {
    fetchDevices,
    fetchDeviceParameter,
    fetchReport
} from '../../../api/device';
import type { Device } from '@/components/dashboard/device/devices-table';

interface ParameterOption {
    id: number;
    name: string;
    unit?: string;
    obisCode?: string;
}

export default function ReportsPage(): React.JSX.Element {
    const [devices, setDevices] = useState<Device[]>([]);
    const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
    const [startDate, setStartDate] = useState<Dayjs | null>(dayjs().subtract(7, 'day'));
    const [endDate, setEndDate] = useState<Dayjs | null>(dayjs());

    // Parameters lists and selected states
    const [parameters, setParameters] = useState<ParameterOption[]>([]);
    const [selectedParamIds, setSelectedParamIds] = useState<number[]>([]);
    
    // UI elements
    const [isParamDialogOpen, setIsParamDialogOpen] = useState(false);
    const [loading, setLoading] = useState<'devices' | 'parameters' | 'generate' | null>('devices');
    const [reportData, setReportData] = useState<{ columns: string[]; rows: any[] } | null>(null);
    
    // Templates
    const [templates, setTemplates] = useState<{ [name: string]: number[] }>({});
    const [templateName, setTemplateName] = useState('');
    const [selectedTemplateName, setSelectedTemplateName] = useState('');

    // Notification states
    const [snackbarOpen, setSnackbarOpen] = useState(false);
    const [snackbarMessage, setSnackbarMessage] = useState('');
    const [snackbarSeverity, setSnackbarSeverity] = useState<'success' | 'error' | 'warning'>('success');

    // Validation error states
    const [errors, setErrors] = useState({
        device: false,
        params: false,
        start: false,
        end: false
    });

    // Default parameters matching list
    const defaultMatches = [
        'avg frequency', 'average frequency', 'frequency',
        'rv', 'r phase voltage', 'voltage l1', 'voltage r',
        'yv', 'y phase voltage', 'voltage l2', 'voltage y',
        'bv', 'b phase voltage', 'voltage l3', 'voltage b',
        'fwd kwh', 'forward active energy', 'active energy import',
        'net kwh', 'net active energy',
        'rev kwh', 'reverse active energy', 'active energy export',
        'kvarh.q1', 'reactive energy - quadrant 1',
        'kvarh.q2', 'reactive energy - quadrant 2',
        'kvarh.q3', 'reactive energy - quadrant 3'
    ];

    // Load initial devices and templates
    useEffect(() => {
        const init = async () => {
            setLoading('devices');
            try {
                const fetchedDevices = await fetchDevices();
                setDevices(fetchedDevices ?? []);
                
                // Load templates from local storage
                const savedTemplates = localStorage.getItem('pqm_report_templates');
                if (savedTemplates) {
                    setTemplates(JSON.parse(savedTemplates));
                }
            } catch (error) {
                console.error('Failed to initialize page data:', error);
                showSnackbar('Failed to load devices list', 'error');
            } finally {
                setLoading(null);
            }
        };
        init();
    }, []);

    // Load device parameters when selected device changes
    const handleDeviceChange = async (event: React.SyntheticEvent, newValue: Device | null) => {
        setSelectedDevice(newValue);
        setSelectedParamIds([]);
        setParameters([]);
        setReportData(null);
        setSelectedTemplateName('');

        if (newValue) {
            setLoading('parameters');
            try {
                const fetchedParams = await fetchDeviceParameter(newValue.id);
                if (fetchedParams && fetchedParams.data) {
                    // Extract unique parameters
                    const seenNames = new Set<string>();
                    const uniqueOptions: ParameterOption[] = [];

                    fetchedParams.data.forEach((p: any) => {
                        if (!seenNames.has(p.name)) {
                            seenNames.add(p.name);
                            uniqueOptions.push({
                                id: p.id,
                                name: p.name,
                                unit: p.unit,
                                obisCode: p.obisCode
                            });
                        }
                    });

                    setParameters(uniqueOptions);

                    // Pre-select defaults (max 10)
                    const autoSelected: number[] = [];
                    uniqueOptions.forEach(opt => {
                        const optLower = opt.name.toLowerCase();
                        const isMatch = defaultMatches.some(match => optLower.includes(match));
                        if (isMatch && autoSelected.length < 10) {
                            autoSelected.push(opt.id);
                        }
                    });

                    // If we didn't find matched defaults, just check the first 10
                    if (autoSelected.length === 0) {
                        uniqueOptions.slice(0, 10).forEach(opt => autoSelected.push(opt.id));
                    }

                    setSelectedParamIds(autoSelected);
                }
            } catch (error) {
                console.error('Failed to fetch device parameters:', error);
                showSnackbar('Failed to fetch parameter options for this device', 'error');
            } finally {
                setLoading(null);
            }
        }
    };

    const handleCheckboxChange = (paramId: number) => {
        setSelectedParamIds((prevSelected) => {
            if (prevSelected.includes(paramId)) {
                return prevSelected.filter(id => id !== paramId);
            } else {
                if (prevSelected.length >= 10) {
                    showSnackbar('Maximum number of parameters allowed is 10', 'warning');
                    return prevSelected;
                }
                return [...prevSelected, paramId];
            }
        });
    };

    const handleSaveTemplate = () => {
        if (!templateName.trim()) {
            showSnackbar('Please enter a template name', 'error');
            return;
        }
        if (selectedParamIds.length === 0) {
            showSnackbar('Select at least one parameter to save a template', 'error');
            return;
        }

        const updatedTemplates = {
            ...templates,
            [templateName.trim()]: selectedParamIds
        };

        setTemplates(updatedTemplates);
        localStorage.setItem('pqm_report_templates', JSON.stringify(updatedTemplates));
        setSelectedTemplateName(templateName.trim());
        setTemplateName('');
        showSnackbar(`Template "${templateName.trim()}" saved successfully`, 'success');
    };

    const handleLoadTemplate = (name: string) => {
        setSelectedTemplateName(name);
        if (templates[name]) {
            // Filter out saved parameter IDs that might not exist for the current device
            const validIds = templates[name].filter(id => parameters.some(p => p.id === id));
            setSelectedParamIds(validIds);
            showSnackbar(`Loaded template "${name}"`, 'success');
        }
    };

    const handleGenerateReport = async () => {
        const newErrors = {
            device: !selectedDevice,
            params: selectedParamIds.length === 0,
            start: !startDate,
            end: !endDate
        };
        setErrors(newErrors);

        if (Object.values(newErrors).some(Boolean)) {
            if (newErrors.device) showSnackbar('Please select a device', 'error');
            else if (newErrors.params) showSnackbar('Please select at least one parameter', 'error');
            return;
        }

        setLoading('generate');
        try {
            const startStr = startDate!.format('MM/DD/YYYY');
            const endStr = endDate!.format('MM/DD/YYYY');
            
            const response = await fetchReport(selectedDevice!.id, selectedParamIds, startStr, endStr);
            if (response && response.status && response.data) {
                setReportData({
                    columns: response.data.columns ?? [],
                    rows: response.data.rows ?? []
                });
                showSnackbar('Report generated successfully', 'success');
            } else {
                setReportData(null);
                showSnackbar('No data found for the selected configuration', 'warning');
            }
        } catch (error) {
            console.error('Failed to generate report:', error);
            showSnackbar('Error generating report from server', 'error');
            setReportData(null);
        } finally {
            setLoading(null);
        }
    };

    const handleExport = () => {
        if (!reportData || reportData.rows.length === 0) {
            showSnackbar('No report data available to export', 'error');
            return;
        }

        const exportRows = reportData.rows.map(row => {
            const item: any = {
                'Timestamp': dayjs(row.timestamp).format('YYYY-MM-DD HH:mm:ss')
            };
            reportData.columns.forEach(col => {
                item[col] = row.values[col] ?? '-';
            });
            return item;
        });

        const worksheet = XLSX.utils.json_to_sheet(exportRows);
        const workbook = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(workbook, worksheet, 'Report');
        
        const fileName = `${selectedDevice?.name.replace(/\s+/g, '_')}_Report_${dayjs().format('YYYYMMDD_HHmmss')}.xlsx`;
        XLSX.writeFile(workbook, fileName);
        showSnackbar('Report exported to Excel successfully', 'success');
    };

    const showSnackbar = (message: string, severity: 'success' | 'error' | 'warning') => {
        setSnackbarMessage(message);
        setSnackbarSeverity(severity);
        setSnackbarOpen(true);
    };

    return (
        <div>
            {loading && (
                <Box
                    sx={{
                        display: 'flex',
                        justifyContent: 'center',
                        alignItems: 'center',
                        position: 'absolute',
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                        backgroundColor: 'rgba(255, 255, 255, 0.7)',
                        zIndex: 9999,
                    }}
                >
                    <CircularProgress />
                </Box>
            )}

            <Stack spacing={3}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="h4">Reports & Analysis</Typography>
                </Box>

                <Grid container spacing={3}>
                    {/* Setup Filter Card */}
                    <Grid size={{ xs: 12, md: 7 }}>
                        <Card sx={{ borderRadius: '8px' }}>
                            <CardHeader title="Report Configuration" subheader="Configure device, date range, and parameters" />
                            <Divider />
                            <CardContent sx={{ p: 2 }}>
                                <Stack spacing={2}>
                                    <FormControl fullWidth size="small">
                                        <Autocomplete
                                            id="device-report-autocomplete"
                                            options={devices}
                                            size="small"
                                            getOptionLabel={(device) => device.name}
                                            value={selectedDevice}
                                            onChange={handleDeviceChange}
                                            isOptionEqualToValue={(option, value) => option.id === value.id}
                                            renderInput={(params) => (
                                                <TextField
                                                    {...params}
                                                    label="Select Device / Meter"
                                                    variant="outlined"
                                                    size="small"
                                                    error={errors.device}
                                                    helperText={errors.device ? "Device is required" : ""}
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
                                                    value={startDate}
                                                    onChange={(newValue) => setStartDate(newValue)}
                                                    slotProps={{
                                                        textField: {
                                                            size: 'small',
                                                            error: errors.start,
                                                            helperText: errors.start ? "Start date is required" : "",
                                                        },
                                                    }}
                                                />
                                                <DatePicker
                                                    label="End Date"
                                                    value={endDate}
                                                    onChange={(newValue) => setEndDate(newValue)}
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

                                    <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', mt: 1 }}>
                                        <Button
                                            variant="outlined"
                                            onClick={() => {
                                                if (!selectedDevice) {
                                                    showSnackbar('Please select a device first', 'warning');
                                                    return;
                                                }
                                                setIsParamDialogOpen(true);
                                            }}
                                        >
                                            View Parameters
                                        </Button>
                                        <Typography variant="body2" color="textSecondary">
                                            {selectedParamIds.length} of 10 parameters selected
                                        </Typography>
                                    </Box>
                                </Stack>
                            </CardContent>
                            <Divider />
                            <CardActions sx={{ justifyContent: 'flex-end', py: 1.5, px: 2 }}>
                                <Button
                                    variant="contained"
                                    onClick={handleGenerateReport}
                                    startIcon={<EyeIcon size={16} />}
                                >
                                    Generate Report
                                </Button>
                            </CardActions>
                        </Card>
                    </Grid>

                    {/* Template Card */}
                    <Grid size={{ xs: 12, md: 5 }}>
                        <Card sx={{ borderRadius: '8px', height: '100%' }}>
                            <CardHeader title="Report Templates" subheader="Save or load parameter selection templates" />
                            <Divider />
                            <CardContent>
                                <Stack spacing={3}>
                                    {/* Load template */}
                                    <FormControl fullWidth size="small">
                                        <InputLabel id="load-template-label">Select Saved Template</InputLabel>
                                        <Select
                                            labelId="load-template-label"
                                            value={selectedTemplateName}
                                            onChange={(e) => handleLoadTemplate(e.target.value as string)}
                                            label="Select Saved Template"
                                        >
                                            <MenuItem value="">
                                                <em>None</em>
                                            </MenuItem>
                                            {Object.keys(templates).map((name) => (
                                                <MenuItem key={name} value={name}>
                                                    {name}
                                                </MenuItem>
                                            ))}
                                        </Select>
                                    </FormControl>

                                    <Divider>or save current selection</Divider>

                                    {/* Save template */}
                                    <Box sx={{ display: 'flex', gap: 1 }}>
                                        <TextField
                                            label="New Template Name"
                                            variant="outlined"
                                            size="small"
                                            fullWidth
                                            value={templateName}
                                            onChange={(e) => setTemplateName(e.target.value)}
                                        />
                                        <Button
                                            variant="contained"
                                            color="secondary"
                                            onClick={handleSaveTemplate}
                                            startIcon={<FloppyDiskIcon size={16} />}
                                        >
                                            Save
                                        </Button>
                                    </Box>
                                </Stack>
                            </CardContent>
                        </Card>
                    </Grid>
                </Grid>

                {/* Report Table Card */}
                {reportData && (
                    <Card sx={{ borderRadius: '8px' }}>
                        <CardHeader
                            title="Report Data"
                            subheader={`Device logs from ${startDate?.format('MMM DD, YYYY')} to ${endDate?.format('MMM DD, YYYY')}`}
                            action={
                                <Button
                                    variant="contained"
                                    color="success"
                                    onClick={handleExport}
                                    startIcon={<DownloadIcon size={16} />}
                                >
                                    Export Excel
                                </Button>
                            }
                        />
                        <Divider />
                        <CardContent sx={{ p: 0 }}>
                            <TableContainer component={Paper} sx={{ maxHeight: 600 }}>
                                <Table stickyHeader size="small" aria-label="report data table">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell sx={{ fontWeight: 'bold', backgroundColor: '#f4f6f8' }}>Timestamp</TableCell>
                                            {reportData.columns.map((col) => {
                                                const param = parameters.find(p => p.name === col);
                                                const unitStr = param?.unit ? ` (${param.unit})` : '';
                                                return (
                                                    <TableCell key={col} sx={{ fontWeight: 'bold', backgroundColor: '#f4f6f8' }}>
                                                        {col}{unitStr}
                                                    </TableCell>
                                                );
                                            })}
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {reportData.rows.length === 0 ? (
                                            <TableRow>
                                                <TableCell colSpan={reportData.columns.length + 1} align="center" sx={{ py: 3 }}>
                                                    No readings available for the selected dates and parameters.
                                                </TableCell>
                                            </TableRow>
                                        ) : (
                                            reportData.rows.map((row, idx) => (
                                                <TableRow hover key={idx}>
                                                    <TableCell>{dayjs(row.timestamp).format('YYYY-MM-DD HH:mm:ss')}</TableCell>
                                                    {reportData.columns.map((col) => (
                                                        <TableCell key={col}>
                                                            {row.values[col] ?? '-'}
                                                        </TableCell>
                                                    ))}
                                                </TableRow>
                                            ))
                                        )}
                                    </TableBody>
                                </Table>
                            </TableContainer>
                        </CardContent>
                    </Card>
                )}
            </Stack>

            {/* Parameter Selector Modal Dialog */}
            <Dialog
                open={isParamDialogOpen}
                onClose={() => setIsParamDialogOpen(false)}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    View & Select Parameters
                    <Typography variant="subtitle2" color="textSecondary">
                        Choose up to 10 parameters to display in the report. Current selection: {selectedParamIds.length}/10
                    </Typography>
                </DialogTitle>
                <Divider />
                <DialogContent sx={{ maxHeight: '400px', overflowY: 'auto' }}>
                    {selectedParamIds.length >= 10 && (
                        <Alert severity="warning" sx={{ mb: 2, py: 0.5 }}>
                            Maximum parameter selection limit (10) reached. Uncheck a parameter to select another.
                        </Alert>
                    )}
                    <FormGroup>
                        <Grid container spacing={1}>
                            {parameters.map((param) => {
                                const isChecked = selectedParamIds.includes(param.id);
                                const isDisabled = !isChecked && selectedParamIds.length >= 10;
                                return (
                                    <Grid size={{ xs: 12, sm: 6 }} key={param.id}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={isChecked}
                                                    onChange={() => handleCheckboxChange(param.id)}
                                                    disabled={isDisabled}
                                                    size="small"
                                                />
                                            }
                                            label={
                                                <Box>
                                                    <Typography variant="body2">{param.name}</Typography>
                                                    {param.unit && (
                                                        <Typography variant="caption" color="textSecondary">
                                                            Unit: {param.unit}
                                                        </Typography>
                                                    )}
                                                </Box>
                                            }
                                        />
                                    </Grid>
                                );
                            })}
                        </Grid>
                    </FormGroup>
                </DialogContent>
                <Divider />
                <DialogActions>
                    <Button onClick={() => setIsParamDialogOpen(false)} variant="contained" size="small">
                        Close
                    </Button>
                </DialogActions>
            </Dialog>

            <Snackbar
                open={snackbarOpen}
                autoHideDuration={4000}
                onClose={() => setSnackbarOpen(false)}
                anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
            >
                <Alert
                    severity={snackbarSeverity}
                    sx={{ width: '100%' }}
                    onClose={() => setSnackbarOpen(false)}
                    variant="filled"
                >
                    {snackbarMessage}
                </Alert>
            </Snackbar>
        </div>
    );
}
