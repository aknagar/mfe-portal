# Building in the container ensures reproducible, portable, consistent builds - the core value of Docker!
# Build stage - creates a temporary container to compile the application
FROM node:20-alpine AS builder

# Set working directory in container - all subsequent commands run from here
WORKDIR /app

# Copy package files first - Docker caches layers, so if these don't change, dependencies won't reinstall
COPY package*.json nx.json ./

# Copy shell package files - needed to install dependencies for the shell
COPY shell/package*.json ./shell/

# Install dependencies in container - needed to build the application
RUN npm install --ignore-scripts

# Copy all shell files - contains source code and configuration
COPY shell ./shell

# Change to shell directory - where the build command needs to run
WORKDIR /app/shell

# Build the application - compiles TypeScript/JSX into static HTML/CSS/JS files
RUN npm run build

# Production stage - creates the final lightweight image that will run in production
FROM node:20-alpine

# Set working directory for the production container
WORKDIR /app

# Install serve - lightweight HTTP server to host the static files
RUN npm install -g serve@14

# Copy only the built files from builder - keeps final image small (no source code or build tools)
COPY --from=builder /app/shell/dist ./dist

# Expose port 1234 - documents which port the container listens on (used by orchestrators)
EXPOSE 1234

# Health check - allows Docker/Kubernetes to verify the container is running correctly
HEALTHCHECK CMD wget -q --spider http://localhost:1234 || exit 1

# Start the web server - command that runs when container starts
CMD ["serve", "-s", "dist", "-l", "1234", "--cors"]