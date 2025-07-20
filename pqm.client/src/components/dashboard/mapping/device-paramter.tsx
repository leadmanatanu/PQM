'use client';

import * as React from 'react';
import { useState,  useEffect} from 'react';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardActions from '@mui/material/CardActions';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Checkbox from '@mui/material/Checkbox';
import Divider from '@mui/material/Divider';
import FormControlLabel from '@mui/material/FormControlLabel';
import FormGroup from '@mui/material/FormGroup';
import Grid from '@mui/material/Grid';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';

//  {
//             "id": 1,
//             "name": "VoltageA",
//             "isActive": false,
//             "isDeleted": false,
//             "createdDate": "0001-01-01T00:00:00",
//             "createdId": null,
//             "modifiedDate": null,
//             "modifiedId": null,
//             "isSelected": false
//         },

export function DeviceParameter({ device, onDeviceUpdate }: { device: unknown[], onDeviceUpdate?: (updatedDevice: unknown[]) => void }): React.JSX.Element {
  const [updatedDevice, setUpdatedDevice] = useState(device);

  useEffect(() => {
    setUpdatedDevice(device);
  }, [device]);

   const handleCheckboxChange = (index: number) => {
    const newDevice = [...updatedDevice];
    newDevice[index] = { ...newDevice[index], isActive: !(newDevice[index] as any).isActive };
    setUpdatedDevice(newDevice);
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    onDeviceUpdate?.(updatedDevice);
  };

 return (
    <form onSubmit={handleSubmit}>
      <Card>
        <CardHeader title="Device Information" />
        <Divider />
        <CardContent>
          {updatedDevice.length > 0 ? (
            <Grid container spacing={6} wrap="wrap">
              <Grid
                size={{
                  md: 4,
                  sm: 6,
                  xs: 12,
                }}
              >
                <Stack spacing={1}>
                  <FormGroup>
                    {updatedDevice.map((row: any, index) => (
                      <FormControlLabel
                        key={row.id || index}
                        control={
                          <Checkbox
                            checked={row.isActive}
                            onChange={() => handleCheckboxChange(index)}
                          />
                        }
                        label={row.name}
                      />
                    ))}
                  </FormGroup>
                </Stack>
              </Grid>
              <Grid
                size={{
                  md: 4,
                  sm: 6,
                  xs: 12,
                }}
              >
              </Grid>
            </Grid>
          ) : (
            <CardHeader title="No device data available" />
          )}
        </CardContent>
        <Divider />
        <CardActions sx={{ justifyContent: 'flex-end' }}>
          <Button variant="contained" type="submit">
            Save changes
          </Button>
        </CardActions>
      </Card>
    </form>
  );
}
