'use client';

import * as React from 'react';
import Avatar from '@mui/material/Avatar';
import Box from '@mui/material/Box';
import Card from '@mui/material/Card';
import Checkbox from '@mui/material/Checkbox';
import Divider from '@mui/material/Divider';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TablePagination from '@mui/material/TablePagination';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';
import Button from '@mui/material/Button';
import dayjs from 'dayjs';

import { useSelection } from '@/hooks/use-selection';
//import { fetchDevices } from '../../../api/device'

function noop(): void {
    // do nothing
}

interface DeviceRTableProps {
    // count?: number;
    page?: number;
    rows?: any[];
    rowsPerPage?: number;
    //show?: boolean;   
    allParam?: boolean;
    paramterString?: string;
}

export function DeviceRTable({
    // count = 0,
    rows = [],
     page = 0,
     rowsPerPage = 0,
    // show = true,
    allParam = false,
    paramterString = "Paramter",
}: DeviceRTableProps): React.JSX.Element | null{
   // if (!show) return null;
  //  if (!rows.length) {
  //       return null;
  //   }
    const rowIds = React.useMemo(() => {
        return rows.map((device) => device.id);
    }, [rows]);

    
    
    const { selectAll, deselectAll, selectOne, deselectOne, selected } = useSelection(rowIds);
    const selectedSome = (selected?.size ?? 0) > 0 && (selected?.size ?? 0) < rows.length;
    const selectedAll = rows.length > 0 && selected?.size === rows.length;

    // const [page, setPage] = React.useState(0);
    // const [rowsPerPage, setRowsPerPage] = React.useState(10);

    // const handleChangePage = (event, newPage) => {
    //     //setPage(newPage);
    //     page = newPage;
    // };


    const handleChangeRowsPerPage = (
        event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
    ) => {
        //setRowsPerPage(parseInt(event.target.value, 10));
        //setPage(0);
        rowsPerPage = parseInt(event.target.value, 10);
        page = 0;
    };


    //  const handleEditClick = (deviceId: number) => {
    //     console.log(`Edit device with ID: ${deviceId}`);
    //     // Placeholder for edit logic (e.g., open edit form)
    //     onEdit(deviceId);
    // };

    const handleSyncClick = (deviceId: number) => {
        console.log(`Sync device with ID: ${deviceId}`);
        // Placeholder for sync logic (e.g., trigger sync API call)
    };

    // React.useEffect(() => {
    //     //const devices = await fetchDevices() satisfies Device[];
    // }, [page, rowsPerPage]);

    return (
        <Card>
            <Box sx={{ overflowX: 'auto' }}>
                <Table sx={{ minWidth: '800px' }}>
                    <TableHead>
                        <TableRow>
                            <TableCell>Id</TableCell>
                            <TableCell>Timestamp</TableCell>
                            <TableCell>Parameter</TableCell>
                            <TableCell>Value</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                       {rows.length > 0 ? (
          rows.map((row) => {
            const isSelected = selected?.has(row.id) || false;
            return (
              <TableRow hover key={row.id} selected={isSelected}>
                <TableCell>{row.id}</TableCell>
                <TableCell>
                  {row.dateStamp ? dayjs(row.dateStamp).format('MMM D, YYYY HH:mm:ss') : '-'}
                </TableCell>
                <TableCell>{row.parameterName || paramterString}</TableCell>
                <TableCell sx={{ fontWeight: 'bold', color: 'primary.main' }}>{row.value}</TableCell>
              </TableRow>
            );
          })
        ) : (
          <TableRow>
            <TableCell colSpan={4} align="center">
              No data available
            </TableCell>
          </TableRow>
        )}
                    </TableBody>
                </Table>
            </Box>

            <Divider />
            {/*<TablePagination
                component="div"
                count={count}
                onPageChange={handleChangePage}
                onRowsPerPageChange={handleChangeRowsPerPage}
                page={page}
                rowsPerPage={rowsPerPage}
                rowsPerPageOptions={[5, 10, 25]}
            />*/}
        </Card>
    );
}


// export function DeviceRTable({
//     // count = 0,
//     rows = [],
//     // page = 0,
//     // rowsPerPage = 0,
//     // show = true,
// }: DeviceRTableProps): React.JSX.Element | null{
//    // if (!show) return null;
//     // const rowIds = React.useMemo(() => {
//     //     return rows.map((device) => device.id);
//     // }, [rows]);

    
//     const { selectAll, deselectAll, selectOne, deselectOne, selected } = useSelection(rowIds);
//     const selectedSome = (selected?.size ?? 0) > 0 && (selected?.size ?? 0) < rows.length;
//     const selectedAll = rows.length > 0 && selected?.size === rows.length;

//     // const [page, setPage] = React.useState(0);
//     // const [rowsPerPage, setRowsPerPage] = React.useState(10);

//     const handleChangePage = (event, newPage) => {
//         //setPage(newPage);
//         page = newPage;
//     };

//     const handleChangeRowsPerPage = (event) => {
//         //setRowsPerPage(parseInt(event.target.value, 10));
//         //setPage(0);
//         rowsPerPage = parseInt(event.target.value, 10);
//         page = 0;
//     };

//     //  const handleEditClick = (deviceId: number) => {
//     //     console.log(`Edit device with ID: ${deviceId}`);
//     //     // Placeholder for edit logic (e.g., open edit form)
//     //     onEdit(deviceId);
//     // };

//     const handleSyncClick = (deviceId: number) => {
//         console.log(`Sync device with ID: ${deviceId}`);
//         // Placeholder for sync logic (e.g., trigger sync API call)
//     };

//     React.useEffect(() => {
//         //const devices = await fetchDevices() satisfies Device[];
//     }, [page, rowsPerPage]);

//     return (
//         <Card>
//             <Box sx={{ overflowX: 'auto' }}>
//                 <Table sx={{ minWidth: '800px' }}>
//                     <TableHead>
//                         <TableRow>
//                             <TableCell>Id</TableCell>
//                             <TableCell>Timestamp</TableCell>
//                             <TableCell>Parameter</TableCell>
//                         </TableRow>
//                     </TableHead>
//                     <TableBody>
//                         {rows.map((row) => {
//                             const isSelected = selected?.has(row.id);
//                             return (
//                                 <TableRow hover key={row.id} selected={isSelected}>
//                                     <TableCell>{row.id}</TableCell>
//                                     <TableCell>{row.dateStamp ? dayjs(row.dateStamp).format('MMM D, YYYY') : ''}</TableCell>
//                                     <TableCell>{row.value}</TableCell>
//                                 </TableRow>
//                             );
//                         })}
//                     </TableBody>
//                 </Table>
//             </Box>
//             <Divider />
//             <TablePagination
//                 component="div"
//                 count={count}
//                 onPageChange={handleChangePage}
//                 onRowsPerPageChange={handleChangeRowsPerPage}
//                 page={page}
//                 rowsPerPage={rowsPerPage}
//                 rowsPerPageOptions={[5, 10, 25]}
//             />
//         </Card>
//     );
// }
