import * as React from 'react';
import type { Metadata } from 'next';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

import { config } from '@/config';
import { Notifications } from '@/components/dashboard/mapping/notifications';
import { UpdatePasswordForm } from '@/components/dashboard/mapping/update-password-form';
import { DeviceFilters } from '@/components/dashboard/mapping/device-selection';

import { fetchDevices } from '../../../api/device';

export const metadata = { title: `Device Mapping | Dashboard | ${config.site.name}` } satisfies Metadata;

export default async function Page(): React.JSX.Element {
  const devices = await fetchDevices();
  console.log(devices[0]);

  // const handleDeviceSelect = (id : number) => {
  //   console.log("Device selected:", id);
  //   // You cannot do client-side state updates directly here
  //   // because this function is "server-side" in its origin.
  // };
  return (
    <Stack spacing={3}>
      <div>
        <Typography variant="h4">Device Mapping</Typography>
      </div>
      <DeviceFilters
        rows={devices}
      />
      {/*<Notifications />*/}
      {/*<UpdatePasswordForm /> */}
    </Stack>
  );
}
