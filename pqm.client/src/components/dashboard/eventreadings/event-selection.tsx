"use client";
import React, { useState, useEffect } from "react";
import {
    FormControl,
    TextField,
    Card,
    CardActions,
    CardContent,
    Divider,
    Button,
    Stack,
} from "@mui/material";
import { Autocomplete } from "@mui/material";

import dayjs, { Dayjs } from "dayjs";
import { DemoContainer } from "@mui/x-date-pickers/internals/demo";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import type { Device } from "@/components/dashboard/device/devices-table";
import { fetchConnectedHeaders, fetchDLMSObjects } from "../../../api/device";

interface DeviceFiltersProps {
    rows: Device[];
    onSearch?: (searchParams: {
        deviceId: string | number | null;
        startTime: Dayjs | null;
        endTime: Dayjs | null;
        eventType: string | number | null;
    }) => void;
}

export function EventFilters({
    rows = [],
    onSearch = () => { },
}: DeviceFiltersProps): React.JSX.Element {
    const [selectedDevice, setSelectedDevice] = useState<Device | null>(null);
    const [selectedEvent, setSelectedEvent] = useState<any | null>(null);
    const [endValue, setEndValue] = useState<Dayjs | null>(dayjs());
    const [startValue, setStartValue] = useState<Dayjs | null>(
        dayjs().subtract(7, "day")
    );

    // Validation states
    const [errors, setErrors] = useState({
        device: false,
        event: false,
        start: false,
        end: false,
    });

    const [eventTypes, setEventTypes] = useState<any[]>([]);

    useEffect(() => {
        if (!selectedDevice) {
            setEventTypes([]);
            setSelectedEvent(null);
            return;
        }

        const loadDeviceEventParams = async () => {
            try {
                const headerRes = await fetchConnectedHeaders(selectedDevice.id);
                if (headerRes && headerRes.status && headerRes.data.length > 0) {
                    const allEventParams: any[] = [];
                    for (const header of headerRes.data) {
                        const objectsRes = await fetchDLMSObjects(header.id);
                        if (objectsRes && objectsRes.status && Array.isArray(objectsRes.data)) {
                            const eventParams = objectsRes.data.filter((obj: any) => 
                                obj.name && obj.name.toLowerCase().includes("event")
                            );
                            allEventParams.push(...eventParams);
                        }
                    }

                    // Map status parameters to dropdown options
                    const statusOptions = allEventParams.map((obj: any) => ({
                        key: `status_${obj.obisCode}`,
                        value: obj.name,
                        isStatusParam: true,
                        obisCode: obj.obisCode,
                        objectType: obj.objectType,
                        dlmsObject: obj,
                    }));

                    setEventTypes(statusOptions);
                } else {
                    setEventTypes([]);
                }
            } catch (error) {
                console.error("Failed to load device event parameters:", error);
                setEventTypes([]);
            }
        };

        loadDeviceEventParams();
        setSelectedEvent(null);
    }, [selectedDevice]);

    const handleSearch = () => {
        const newErrors = {
            device: !selectedDevice,
            event: !selectedEvent,
            start: !startValue,
            end: !endValue,
        };
        setErrors(newErrors);

        if (Object.values(newErrors).some(Boolean)) return;

        onSearch({
            deviceId: selectedDevice?.id ?? null,
            startTime: startValue,
            endTime: endValue,
            eventType: selectedEvent?.key ?? null,
        });
    };

    return (
        <Card sx={{ maxWidth: '600px', width: '100%', borderRadius: '8px' }}>
            <Divider />
            <CardContent sx={{ p: 2 }}>
                <Stack spacing={2} sx={{ maxWidth: "100%" }}>
                    {/* Device */}
                    <FormControl fullWidth size="small">
                        <Autocomplete
                            id="device-filter-autocomplete"
                            options={rows}
                            size="small"
                            getOptionLabel={(device) => device.name}
                            value={selectedDevice}
                            onChange={(e, v) => setSelectedDevice(v)}
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

                    {/* Event */}
                    <FormControl fullWidth size="small">
                        <Autocomplete
                            id="event-filter-autocomplete"
                            options={eventTypes}
                            size="small"
                            getOptionLabel={(event) => event.value}
                            value={selectedEvent}
                            onChange={(e, v) => setSelectedEvent(v)}
                            isOptionEqualToValue={(option, value) => option.key === value.key}
                            renderInput={(params) => (
                                <TextField
                                    {...params}
                                    label="Select or type to search event"
                                    variant="outlined"
                                    size="small"
                                    error={errors.event}
                                    helperText={errors.event ? "Event is required" : ""}
                                />
                            )}
                            openOnFocus
                        />
                    </FormControl>

                    {/* Dates */}
                    <FormControl fullWidth>
                        <LocalizationProvider dateAdapter={AdapterDayjs}>
                            <DemoContainer components={["DatePicker", "DatePicker"]}>
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
            <CardActions sx={{ justifyContent: "flex-end", py: 1, px: 2 }}>
                <Button variant="contained" size="small" onClick={handleSearch}>
                    Search
                </Button>
            </CardActions>
        </Card>
    );
}
