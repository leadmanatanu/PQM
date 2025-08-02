import axios from 'axios';
import dayjs from 'dayjs';
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

export const fetchFtpDetails = async () => {
    const response = await axios.get<Response>(`${API_URL}/ftp`).then(response => {
        return response.data;
    }).catch(error => {
        console.error("Error:", error);
    });;
    //console.log(response.data);
    return response.data;
};

export const updateFtpDetails = async (aJson: any): Promise<any | undefined> => {
  try {
    const response = await axios.put<Response>(`${API_URL}/ftp`, aJson);
    return response.data.data;
  } catch (error) {
    console.error('Error updating FTP details:', error);
    return undefined;
  }
};

export const testFtpDetails = async (aJson: any): Promise<any | undefined> => {
  try {
    const response = await axios.get<Response>(`${API_URL}/ftp/ftpconnectiontest`, {
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

export const fetchDeviceParameter = async (id: string | number): Promise<any | null> => {
    try {
        const response = await axios.get(`${API_URL}/Parameter/${id}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching device parameter:", error);
        return null; // or throw error if you want it handled upstream
    }
};

export const updateDeviceParamMapping = async (deviceParams: any[]): Promise<any | undefined> => {
  try {
    const response = await axios.post<Response>(`${API_URL}/deviceparammapping`, deviceParams);
    return response.data;
  } catch (error) {
    console.error('Error updating device parameter mapping:', error);
    return undefined;
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

export const fetchDeviceReading = async (
  deviceId: string | number,
  parameterId: string | number,
  pageNumber: number,
  pageSize: number,
  startDate: string,
  endDate: string
): Promise<any | null> => {
  try {
    // // Construct and log the full URL
    // // Construct the query string
    // const params = new URLSearchParams({
    //   deviceId: String(deviceId),
    //   parameterId: String(parameterId),
    //   pageNumber: String(pageNumber),
    //   pageSize: String(pageSize),
    //   startDate ,//:  dayjs(startDate).format('MM/DD/YYYY'),
    //   endDate // :  dayjs(endDate).format('MM/DD/YYYY'),
    // }).toString();
    // const fullUrl = `${API_URL}/devicelog/search?${params}`;
    // console.log('Fetching from API URL:', fullUrl);
    const response = await axios.get(`${API_URL}/devicelog/search`, {
      params: {
        deviceId,
        parameterId,
        pageNumber,
        pageSize,
        startDate,
        endDate,
      },
    });
    return response.data;
  } catch (error) {
    console.error('Error fetching device readings:', error);
    return null;
  }
};

export interface Response {
    status: string;
    statusCode: number;
    data: any;
}
