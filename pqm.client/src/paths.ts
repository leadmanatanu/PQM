export const paths = {
    home: '/',
    auth: { signIn: '/auth/sign-in', signUp: '/auth/sign-up', resetPassword: '/auth/reset-password' },
    dashboard: {
        overview: '/dashboard',
        account: '/dashboard/account',
        devices: '/dashboard/devices',
        customers: '/dashboard/customers',
        integrations: '/dashboard/integrations',
        settings: '/dashboard/settings',
        mapping: '/dashboard/mapping',
    },
    errors: { notFound: '/errors/not-found' },
} as const;
