import http from "k6/http";
import { check, sleep } from "k6";

// Smoke test configuration - minimal load to verify functionality
export const options = {
    vus: 1,              // 1 virtual user
    duration: '30s',     // Run for 30 seconds
    thresholds: {
        http_req_duration: ['p(99)<1000'], // 99% of requests should be below 1s
        http_req_failed: ['rate<0.05'],    // Less than 5% of requests should fail
    },
};

export default function () {
    // Use Aspire-injected environment variables with fallback chain
    // Test AppHost uses 'augmentservice-api', Main AppHost uses 'augmentservice'
    const baseUrl = __ENV.services__augmentservice__http__0;

    // Test 1: Health endpoint
    const healthResponse = http.get(`${baseUrl}/health`);
    check(healthResponse, {
        'health endpoint returns 200': (r) => r.status === 200,
    });

    sleep(0.5);

    // Test 2: API endpoint (adjust based on your actual endpoints)
    const apiResponse = http.get(`${baseUrl}/api/augment`);
    check(apiResponse, {
        'api endpoint is accessible': (r) => r.status >= 200 && r.status < 500,
    });

    sleep(0.5);
}
