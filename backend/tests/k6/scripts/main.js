import http from "k6/http";
import { check, sleep } from "k6";

// Test configuration
export const options = {
    stages: [
        { duration: '30s', target: 10 },  // Ramp up to 10 users
        { duration: '1m', target: 10 },   // Stay at 10 users
        { duration: '30s', target: 0 },   // Ramp down to 0 users
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% of requests should be below 500ms
        http_req_failed: ['rate<0.01'],   // Less than 1% of requests should fail
    },
};

// Setup function runs once at the start to log environment variables
export function setup() {
    console.log('=== K6 Environment Variables Diagnostic ===');
    console.log('services__augmentservice_api__http__0:', __ENV.services__augmentservice_api__http__0 || 'NOT SET');
    console.log('services__augmentservice__http__0:', __ENV.services__augmentservice__http__0 || 'NOT SET');

    const baseUrl = __ENV.services__augmentservice_api__http__0
        || __ENV.services__augmentservice__http__0
        || 'http://localhost:5000';

    console.log('Selected baseUrl:', baseUrl);
    console.log('===========================================');

    return { baseUrl };
}

export default function (data) {
    // Use the baseUrl from setup
    const baseUrl = data.baseUrl;

    // Test health endpoint
    const healthResponse = http.get(`${baseUrl}/health`);
    check(healthResponse, {
        'health check status is 200': (r) => r.status === 200,
        'health check response time < 200ms': (r) => r.timings.duration < 200,
    });

    sleep(1);
}
