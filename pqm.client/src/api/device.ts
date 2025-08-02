import axios from 'axios';
import type { Device } from '@/components/dashboard/device/devices-table';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
//const API_URL = 'http://localhost:5135';
const API_URL = 'http://103.83.106.174:83';

export const fetchDevices = async () => {
    const response = await axios.get<Response>(`${API_URL}/device`).then(response => {
        return response.data;
    }).catch(error => {
        console.error("Error:", error);
    });;
    //console.log(response.data);
    return response.data;
};

export const fetchDeviceParameter = async (id: string | number): Promise<any | null> => {
    try {
        const response = await axios.get(`${API_URL}/Parameter/${id}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching device parameter:", error);
        return null; // or throw error if you want it handled upstream
    }
};

export const addDevice = async (device: Device) => {
    console.log(device);
    const response = await axios.post<Response>(`${API_URL}/device`, {
        'Name': device.name,
        'IsActive': device.isActive == '1' ? true : false,
        'IP': device.ip,
        'Port': device.port,
        'SerialNumber':device.serialNumber,
        'ConsumerNumber':device.consumerNumber,
        'ftpFolder': device.ftpFolder
    }).then(response => {
        return response.data;
    }).catch(error => {
            console.error('Error:', error);
     });
    console.log('Response:', response.data);
    return response.data;

};


export interface Response {
    status: string;
    statusCode: number;
    data: any;
}
