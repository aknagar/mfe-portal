# Building in the container ensures reproducible, portable, consistent builds - the core value of Docker!
# Build stage - creates a temporary container to compile the application
FROM node:20-alpine AS builder

# Set working directory in container - all subsequent commands run from here
WORKDIR /app

# Copy package files first - Docker caches layers, so if these don't change, dependencies won't reinstall
COPY shell/package.json ./

# Install dependencies in container - needed to build the application
RUN npm install --ignore-scripts --legacy-peer-deps

# Copy all shell files - contains source code and configuration
COPY shell ./

# Build the application - compiles TypeScript/JSX into static HTML/CSS/JS files
RUN npm run build

# Production stage - creates the final lightweight image that will run in production
FROM node:20-alpine

# Build arguments for semantic versioning - pass during build with --build-arg
ARG VERSION=dev
ARG BUILD_DATE
ARG VCS_REF

# Set working directory for the production container
WORKDIR /app

# Add metadata labels following OCI image spec and semantic versioning
LABEL org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.created="${BUILD_DATE}" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.title="MFE Portal Frontend" \
      org.opencontainers.image.description="Micro-frontend shell application"

# Install serve - lightweight HTTP server to host the static files
RUN npm install -g serve@14

# Copy only the built files from builder - keeps final image small (no source code or build tools)
COPY --from=builder /app/dist/release ./dist

# Expose port 1234 - documents which port the container listens on (used by orchestrators)
EXPOSE 1234

# Health check - allows Docker/Kubernetes to verify the container is running correctly
HEALTHCHECK CMD wget -q --spider http://localhost:1234 || exit 1

# Start the web server - command that runs when container starts
CMD ["serve", "-s", "dist", "-l", "1234", "--cors"]