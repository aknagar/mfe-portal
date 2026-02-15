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

export default function () {
    const baseUrl = __ENV.services__augmentservice__http__0;

    // Test health endpoint
    const healthEndpoint = `${baseUrl}/health`;
    console.log(`Calling endpoint: ${healthEndpoint}`);
    const healthResponse = http.get(healthEndpoint);
    check(healthResponse, {
        'health check status is 200': (r) => r.status === 200,
        'health check response time < 200ms': (r) => r.timings.duration < 200,
    });

    sleep(1);
}
