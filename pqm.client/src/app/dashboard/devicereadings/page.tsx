'use client';

import * as React from 'react';
import { useState, useEffect } from 'react';
import type { Metadata } from 'next';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import CircularProgress from '@mui/material/CircularProgress';
import Box from '@mui/material/Box';

import { config } from '@/config';
import { DeviceRTable } from '@/components/dashboard/devicereadings/devices-table';
import { UpdatePasswordForm } from '@/components/dashboard/devicereadings/update-password-form';
import { DeviceFilters } from '@/components/dashboard/devicereadings/device-selection';

import { fetchDevices, fetchDeviceParameter, fetchDeviceReading } from '../../../api/device';

export default function Page(): React.JSX.Element {
  const [loading, setLoading] = React.useState<'devices' | 'parameters' | 'search' | null>('devices');
  const [devices, setDevices] = useState<Device[]>([]);
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);
  const [devParamArr, setDevParamArr] = useState([]);
  const [deviceLogArr, setDeviceLogArr] = useState([]);
  const [selParamName, setSelParamName] = useState<string | null>(null);

  const [isVisible, setIsVisible] = useState(false);
  const page = 0;
  const rowsPerPage = 10;

  useEffect(() => {
    const loadDevices = async () => {
      setLoading('devices');
      try {
        const fetchedDevices = await fetchDevices();
        setDevices(fetchedDevices);
        console.log('Fetched devices:', fetchedDevices[0]);
      } catch (error) {
        console.error('Failed to fetch devices:', error);
      }
      setLoading(null);
    };
    loadDevices();
  }, []);

  useEffect(() => {
    if (deviceLogArr.length > 0) {
      //console.log('deviceLogArr updated:', deviceLogArr);
    }
  }, [deviceLogArr]);

  const handleDeviceSelection = async (id: string | number) => {
    setSelectedDeviceId(id);
    console.log('Device selected:', id);
    setLoading('parameters');
    try {
      const fetchedDeviceParameter = await fetchDeviceParameter(id);
      // Filter out duplicates, keeping the first occurrence of each name
      const seenNames = new Set<string>();
      const uniqueParams = fetchedDeviceParameter.data.filter((param: { name: string }) => {
        if (seenNames.has(param.name)) {
          return false; // Skip duplicates
        }
        seenNames.add(param.name);
        return true; // Keep first occurrence
      });
      setDevParamArr(uniqueParams);
      console.log('Fetched devices param:', uniqueParams);
    } catch (error) {
      console.error('Failed to fetch device parameters:', error);
      setDevParamArr([]);
    } finally {
      setLoading(null);
    }
  };

  const handleDeviceUpdate = (updatedDevice: unknown[]) => {
    //setDevParamArr(updatedDevice);
    console.log('Updated device parameters:', updatedDevice);
  };

  const handleSearch = async ({
    deviceId,
    startTime,
    endTime,
    paramId,
  }: {
    deviceId: string | number | null;
    startTime: Dayjs | null;
    endTime: Dayjs | null;
    paramId: string | number | null;
  }) => {
    setIsVisible(true);
    // Basic validation
    if (!deviceId) {
      console.error('Search failed: No device selected');
      return;
    }
    if (!paramId) {
      console.error('Search failed: No parameter selected');
      return;
    }
    if (!startTime || !endTime) {
      console.error('Search failed: Start time or end time missing');
      return;
    }
    if (endTime.isBefore(startTime)) {
      console.error('Search failed: End time must be after start time');
      return;
    }

    // Log search parameters for debugging
    console.log('Search initiated with parameters:', {
      deviceId,
      startTime: startTime.toISOString(),
      endTime: endTime.toISOString(),
      paramId,
    });

    // Format dates to MM/DD/YYYY as required by the API
    const startDate = startTime.format('MM/DD/YYYY');
    const endDate = endTime.format('MM/DD/YYYY');

    setLoading('search');
    try {
      const data = await fetchDeviceReading(deviceId, paramId, 1, 1000000, startDate, endDate);
      if (data) {
        console.log('Device readings:', data);
        const matchedRow = devParamArr.find((d: any) => d.id === paramId);
        console.log('matchedRow:', matchedRow);
        if (matchedRow) {
          setSelParamName(matchedRow.name);
        }
        setDeviceLogArr(data.data.deviceLogSearch);
      } else {
        console.error('No data returned from fetchDeviceReading');
      }
    } catch (error) {
      console.error('Search failed:', error);
    } finally {
      setLoading(null);
    }
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
            zIndex: 1,
          }}
        >
          <CircularProgress />
        </Box>
      )}
      <Stack spacing={3}>
        <div>
          <Typography variant="h4">Device Readings</Typography>
        </div>
        <DeviceFilters
          rows={devices}
          onDeviceSelect={handleDeviceSelection}
          paramArray={devParamArr}
          onSearch={handleSearch}
        />
        {isVisible && (
          <DeviceRTable
            rows={deviceLogArr}
            allParam={false}
            paramterString={selParamName}
          />
        )}
        {/*<DeviceParameter device={devParamArr} onDeviceUpdate={handleDeviceUpdate} /> */}
        {/*<UpdatePasswordForm />*/ }
      </Stack>
    </div>
  );
}

// export default function Page(): React.JSX.Element {
//   const [loading, setLoading] = React.useState(true);
//   const [devices, setDevices] = useState<Device[]>([]);
//   const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);
//   const [devParamArr,setDevParamArr] = useState([]);
//   const [deviceLogArr,setDeviceLogArr] = useState([]);
//   const [selParamName, setSelParamName] = useState<string | null>(null);

//   const [isVisible, setIsVisible] = useState(false);
//   const page = 0;
//   const rowsPerPage = 10;

//   useEffect(() => {
//     const loadDevices = async () => {
//       setLoading(true);
//       try {
//         const fetchedDevices = await fetchDevices();
//         setDevices(fetchedDevices);
//         console.log('Fetched devices:', fetchedDevices[0]);
        
//       } catch (error) {
//         console.error('Failed to fetch devices:', error);
//       }
//       setLoading(false);
//     };
//     loadDevices();
//   }, []);

//   useEffect(() => {
//     if (deviceLogArr.length > 0) {
//       //console.log('deviceLogArr updated:', deviceLogArr);
//     }
//   }, [deviceLogArr]);

//   const handleDeviceSelection = (id: string | number) => {
//     setSelectedDeviceId(id);
//     console.log('Device selected:', id);
//    // setLoading(true);
//     fetchDeviceParameter(id)
//       .then((fetchedDeviceParameter) => {
//         // Filter out duplicates, keeping the first occurrence of each name
//         const seenNames = new Set<string>();
//         const uniqueParams = fetchedDeviceParameter.data.filter((param: { name: string }) => {
//           if (seenNames.has(param.name)) {
//             return false; // Skip duplicates
//           }
//           seenNames.add(param.name);
//           return true; // Keep first occurrence
//         });
//         setDevParamArr(uniqueParams);
//         console.log('Fetched devices param:', uniqueParams);
//        // setLoading(false);
//       })
//       .catch((error) => {
//         console.error('Failed to fetch devices:', error);
//         setDevParamArr([]);
//        // setLoading(false);
//       });
// };

//     const handleDeviceUpdate = (updatedDevice: unknown[]) => {
//   //  setDevParamArr(updatedDevice);
//     console.log('Updated device parameters:', updatedDevice);
//   };

//  const handleSearch = async({
//   deviceId,
//   startTime,
//   endTime,
//   paramId,
// }: {
//   deviceId: string | number | null;
//   startTime: Dayjs | null;
//   endTime: Dayjs | null;
//   paramId: string | number | null;
// }) => {
//   setIsVisible(true);
//   // Basic validation
//   if (!deviceId) {
//     console.error('Search failed: No device selected');
//     return;
//   }
//   if (!paramId) {
//     console.error('Search failed: No parameter selected');
//     return;
//   }
//   if (!startTime || !endTime) {
//     console.error('Search failed: Start time or end time missing');
//     return;
//   }
//   if (endTime.isBefore(startTime)) {
//     console.error('Search failed: End time must be after start time');
//     return;
//   }

//   // Log search parameters for debugging
//   console.log('Search initiated with parameters:', {
//     deviceId,
//     startTime: startTime.toISOString(),
//     endTime: endTime.toISOString(),
//     paramId,
//   });

//   // Format dates to MM/DD/YYYY as required by the API
//   const startDate = startTime.format('MM/DD/YYYY');
//   const endDate = endTime.format('MM/DD/YYYY');

//   // Log search parameters for debugging
//   console.log('Search initiated with parameters:', {
//     deviceId,
//     startTime: startTime.toISOString(),
//     endTime: endTime.toISOString(),
//     paramId,
//   });

//   try {
//     const data = await fetchDeviceReading(deviceId, paramId, 1, 1000000, startDate, endDate);
//     if (data) {
//       console.log('Device readings:', data);
//       const matchedRow = devParamArr.find((d) => d.id === paramId);
//       console.log('matchedRow:', matchedRow);
//       if (matchedRow) {
//           setSelParamName(matchedRow.name);
//       }
//       setDeviceLogArr(data.data.deviceLogSearch)      
//       // Process the data (e.g., update state for display)
//     } else {
//       console.error('No data returned from fetchDeviceReading');
//     }
//   } catch (error) {
//     console.error('Search failed:', error);
//   }
// };


//   return (
//     <div>
//       {loading ? (
//         <Box sx={{
//           display: 'flex',
//           justifyContent: 'center', // Center horizontally
//           alignItems: 'center', // Center vertically
//           minHeight: '100px', // Optional: Ensure the Box has height for visibility
//         }}>
//           <CircularProgress />
//         </Box>
//       ) : (
//     <Stack spacing={3}>
//       <div>
//         <Typography variant="h4">Device Readings</Typography>
//       </div>
//       <DeviceFilters
//         rows={devices}
//         onDeviceSelect={handleDeviceSelection}
//         paramArray={devParamArr}
//         onSearch={handleSearch}
//       />

//       {isVisible && (<DeviceRTable
//         rows={deviceLogArr}
//         allParam={false}
//         paramterString={selParamName}
//       />)}
//       {/*<DeviceParameter device={devParamArr} onDeviceUpdate={handleDeviceUpdate} /> */}
//       {/*<UpdatePasswordForm />*/ }
//     </Stack>
//       )}
//     </div>
//   );
// }
