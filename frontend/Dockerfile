# syntax=docker/dockerfile:1

# ---- build ----
FROM node:24-alpine AS build
WORKDIR /app

# Install layer: copy only the manifests first so the npm cache is reused when only source changes.
COPY package.json package-lock.json ./
RUN npm ci

# schema.d.ts is committed, so the build never needs a running backend (OPENAPI).
COPY . .
RUN npm run build

# ---- runtime ----
FROM nginx:stable-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
