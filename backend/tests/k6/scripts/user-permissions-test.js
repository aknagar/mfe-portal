import http from "k6/http";
import { check, sleep } from "k6";

// Load test configuration for user permissions API
export const options = {
    stages: [
        { duration: '30s', target: 20 },  // Ramp up to 20 users
        { duration: '1m', target: 20 },   // Stay at 20 users
        { duration: '30s', target: 0 },   // Ramp down to 0 users
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
        http_req_failed: ['rate<0.01'],   // Less than 1% of requests should fail
    },
};

export default function () {
    const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';

    // Test user permissions endpoints
    const headers = {
        'Content-Type': 'application/json',
    };

    // Get user permissions
    const getPermissionsResponse = http.get(`${baseUrl}/api/permissions`, { headers });
    check(getPermissionsResponse, {
        'get permissions status is 200 or 401': (r) => r.status === 200 || r.status === 401,
        'get permissions response time < 300ms': (r) => r.timings.duration < 300,
    });

    sleep(1);

    // Check specific permission
    const checkPermissionResponse = http.get(`${baseUrl}/api/permissions/check?permission=read`, { headers });
    check(checkPermissionResponse, {
        'check permission status is accessible': (r) => r.status >= 200 && r.status < 500,
        'check permission response time < 200ms': (r) => r.timings.duration < 200,
    });

    sleep(1);
}
