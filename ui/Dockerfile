# Development stage
FROM node:18-alpine AS dev
WORKDIR /app
COPY ui/package*.json ./
RUN npm ci --only=production=false && npm cache clean --force
COPY ui/ .
EXPOSE 3000
CMD ["npm", "run", "dev", "--", "--host"]

# Build stage
FROM node:18-alpine AS build
WORKDIR /app

# Copy package files and install dependencies
COPY ui/package*.json ./
RUN npm ci --only=production=false && npm cache clean --force

# Copy source and build
COPY ui/ .
RUN npm run build && \
    rm -rf node_modules && \
    rm -rf src && \
    rm -rf .vite

# Production stage - minimal nginx alpine
FROM nginx:alpine AS prod
# Remove default nginx config and static files
RUN rm -rf /usr/share/nginx/html/* /etc/nginx/conf.d/default.conf

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html
COPY ui/nginx.conf /etc/nginx/conf.d/default.conf

# Note: nginx:alpine runs as non-root by default, no need to change user
# This keeps the image minimal and secure

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
