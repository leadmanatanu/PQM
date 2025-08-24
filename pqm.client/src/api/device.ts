import axios from 'axios';
import dayjs from 'dayjs';
import type { Device } from '@/components/dashboard/device/devices-table';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
//const API_URL = 'http://localhost:5135/api';
const API_URL = 'http://103.83.106.174:83/api';

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
            IsActive: device.isActive === '1',
            IP: device.ip,
            Port: device.port,
            SerialNumber: device.serialNumber,
            ConsumerNumber: device.consumerNumber,
            ftpFolder: device.ftpFolder,
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
            IsActive: device.isActive === '1',
            IP: device.ip,
            Port: device.port,
            SerialNumber: device.serialNumber,
            ConsumerNumber: device.consumerNumber,
            ftpFolder: device.ftpFolder,
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
