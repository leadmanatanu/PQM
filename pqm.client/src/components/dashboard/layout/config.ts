import type { NavItemConfig } from '@/types/nav';
import { paths } from '@/paths';

export const navItems = [
    { key: 'devices', title: 'Devices', href: paths.dashboard.devices, icon: 'devices' },
    { key: 'mapping', title: 'Mapping', href: paths.dashboard.mapping, icon: 'gear-six' },
    { key: 'devicereadings', title: 'Device Readings', href: paths.dashboard.devicereadings, icon: 'user' },
    { key: 'eventreadings', title: 'Event Readings', href: paths.dashboard.eventreadings, icon: 'x-square' },
    { key: 'reports', title: 'Reports', href: paths.dashboard.reports, icon: 'file-text' },
] satisfies NavItemConfig[];
