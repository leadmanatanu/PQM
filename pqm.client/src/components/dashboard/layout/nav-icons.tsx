import type { Icon } from '@phosphor-icons/react/dist/lib/types';
import { ChartPieIcon } from '@phosphor-icons/react/dist/ssr/ChartPie';
import { GearSixIcon } from '@phosphor-icons/react/dist/ssr/GearSix';
import { Folder } from '@phosphor-icons/react/dist/ssr/Folder';
import { UserIcon } from '@phosphor-icons/react/dist/ssr/User';
import { UsersIcon } from '@phosphor-icons/react/dist/ssr/Users';
import { XSquare } from '@phosphor-icons/react/dist/ssr/XSquare';
import { Devices } from '@phosphor-icons/react/dist/ssr/Devices';
import { FileText } from '@phosphor-icons/react/dist/ssr/FileText';

export const navIcons = {
    'chart-pie': ChartPieIcon,
    'gear-six': GearSixIcon,
    'plugs-connected': Folder,
    'x-square': XSquare,
    user: UserIcon,
    users: UsersIcon,
    devices: Devices,
    'file-text': FileText,
} as Record<string, Icon>;
