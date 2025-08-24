/** @type {import('next').NextConfig} */
//const config = {};

//export default config;

const nextConfig = {
    eslint: {
            ignoreDuringBuilds: true
        },
    output: 'export',
    distDir: 'build',
};

export default nextConfig;