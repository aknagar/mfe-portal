# Build stage
FROM node:20-alpine AS builder

WORKDIR /app/shell

# Copy package files
COPY shell/package*.json ./

# Install all dependencies
RUN npm install --ignore-scripts

# Copy application files
COPY shell/tsconfig.json ./
COPY shell/vite.config.ts ./
COPY shell/tailwind.config.js ./
COPY shell/postcss.config.js ./
COPY shell/project.json ./
COPY shell/src/ ./src/

# Build the Piral shell
RUN npm run build

# Production stage
FROM node:20-alpine AS production

WORKDIR /app

# Install serve and curl for health checks
RUN npm install -g serve@14 && \
    apk add --no-cache curl

# Copy built assets from builder
COPY --from=builder /app/shell/dist ./dist

# Expose port
EXPOSE 1234

# Health check using curl
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:1234 || exit 1

# Start the application
CMD ["serve", "-s", "dist", "-l", "1234", "--cors", "--no-clipboard"]