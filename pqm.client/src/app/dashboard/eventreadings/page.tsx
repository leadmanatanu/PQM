"use client";

import * as React from "react";
import { useState, useEffect } from "react";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import CircularProgress from "@mui/material/CircularProgress";
import Box from "@mui/material/Box";

import { EventRTable } from "@/components/dashboard/eventreadings/events-table";
import { EventFilters } from "@/components/dashboard/eventreadings/event-selection";
import { EventStatusCheckboxCard } from "@/components/dashboard/eventreadings/event-status-checkbox";
import { fetchDevices, fetchEventReading } from "../../../api/device";
import type { Device } from "@/components/dashboard/device/devices-table";
import type { Dayjs } from "dayjs";

export default function Page(): React.JSX.Element {
  const [overlayLoading, setOverlayLoading] = useState(true);
  const [tableLoading, setTableLoading] = useState(false);

  const [devices, setDevices] = useState<Device[]>([]);
  const [eventLogArr, setEventLogArr] = useState<any[]>([]);
  const [totalCount, setTotalCount] = useState(0);

  // Pagination state
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  const [filters, setFilters] = useState<{
    deviceId: string | number | null;
    startTime: Dayjs | null;
    endTime: Dayjs | null;
    eventType: string | number | null;
  } | null>(null);

  useEffect(() => {
    const loadDevices = async () => {
      setOverlayLoading(true);
      try {
        const fetchedDevices = await fetchDevices();
        setDevices(fetchedDevices);
      } catch (error) {
        console.error("Failed to fetch devices:", error);
      } finally {
        setOverlayLoading(false);
      }
    };
    loadDevices();
  }, []);

  const fetchData = async (
    deviceId: string | number,
    eventType: string | number,
    startTime: Dayjs | null,
    endTime: Dayjs | null,
    page: number,
    rowsPerPage: number
  ) => {
    if (eventType.toString().startsWith("status_")) {
      setEventLogArr([]);
      setTotalCount(0);
      return;
    }
    setTableLoading(true);
    try {
      const startDate = startTime ? startTime.format("MM/DD/YYYY") : "";
      const endDate = endTime ? endTime.format("MM/DD/YYYY") : "";

      const data = await fetchEventReading(
        deviceId,
        eventType,
        page + 1, // API is 1-based
        rowsPerPage,
        startDate,
        endDate
      );

        //console.log(data);

      if (data) {
        setEventLogArr(data.data.eventLogSearch);
        setTotalCount(data.data.totalCount ?? 0);
      } else {
        setEventLogArr([]);
        setTotalCount(0);
      }
    } catch (error) {
      console.error("Search failed:", error);
    } finally {
      setTableLoading(false);
    }
  };

  const handleSearch = (params: {
    deviceId: string | number | null;
    startTime: Dayjs | null;
    endTime: Dayjs | null;
    eventType: string | number | null;
  }) => {
    const { deviceId, startTime, endTime, eventType } = params;
    if (!deviceId || !eventType || !startTime || !endTime) return;

    setFilters({ deviceId, eventType, startTime, endTime });
    setPage(0); // reset to first page
    fetchData(deviceId, eventType, startTime, endTime, 0, rowsPerPage);
  };

  const handlePageChange = (newPage: number, newRowsPerPage: number) => {
    if (!filters) return;

    const effectivePage = newRowsPerPage !== rowsPerPage ? 0 : newPage;

    setPage(effectivePage);
    setRowsPerPage(newRowsPerPage);

    // ✅ Delay fetch so dropdown stays open until after mouse release
    setTimeout(() => {
      fetchData(
        filters.deviceId!,
        filters.eventType!,
        filters.startTime,
        filters.endTime,
        effectivePage,
        newRowsPerPage
      );
    }, 0);
  };

  return (
    <div>
      {overlayLoading && (
        <Box
          sx={{
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            position: "absolute",
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: "rgba(255, 255, 255, 0.7)",
            zIndex: 1,
            pointerEvents: overlayLoading ? "auto" : "none", // ✅ block only when loading
          }}
        >
          <CircularProgress />
        </Box>
      )}
      <Stack spacing={3}>
        <div>
          <Typography variant="h4">Event Readings</Typography>
        </div>
        <EventFilters rows={devices} onSearch={handleSearch} />
        {filters && (
          filters.eventType?.toString().startsWith("status_") ? (
            <EventStatusCheckboxCard
              deviceId={filters.deviceId!}
              obisCode={filters.eventType.toString().substring(7)}
              startDate={filters.startTime?.toISOString()}
              endDate={filters.endTime?.toISOString()}
            />
          ) : (
            <EventRTable
              rows={eventLogArr}
              totalCount={totalCount}
              page={page}
              rowsPerPage={rowsPerPage}
              onPageChange={handlePageChange}
              eventType={filters.eventType?.toString() ?? null} 
            />
          )
        )}
      </Stack>
    </div>
  );
}
