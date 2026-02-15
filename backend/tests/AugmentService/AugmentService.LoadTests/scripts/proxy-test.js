import http from "k6/http";
import { check, sleep } from "k6";

// Load test configuration for proxy API
export const options = {
    stages: [
        { duration: '30s', target: 15 },  // Ramp up to 15 users
        { duration: '1m', target: 15 },   // Stay at 15 users
        { duration: '30s', target: 0 },   // Ramp down to 0 users
    ],
    thresholds: {
        http_req_duration: ['p(95)<1000'], // 95% of requests should be below 1s (proxy may be slower)
        http_req_failed: ['rate<0.05'],    // Less than 5% of requests should fail
    },
};

export default function () {
    const baseUrl = __ENV.BASE_URL || 'http://localhost:5000';

    // Test proxy/augment endpoints
    const headers = {
        'Content-Type': 'application/json',
    };

    // Test augment API endpoint
    const augmentPayload = JSON.stringify({
        text: 'Sample text for augmentation',
        options: {
            enhance: true
        }
    });

    const augmentResponse = http.post(`${baseUrl}/api/augment`, augmentPayload, { headers });
    check(augmentResponse, {
        'augment status is accessible': (r) => r.status >= 200 && r.status < 500,
        'augment response time < 1000ms': (r) => r.timings.duration < 1000,
    });

    sleep(1);

    // Test proxy health
    const proxyHealthResponse = http.get(`${baseUrl}/api/proxy/health`, { headers });
    check(proxyHealthResponse, {
        'proxy health status is accessible': (r) => r.status >= 200 && r.status < 500,
        'proxy health response time < 200ms': (r) => r.timings.duration < 200,
    });

    sleep(0.5);
}
