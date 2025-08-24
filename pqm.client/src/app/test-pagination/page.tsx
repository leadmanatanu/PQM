"use client";

import * as React from "react";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import Divider from "@mui/material/Divider";
import TablePagination from "@mui/material/TablePagination";

export default function Page(): React.JSX.Element {
    const [page, setPage] = React.useState(0);
    const [rowsPerPage, setRowsPerPage] = React.useState(10);
    const totalCount = 123; // dummy total

    const handleChangePage = (
        event: React.MouseEvent<HTMLButtonElement> | null,
        newPage: number
    ) => {
        console.log("[Test] handleChangePage", { newPage, rowsPerPage });
        setPage(newPage);
    };

    const handleChangeRowsPerPage = (
        event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
    ) => {
        const newRows = parseInt(event.target.value, 10);
        console.log("[Test] handleChangeRowsPerPage", { newRows, rowsPerPage });
        setRowsPerPage(newRows);
        setPage(0);
    };

    return (
        <Card sx={{ p: 2, maxWidth: 600, margin: "40px auto" }}>
            <Box sx={{ mb: 2 }}>Minimal Pagination Test</Box>
            <Divider />
            <TablePagination
                component="div"
                count={totalCount}
                page={page}
                onPageChange={handleChangePage}
                rowsPerPage={rowsPerPage}
                onRowsPerPageChange={handleChangeRowsPerPage}
                rowsPerPageOptions={[10, 25, 50, 100]}
                SelectProps={{
                    MenuProps: {
                        keepMounted: true,
                        disableScrollLock: true,
                        disablePortal: false, // ✅ default: render at document.body
                        container: typeof document !== "undefined" ? document.body : undefined,
                    },
                }}
            />
        </Card>
    );
}
