import axios from 'axios';
import type { Device } from '@/components/dashboard/device/devices-table';
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
const API_URL = 'http://localhost:5135';

const config = {
    headers: {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Methods": "GET,PUT,POST,DELETE,PATCH,OPTIONS",
        "Content-Type": "application/json",
    }
};

export const fetchDevices = async () => {
    const response = await axios.get<Device[]>(`${API_URL}/device`).then(response => {
        return response.data;
    }).catch(error => {
        console.error("Error:", error);
    });;
    //console.log(response.data);
    return response.data;
};


export const addDevice = async (device: Device) => {
    //console.log(device);
    const response = await axios.post<Device[]>(`${API_URL}/device`, {
        'name': device.name,
        'isactive': device.isActive == '1' ? true : false,
        'ip': device.ip,
        'port': device.port
    }).then(response => {
        return response.data;
    }).catch(error => {
            console.error('Error:', error);
     });
    console.log('Response:', response.data);
    return response.data;

};