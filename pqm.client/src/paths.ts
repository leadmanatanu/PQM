export const paths = {
    home: '/',
    auth: { signIn: '/auth/sign-in', signUp: '/auth/sign-up', resetPassword: '/auth/reset-password' },
    dashboard: {
        overview: '/dashboard',
        devices: '/dashboard/devices',
        mapping: '/dashboard/mapping',
        ftpfolder: '/dashboard/ftpfolder',
        devicereadings: '/dashboard/devicereadings',
        eventreadings: '/dashboard/eventreadings',
        reports: '/dashboard/reports'
    },
    errors: { notFound: '/errors/not-found' },
} as const;
