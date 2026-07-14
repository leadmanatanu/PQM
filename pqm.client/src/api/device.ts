import axios from 'axios';
import dayjs from 'dayjs';
import type { Device } from '@/components/dashboard/device/devices-table';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5135/api';

export interface ApiResponse<T = any> {
    status: string;
    statusCode: number;
    data: T;
}

// Fetch all devices
export const fetchDevices = async (): Promise<Device[]> => {
    try {
        const { data } = await axios.get<ApiResponse<Device[]>>(`${API_URL}/device`);
        return data.data; // This is the array
    } catch (error) {
        console.error("Error fetching devices:", error);
        return []; // Return empty array on error instead of null
    }
};

// Fetch FTP details
export const fetchFtpDetails = async (): Promise<any | null> => {
    try {
        const { data } = await axios.get<ApiResponse>(`${API_URL}/ftp`);
        return data.data;
    } catch (error) {
        console.error("Error fetching FTP details:", error);
        return null;
    }
};


// Update FTP details
export const updateFtpDetails = async (aJson: any): Promise<any | undefined> => {
    try {
        const { data } = await axios.put<ApiResponse>(`${API_URL}/ftp`, aJson);
        return data.data;
    } catch (error) {
        console.error('Error updating FTP details:', error);
        return undefined;
    }
};


export const testFtpDetails = async (aJson: any): Promise<any | undefined> => {
    try {
        const response = await axios.get<ApiResponse>(`${API_URL}/ftp/ftpconnectiontest`, {
            params: {
                ftphost: aJson.ftpHost,
                username: aJson.userName,
                password: aJson.password,
            },
        });
        return response.data;
    } catch (error) {
        console.error('Error testing FTP connection:', error);
        return undefined;
    }
};


// Fetch device parameters
export const fetchDeviceParameter = async (id: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/Parameter/${id}`);
        return data;
    } catch (error) {
        console.error("Error fetching device parameter:", error);
        return null;
    }
};

// Update device parameter mapping
export const updateDeviceParamMapping = async (deviceParams: any[]): Promise<any | undefined> => {
    try {
        const { data } = await axios.post<ApiResponse>(`${API_URL}/deviceparammapping`, deviceParams);
        return data;
    } catch (error) {
        console.error('Error updating device parameter mapping:', error);
        return undefined;
    }
};

// Add device
export const addDevice = async (device: Device): Promise<any | undefined> => {
    try {
        const payload = {
            Name: device.name,
            IsActive: device.isActive === '1' || device.isActive === true || String(device.isActive) === 'true',
            IP: device.ip,
            Port: device.port,
            SerialNumber: device.serialNumber,
            ConsumerNumber: device.consumerNumber,
            ConnectionSettings: device.connectionSettings,
        };
        const { data } = await axios.post<ApiResponse>(`${API_URL}/device`, payload);
        return data;
    } catch (error) {
        console.error('Error adding device:', error);
        return undefined;
    }
};

// Edit device
export const editDevice = async (device: Device): Promise<any | undefined> => {
    try {
        const payload = {
            Id: device.id,
            Name: device.name,
            IsActive: device.isActive === '1' || device.isActive === true || String(device.isActive) === 'true',
            IP: device.ip,
            Port: device.port,
            SerialNumber: device.serialNumber,
            ConsumerNumber: device.consumerNumber,
            ConnectionSettings: device.connectionSettings,
        };
        const { data } = await axios.put<ApiResponse>(`${API_URL}/device`, payload);
        return data;
    } catch (error) {
        console.error('Error editing device:', error);
        return undefined;
    }
};

// Delete device
export const deleteDevice = async (device: Device): Promise<any> => {
    if (!device.id) throw new Error('Device ID is required');
    try {
        const { data } = await axios.delete<ApiResponse>(`${API_URL}/device/${device.id}`);
        return data;
    } catch (error) {
        console.error('Error deleting device:', error);
        throw error;
    }
};

// Fetch device readings
export const fetchDeviceReading = async (
    deviceId: string | number,
    parameterId: string | number,
    pageNumber: number,
    pageSize: number,
    startDate: string,
    endDate: string
): Promise<any | null> => {
    try {
        const { data } = await axios.get<ApiResponse>(`${API_URL}/devicelog/search`, {
            params: { deviceId, parameterId, pageNumber, pageSize, startDate, endDate },
        });
        return data;
    } catch (error) {
        console.error('Error fetching device readings:', error);
        return null;
    }
};

// Fetch event readings
export const fetchEventReading = async (
    deviceId: string | number,
    eventType: string | number,
    pageNumber: number,
    pageSize: number,
    startDate: string,
    endDate: string
): Promise<any | null> => {
    try {
        const { data } = await axios.get<ApiResponse>(`${API_URL}/eventslog/search`, {
            params: { deviceId, eventType, pageNumber, pageSize, startDate, endDate },
        });
        return data;
    } catch (error) {
        console.error('Error fetching event readings:', error);
        return null;
    }
};

// Discover and read meter parameters
export const discoverDeviceParameters = async (id: string | number, objectType?: string): Promise<any | null> => {
    try {
        const { data } = await axios.post(`${API_URL}/device/${id}/discover-parameters`, null, {
            params: objectType && objectType !== 'All' ? { objectType } : {}
        });
        return data;
    } catch (error) {
        console.error("Error discovering device parameters:", error);
        return null;
    }
};

// Read a single parameter value from a device
export const readDeviceParameter = async (deviceId: string | number, parameterId: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.post(`${API_URL}/device/${deviceId}/read-parameter/${parameterId}`);
        return data;
    } catch (error) {
        console.error("Error reading device parameter:", error);
        return null;
    }
};

// Fetch connected headers for a device
export const fetchConnectedHeaders = async (deviceId: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/connectedheader/device/${deviceId}`);
        return data;
    } catch (error) {
        console.error("Error fetching connected headers:", error);
        return null;
    }
};

// Fetch DLMS objects for a header
export const fetchDLMSObjects = async (headerId: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/dlmsobject/header/${headerId}`);
        return data;
    } catch (error) {
        console.error("Error fetching DLMS objects:", error);
        return null;
    }
};

// Fetch parameters for a DLMS object
export const fetchObjectParameters = async (objectId: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/objectparameter/object/${objectId}`);
        return data;
    } catch (error) {
        console.error("Error fetching object parameters:", error);
        return null;
    }
};

// Read a DLMS object (all parameters / attributes)
export const readDLMSObject = async (deviceId: string | number, objectId: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.post(`${API_URL}/device/${deviceId}/read-object/${objectId}`);
        return data;
    } catch (error) {
        console.error("Error reading DLMS object:", error);
        return null;
    }
};

// Read multiple DLMS objects in batch (single connection)
export const readDLMSObjectsBatch = async (deviceId: string | number, objectIds: number[]): Promise<any | null> => {
    try {
        const { data } = await axios.post(`${API_URL}/device/${deviceId}/read-objects`, objectIds);
        return data;
    } catch (error) {
        console.error("Error reading DLMS objects in batch:", error);
        return null;
    }
};


// Import local CSV files from PQM.Server/CSVFiles directory
export const importLocalCsvFiles = async (deviceId: string | number): Promise<any | undefined> => {
    try {
        const { data } = await axios.post<ApiResponse>(`${API_URL}/ftp/ImportLocalCSV`, null, {
            params: { deviceId }
        });
        return data;
    } catch (error) {
        console.error('Error importing local CSV files:', error);
        return undefined;
    }
};

// Fetch the latest clock reading for a device
export const fetchClockLatest = async (deviceId: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/clock/latest`, {
            params: { deviceId }
        });
        return data;
    } catch (error) {
        console.error("Error fetching latest clock:", error);
        return null;
    }
};

// Fetch the latest activity calendar for a device
export const fetchActivityCalendarLatest = async (deviceId: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/activitycalendar/latest`, {
            params: { deviceId }
        });
        return data;
    } catch (error) {
        console.error("Error fetching latest activity calendar:", error);
        return null;
    }
};

// Fetch ProfileGenericEntry records
export const fetchProfileGenericEntries = async (
    deviceId: string | number,
    obisCode: string,
    columnName?: string,
    startDate?: string,
    endDate?: string
): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/profilegeneric/entries`, {
            params: { deviceId, obisCode, columnName, startDate, endDate }
        });
        return data;
    } catch (error) {
        console.error("Error fetching profile entries:", error);
        return null;
    }
};

// Fetch Device Configuration setup data
export const fetchDeviceConfiguration = async (id: string | number): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/device/${id}/configuration`);
        return data;
    } catch (error) {
        console.error("Error fetching device configuration:", error);
        return null;
    }
};

// Fetch Event Status Mappings dynamically from database
export const fetchEventStatusMappings = async (): Promise<any | null> => {
    try {
        const { data } = await axios.get(`${API_URL}/eventstatusmapping`);
        return data;
    } catch (error) {
        console.error("Error fetching event status mappings:", error);
        return null;
    }
};

// Write a DLMS object attribute value
export const writeDLMSObjectAttribute = async (
    deviceId: string | number,
    obisCode: string,
    value: string,
    attributeId: number = 2
): Promise<any | null> => {
    try {
        const { data } = await axios.post(`${API_URL}/device/${deviceId}/write-object`, {
            obisCode,
            value,
            attributeId
        });
        return data;
    } catch (error) {
        console.error("Error writing DLMS object attribute:", error);
        return null;
    }
};

// Generate grouped report for selected device and parameters
export const fetchReport = async (
    deviceId: number | string,
    parameterIds: (number | string)[],
    startDate: string,
    endDate: string
): Promise<any | null> => {
    try {
        const { data } = await axios.post(`${API_URL}/reports/generate`, {
            deviceId: Number(deviceId),
            parameterIds: parameterIds.map(Number),
            startDate,
            endDate
        });
        return data;
    } catch (error) {
        console.error("Error generating report:", error);
        return null;
    }
};

// Fetch device by ID
export const fetchDeviceById = async (id: number): Promise<Device | undefined> => {
    try {
        const { data } = await axios.get<ApiResponse<Device>>(`${API_URL}/device/${id}`);
        return data.data;
    } catch (error) {
        console.error(`Error fetching device ${id}:`, error);
        return undefined;
    }
};

// Connect to device
export const connectDevice = async (id: number): Promise<any | undefined> => {
    try {
        const { data } = await axios.post<ApiResponse>(`${API_URL}/device/${id}/connect`);
        return data;
    } catch (error: any) {
        console.error('Error connecting to device:', error);
        return error.response?.data || { status: false, errors: ['Network connection error.'] };
    }
};

// Disconnect from device
export const disconnectDevice = async (id: number): Promise<any | undefined> => {
    try {
        const { data } = await axios.post<ApiResponse>(`${API_URL}/device/${id}/disconnect`);
        return data;
    } catch (error: any) {
        console.error('Error disconnecting from device:', error);
        return error.response?.data || { status: false, errors: ['Network connection error.'] };
    }
};

