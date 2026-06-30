import type { NavItemConfig } from '@/types/nav';
import { paths } from '@/paths';

export const navItems = [
    { key: 'devices', title: 'Devices', href: paths.dashboard.devices, icon: 'devices' },
    { key: 'mapping', title: 'Mapping', href: paths.dashboard.mapping, icon: 'gear-six' },
    { key: 'ftpfolder', title: 'FTP Folder', href: paths.dashboard.ftpfolder, icon: 'plugs-connected' },
    { key: 'devicereadings', title: 'Device Readings', href: paths.dashboard.devicereadings, icon: 'user' },
    { key: 'eventreadings', title: 'Event Readings', href: paths.dashboard.eventreadings, icon: 'x-square' },
   // { key: 'customers', title: 'Customers', href: paths.dashboard.customers, icon: 'users' },
    //{ key: 'integrations', title: 'Integrations', href: paths.dashboard.integrations, icon: 'plugs-connected' },
  //  { key: 'settings', title: 'Settings', href: paths.dashboard.settings, icon: 'gear-six' },
   // { key: 'account', title: 'Account', href: paths.dashboard.account, icon: 'user' },
    //{ key: 'error', title: 'Error', href: paths.errors.notFound, icon: 'x-square' },
] satisfies NavItemConfig[];
